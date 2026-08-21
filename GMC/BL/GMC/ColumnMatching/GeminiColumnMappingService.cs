using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GMC.Interface.GMC;
using Microsoft.Extensions.Options;

namespace GMC.BL.GMC.ColumnMatching
{
    public class GeminiColumnMappingService : IAiColumnMappingService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly AiColumnMappingOptions _opts;
        private readonly ILogger<GeminiColumnMappingService> _log;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiColumnMappingService(
            IHttpClientFactory httpFactory,
            IOptions<AiColumnMappingOptions> opts,
            ILogger<GeminiColumnMappingService> log)
        {
            _httpFactory = httpFactory;
            _opts        = opts.Value;
            _log         = log;
        }

        public async Task<IReadOnlyDictionary<string, AiColumnSuggestion>> SuggestAsync(
            IReadOnlyList<string> sourceColumns,
            IReadOnlyList<string> masterColumns,
            string dataCategory,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, AiColumnSuggestion>(StringComparer.OrdinalIgnoreCase);
            if (sourceColumns.Count == 0 || masterColumns.Count == 0)
                return result;

            if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                _log.LogInformation("Column-mapping AI disabled or no API key — skipping Gemini call.");
                return result;
            }

            try
            {
                var prompt = BuildPrompt(sourceColumns, masterColumns, dataCategory);
                var raw = await CallGeminiAsync(prompt, cancellationToken);
                var parsed = ParseAiResponse(raw, sourceColumns, masterColumns);
                foreach (var kv in parsed)
                    result[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Gemini column mapping failed — caller will use fuzzy fallback.");
            }

            return result;
        }

        private static string BuildPrompt(
            IReadOnlyList<string> sources,
            IReadOnlyList<string> masters,
            string dataCategory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You map Excel column headers to database column names for GMC (Group Medical Cover) insurance rollover data.");
            sb.AppendLine($"Data category: {dataCategory}");
            sb.AppendLine();
            sb.AppendLine("Excel columns to map:");
            foreach (var s in sources)
                sb.AppendLine($"- {s}");
            sb.AppendLine();
            sb.AppendLine("Valid DB columns (target MUST be exactly one of these names, or null if no good match):");
            foreach (var m in masters)
                sb.AppendLine($"- {m}");
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON array, no markdown, no explanation:");
            sb.AppendLine("[{\"source\":\"Excel Header\",\"target\":\"Exact DB Column Name or null\",\"confidence\":85}]");
            sb.AppendLine("Rules:");
            sb.AppendLine("- Use semantic meaning (e.g. \"Relation Band\" → \"Relation\", \"Disease\" → closest disease column).");
            sb.AppendLine("- confidence is 0-100 (how sure you are).");
            sb.AppendLine("- target must exactly match a valid DB column name or be null.");
            return sb.ToString();
        }

        private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
        {
            var model = string.IsNullOrWhiteSpace(_opts.Model) ? "gemini-2.0-flash" : _opts.Model.Trim();
            var url = $"{_opts.BaseUrl.TrimEnd('/')}/{model}:generateContent?key={Uri.EscapeDataString(_opts.ApiKey.Trim())}";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(url, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Gemini HTTP {Status}: {Body}", (int)response.StatusCode, Truncate(json, 400));
                throw new InvalidOperationException($"Gemini API returned {(int)response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                throw new InvalidOperationException("Gemini returned no candidates.");

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }

        private static Dictionary<string, AiColumnSuggestion> ParseAiResponse(
            string raw,
            IReadOnlyList<string> sources,
            IReadOnlyList<string> masters)
        {
            var map = new Dictionary<string, AiColumnSuggestion>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return map;

            var json = ExtractJsonArray(raw);
            if (string.IsNullOrWhiteSpace(json))
                return map;

            var rows = JsonSerializer.Deserialize<List<AiRow>>(json, JsonOpts) ?? new List<AiRow>();
            var masterLookup = masters.ToDictionary(m => m, m => m, StringComparer.OrdinalIgnoreCase);
            var sourceLookup = sources.ToDictionary(s => s, s => s, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Source)) continue;
                if (!sourceLookup.TryGetValue(row.Source.Trim(), out var srcKey)) continue;

                string? target = null;
                if (!string.IsNullOrWhiteSpace(row.Target)
                    && !string.Equals(row.Target.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                {
                    if (masterLookup.TryGetValue(row.Target.Trim(), out var exact))
                        target = exact;
                }

                var conf = row.Confidence;
                if (conf < 0m) conf = 0m;
                if (conf > 100m) conf = 100m;

                map[srcKey] = new AiColumnSuggestion
                {
                    TargetColumn  = target,
                    ConfidencePct = conf
                };
            }

            return map;
        }

        private static string ExtractJsonArray(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var start = text.IndexOf('[');
                var end   = text.LastIndexOf(']');
                if (start >= 0 && end > start)
                    return text[start..(end + 1)];
            }
            var i = text.IndexOf('[');
            var j = text.LastIndexOf(']');
            if (i >= 0 && j > i)
                return text[i..(j + 1)];
            return text;
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";

        private sealed class AiRow
        {
            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonPropertyName("target")]
            public string? Target { get; set; }

            [JsonPropertyName("confidence")]
            public decimal Confidence { get; set; }
        }
    }
}
