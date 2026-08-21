using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.BL.GMC.ColumnMatching
{
    /// <summary>
    /// Column-mapping service.  The master universe of valid target columns
    /// comes from <c>tbl_GMC_Master_Column</c>; per-row <c>Synonyms</c>
    /// (CSV / pipe-separated) seed the exact-match dictionary.  The legacy
    /// SP <c>udsp_GMS_Column_Plotting_*</c> is still consulted as a secondary
    /// alias source so any previously-learned mappings (persisted via
    /// <c>udsp_Save_GMC_*_MappingDatta</c>) keep paying off.  When neither
    /// dictionary contains a header we call Google Gemini (free tier) for
    /// semantic mapping, then fall back to Jaro-Winkler fuzzy match if AI is
    /// unavailable or returns low confidence.
    ///
    /// Confidence buckets used by the UI:
    ///   high  >= 90%   green
    ///   mid    60..89% yellow
    ///   low   <  60%   red
    /// </summary>
    public class LegacyColumnMatcher : IColumnMatcher
    {
        private readonly IRolloverUploadRepo _repo;
        private readonly IAiColumnMappingService _ai;
        private readonly ILogger<LegacyColumnMatcher> _log;

        public LegacyColumnMatcher(
            IRolloverUploadRepo repo,
            IAiColumnMappingService ai,
            ILogger<LegacyColumnMatcher> log)
        {
            _repo = repo;
            _ai   = ai;
            _log  = log;
        }

        public async Task<List<ColumnMapping>> MatchAsync(IEnumerable<string> sourceColumns,
                                                          string? dataCategory = null)
        {
            var headers = sourceColumns
                .Select(s => (s ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            if (headers.Count == 0) return new List<ColumnMapping>();

            var cat = string.IsNullOrWhiteSpace(dataCategory) ? "Enrollment" : dataCategory!;

            // 1) Master universe + alias dictionary from tbl_GMC_Master_Column
            List<MasterColumnRow> master;
            try
            {
                master = await _repo.GetMasterColumnsAsync(cat);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Could not read tbl_GMC_Master_Column — using empty master list.");
                master = new List<MasterColumnRow>();
            }

            // Dropdown / fuzzy universe = distinct single [Master Parameter] values.
            var masterCols = master
                .Select(m => (m.ColumnName ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrEmpty(s) && !s.Contains(',', StringComparison.Ordinal))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // header (normalised) -> master column
            var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void AddAlias(string? alias, string targetMaster)
            {
                if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(targetMaster)) return;
                var key = Normalise(alias);
                if (key.Length == 0) return;
                if (!aliasMap.ContainsKey(key)) aliasMap[key] = targetMaster;
            }

            // a) every single master column is its own alias
            foreach (var m in master)
            {
                if (string.IsNullOrWhiteSpace(m.ColumnName) || m.ColumnName.Contains(','))
                    continue;
                AddAlias(m.ColumnName, m.ColumnName);
            }

            // b) [CurrentColumnName] (or Synonyms) → [Master Parameter]
            foreach (var m in master)
            {
                if (string.IsNullOrWhiteSpace(m.ColumnName) || m.ColumnName.Contains(','))
                    continue;
                if (!string.IsNullOrWhiteSpace(m.Synonyms))
                    AddAlias(m.Synonyms.Trim(), m.ColumnName);
            }

            // c) Legacy SP — kept as a secondary learning channel so older
            //    underwriter corrections still help.  Failures are non-fatal.
            try
            {
                var legacy = await _repo.RunLegacyColumnPlottingAsync(string.Join(",", headers), cat);
                foreach (var row in legacy)
                {
                    if (string.IsNullOrWhiteSpace(row.MatchedInput)) continue;
                    if (string.IsNullOrWhiteSpace(row.MasterColumn)) continue;

                    // Only honour SP hints whose target is in the new master
                    // universe — keeps the matcher consistent with the table.
                    var hit = masterCols.FirstOrDefault(c =>
                        string.Equals(c, row.MasterColumn, StringComparison.OrdinalIgnoreCase));
                    if (hit != null) AddAlias(row.MatchedInput, hit);
                }
            }
            catch (Exception ex)
            {
                _log.LogInformation(ex, "udsp_GMS_Column_Plotting_* skipped (non-fatal).");
            }

            // 2) Per-Excel-header decision — exact matches first
            var results     = new List<ColumnMapping>(headers.Count);
            var needAi      = new List<string>();

            foreach (var src in headers)
            {
                var key = Normalise(src);

                if (aliasMap.TryGetValue(key, out var exact))
                {
                    results.Add(new ColumnMapping
                    {
                        SourceColumn  = src,
                        TargetColumn  = exact,
                        ConfidencePct = 100m,
                        IsManual      = false,
                        IsApproved    = true,
                        SuggestedBy   = "MasterTable"
                    });
                    continue;
                }

                needAi.Add(src);
            }

            // 3) Free AI (Gemini) for remaining headers — one batch call
            IReadOnlyDictionary<string, AiColumnSuggestion> aiMap =
                new Dictionary<string, AiColumnSuggestion>(StringComparer.OrdinalIgnoreCase);
            if (needAi.Count > 0 && masterCols.Count > 0)
            {
                aiMap = await _ai.SuggestAsync(needAi, masterCols, cat);
            }

            foreach (var src in needAi)
            {
                if (aiMap.TryGetValue(src, out var ai)
                    && !string.IsNullOrWhiteSpace(ai.TargetColumn)
                    && ai.ConfidencePct >= 60m)
                {
                    results.Add(new ColumnMapping
                    {
                        SourceColumn  = src,
                        TargetColumn  = ai.TargetColumn,
                        ConfidencePct = ai.ConfidencePct,
                        IsManual      = false,
                        IsApproved    = ai.ConfidencePct >= 90m,
                        SuggestedBy   = "AI"
                    });
                    continue;
                }

                // Fuzzy fallback when AI is off, fails, or low confidence
                var key = Normalise(src);
                string? bestTarget = null;
                double bestScore = 0d;
                foreach (var m in masterCols)
                {
                    var score = JaroWinkler(key, Normalise(m));
                    if (score > bestScore) { bestScore = score; bestTarget = m; }
                }
                var pct = (decimal)Math.Round(bestScore * 100d, 2);

                if (bestTarget != null && pct >= 60m)
                {
                    results.Add(new ColumnMapping
                    {
                        SourceColumn  = src,
                        TargetColumn  = bestTarget,
                        ConfidencePct = pct,
                        IsManual      = false,
                        IsApproved    = pct >= 90m,
                        SuggestedBy   = "Fuzzy"
                    });
                }
                else
                {
                    results.Add(new ColumnMapping
                    {
                        SourceColumn  = src,
                        TargetColumn  = null,
                        ConfidencePct = 0m,
                        IsManual      = false,
                        IsApproved    = false,
                        SuggestedBy   = "None"
                    });
                }
            }

            // Preserve original Excel column order
            var order = headers
                .Select((h, i) => new { h, i })
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);
            results.Sort((a, b) =>
            {
                order.TryGetValue(a.SourceColumn, out var ia);
                order.TryGetValue(b.SourceColumn, out var ib);
                return ia.CompareTo(ib);
            });

            return results;
        }

        // --------------------------------------------------------------- helpers

        public static string Normalise(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            Span<char> buf = stackalloc char[s.Length];
            int len = 0;
            foreach (var ch in s)
                if (char.IsLetterOrDigit(ch))
                    buf[len++] = char.ToLowerInvariant(ch);
            return new string(buf[..len]);
        }

        public static double JaroWinkler(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0d;
            if (s1 == s2) return 1d;

            int len1 = s1.Length, len2 = s2.Length;
            int matchDistance = Math.Max(len1, len2) / 2 - 1;
            if (matchDistance < 0) matchDistance = 0;

            bool[] m1 = new bool[len1];
            bool[] m2 = new bool[len2];
            int matches = 0;

            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end   = Math.Min(i + matchDistance + 1, len2);
                for (int j = start; j < end; j++)
                {
                    if (m2[j]) continue;
                    if (s1[i] != s2[j]) continue;
                    m1[i] = true; m2[j] = true; matches++;
                    break;
                }
            }
            if (matches == 0) return 0d;

            int trans = 0;
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!m1[i]) continue;
                while (!m2[k]) k++;
                if (s1[i] != s2[k]) trans++;
                k++;
            }
            double m = matches;
            double jaro = (m / len1 + m / len2 + (m - trans / 2d) / m) / 3d;

            int prefix = 0;
            int maxPrefix = Math.Min(4, Math.Min(len1, len2));
            for (int i = 0; i < maxPrefix; i++)
            {
                if (s1[i] == s2[i]) prefix++; else break;
            }
            return jaro + 0.1 * prefix * (1 - jaro);
        }
    }
}
