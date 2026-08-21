using GMC.BL.GMC.ColumnMatching;
using GMC.Interface.GMC;
using GMC.Models.GMC;
using OfficeOpenXml;

namespace GMC.BL.GMC
{
        public class UnderwriterBL : IUnderwriterBL
    {
        private readonly IRolloverUploadRepo _repo;
        private readonly IColumnMatcher _matcher;
        private readonly IMappedExcelIngestor _ingestor;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UnderwriterBL> _log;

        public UnderwriterBL(IRolloverUploadRepo repo,
                             IColumnMatcher matcher,
                             IMappedExcelIngestor ingestor,
                             IWebHostEnvironment env,
                             ILogger<UnderwriterBL> log)
        {
            _repo     = repo;
            _matcher  = matcher;
            _ingestor = ingestor;
            _env      = env;
            _log      = log;
        }

        public Task<List<RolloverUpload>> GetQueueAsync(string role, string userName, string? status = null)
            => _repo.GetPendingUploadsAsync(role, userName, status);

        public async Task<UploadReviewModel> GetReviewAsync(int uploadId, bool forceRematch = false)
        {
            var upload = await _repo.GetUploadByIdAsync(uploadId)
                          ?? throw new InvalidOperationException($"Upload {uploadId} not found.");

            // existing draft mappings, if any
            var existing = await _repo.GetMappingsAsync(uploadId);

            // (re-)run the matcher if requested OR no mapping draft yet
            if (forceRematch || existing.Count == 0)
            {
                var headers = ReadHeaders(upload);
                if (headers.Count > 0)
                {
                    existing = await _matcher.MatchAsync(headers, upload.DataCategory);
                    await _repo.SaveMappingsAsync(uploadId, existing);

                    var avg = existing.Count == 0 ? 0m : existing.Average(m => m.ConfidencePct);
                    var newStatus = existing.All(m => m.ConfidencePct >= 90m)
                        ? UploadStatus.Mapped
                        : UploadStatus.MappingRequired;
                    await _repo.UpdateStatusAsync(uploadId, newStatus, "system-matcher",
                                                  confidenceAvg: avg);

                    upload = await _repo.GetUploadByIdAsync(uploadId) ?? upload;
                }
            }

            // Standard-column universe for the underwriter dropdown comes from the
            // existing column-plotting SP — no separate StandardColumns table.
            var standard = await BuildStandardColumnsForViewAsync(upload, existing);

            var policyUploads = await _repo.GetUploadsByPolicyNoAsync(upload.PolicyNo);
            RolloverUpload? Pick(string category) =>
                policyUploads.FirstOrDefault(u =>
                    string.Equals(u.DataCategory, category, StringComparison.OrdinalIgnoreCase));

            var enrollmentUpload = Pick("Enrollment");
            var claimUpload      = Pick("Claim");
            var activeTab = string.Equals(upload.DataCategory, "Claim", StringComparison.OrdinalIgnoreCase)
                ? "Claim"
                : "Enrollment";

            return new UploadReviewModel
            {
                Upload           = upload,
                EnrollmentUpload = enrollmentUpload,
                ClaimUpload      = claimUpload,
                ActiveTab        = activeTab,
                Mappings         = existing,
                StandardColumns  = standard,
                AvgConfidence    = upload.MappingConfidenceAvg ?? 0m,
                NeedsAttention   = existing.Any(m => m.ConfidencePct < 90m)
            };
        }

        public async Task<RolloverUpload?> SaveMappingAsync(SaveMappingRequest req, string reviewedBy)
        {
            if (req == null) throw new ArgumentException("Empty mapping payload.");
            if (req.Mappings == null) req.Mappings = new();

            // Mark anything the underwriter explicitly mapped (vs the AI guess) as manual.
            foreach (var m in req.Mappings)
            {
                m.IsApproved = req.Approve;
            }

            if (req.Mappings.Count > 0)
                await _repo.SaveMappingsAsync(req.UploadId, req.Mappings);

            var avg = req.Mappings.Count == 0 ? 0m : req.Mappings.Average(m => m.ConfidencePct);
            var newStatus = req.Approve ? UploadStatus.Mapped : UploadStatus.UnderReview;
            await _repo.UpdateStatusAsync(req.UploadId, newStatus, reviewedBy,
                                          remarks: req.Remarks, confidenceAvg: avg);

            // On approve: persist every mapped Excel column into tbl_GMC_Master_Column
            // when that (Master Parameter, CurrentColumnName) pair is not already there.
            if (req.Approve)
            {
                var mapped = req.Mappings
                    .Where(m => !string.IsNullOrWhiteSpace(m.TargetColumn)
                             && !string.IsNullOrWhiteSpace(m.SourceColumn))
                    .Select(m => (masterColumn: m.TargetColumn!.Trim(), sourceColumn: m.SourceColumn.Trim()))
                    .ToList();

                if (mapped.Count > 0)
                {
                    try
                    {
                        var inserted = await _repo.EnsureMasterColumnMappingsAsync(mapped);
                        _log.LogInformation(
                            "Approved upload {Id}: ensured {Total} mappings in tbl_GMC_Master_Column ({New} new).",
                            req.UploadId, mapped.Count, inserted);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex,
                            "Failed to save approved mappings to tbl_GMC_Master_Column for upload {Id}.",
                            req.UploadId);
                    }

                    // Secondary: legacy SP path (same tables the old uploader used).
                    var upload = await _repo.GetUploadByIdAsync(req.UploadId);
                    if (upload != null)
                    {
                        try
                        {
                            await _repo.SaveLegacyMasterMappingAsync(upload.DataCategory, mapped);
                        }
                        catch (Exception ex)
                        {
                            _log.LogWarning(ex, "Legacy udsp_Save_GMC_*_MappingDatta skipped (non-fatal).");
                        }

                        try
                        {
                            var rowCount = await _ingestor.IngestAsync(upload, req.Mappings, reviewedBy);
                            _log.LogInformation(
                                "Approved upload {Id}: imported {Rows} Excel rows into {Cat} data table.",
                                req.UploadId, rowCount, upload.DataCategory);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex,
                                "Failed to import Excel data for approved upload {Id}.", req.UploadId);
                            throw new InvalidOperationException(
                                $"Mapping was saved but Excel import failed: {ex.Message}", ex);
                        }
                    }
                }
            }

            return await _repo.GetUploadByIdAsync(req.UploadId);
        }

        public async Task<PostApproveRedirect> GetPostApproveRedirectAsync(string policyNo)
        {
            var uploads = await _repo.GetUploadsByPolicyNoAsync(policyNo);
            var enroll = uploads.FirstOrDefault(u =>
                string.Equals(u.DataCategory, "Enrollment", StringComparison.OrdinalIgnoreCase));
            var claim = uploads.FirstOrDefault(u =>
                string.Equals(u.DataCategory, "Claim", StringComparison.OrdinalIgnoreCase));

            if (enroll == null || claim == null)
            {
                return new PostApproveRedirect
                {
                    OpenCalculator = false,
                    ReviewUploadId = (enroll ?? claim)?.UploadId,
                    Message = "Both Enrollment and Claim files must be uploaded and approved before opening the calculator."
                };
            }

            static bool IsMapped(RolloverUpload u) =>
                string.Equals(u.Status, UploadStatus.Mapped, StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.Status, UploadStatus.Completed, StringComparison.OrdinalIgnoreCase);

            if (IsMapped(enroll) && IsMapped(claim))
            {
                return new PostApproveRedirect
                {
                    OpenCalculator = true,
                    Message = "Enrollment and Claim mappings are complete. Opening GMC Calculator…"
                };
            }

            var next = !IsMapped(enroll) ? enroll : claim;
            var nextLabel = string.Equals(next.DataCategory, "Claim", StringComparison.OrdinalIgnoreCase)
                ? "Claim"
                : "Enrollment";

            return new PostApproveRedirect
            {
                OpenCalculator = false,
                ReviewUploadId = next.UploadId,
                Message = $"{nextLabel} column mapping is still required. Switching to the {nextLabel} tab…"
            };
        }

        public async Task RejectAsync(int uploadId, string reason, string reviewedBy)
        {
            await _repo.UpdateStatusAsync(uploadId, UploadStatus.Rejected, reviewedBy, remarks: reason);
        }

        // ----------------------------------------------------------------- helpers

        /// <summary>
        /// The Underwriter dropdown needs the full list of valid target columns.
        /// Source of truth is <c>tbl_GMC_Master_Column</c> (filtered by the
        /// upload's DataCategory).  This is the same universe the matcher uses,
        /// so the suggestions and the dropdown always agree.
        /// </summary>
        private async Task<List<StandardColumn>> BuildStandardColumnsForViewAsync(
            RolloverUpload upload, IEnumerable<ColumnMapping> currentMappings)
        {
            try
            {
                var rows = await _repo.GetMasterColumnsAsync(upload.DataCategory);
                if (rows.Count == 0)
                {
                    _log.LogWarning("tbl_GMC_Master_Column returned 0 rows for category {Cat}", upload.DataCategory);
                    return new List<StandardColumn>();
                }

                // One dropdown entry per distinct [Master Parameter]; skip legacy
                // rows where that field holds a comma-separated bundle of names.
                var standard = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.ColumnName)
                             && !r.ColumnName.Contains(',', StringComparison.Ordinal))
                    .GroupBy(r => r.ColumnName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select((g, idx) => new StandardColumn
                    {
                        ColumnName   = g.Key,
                        DataType     = "string",
                        DataCategory = upload.DataCategory,
                        IsRequired   = g.Any(x => x.IsRequired),
                        DisplayOrder = idx + 1
                    })
                    .OrderBy(s => s.ColumnName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Keep AI-suggested targets selectable even if not in master yet.
                var known = new HashSet<string>(standard.Select(s => s.ColumnName),
                                                StringComparer.OrdinalIgnoreCase);
                foreach (var m in currentMappings)
                {
                    if (string.IsNullOrWhiteSpace(m.TargetColumn)) continue;
                    var t = m.TargetColumn.Trim();
                    if (known.Add(t))
                    {
                        standard.Add(new StandardColumn
                        {
                            ColumnName   = t,
                            DataType     = "string",
                            DataCategory = upload.DataCategory,
                            IsRequired   = false,
                            DisplayOrder = standard.Count + 1
                        });
                    }
                }

                return standard.OrderBy(s => s.ColumnName, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Could not pull standard columns from tbl_GMC_Master_Column for upload {Id}", upload.UploadId);
                return new List<StandardColumn>();
            }
        }

        /// <summary>
        /// Reads only the header row of the uploaded Excel.
        /// </summary>
        private List<string> ReadHeaders(RolloverUpload upload)
        {
            var headers = new List<string>();
            try
            {
                var rel = (upload.FilePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
                var abs = Path.IsPathRooted(rel) ? rel : Path.Combine(_env.WebRootPath, rel);
                if (!File.Exists(abs))
                {
                    _log.LogWarning("Upload file not found at {Path}", abs);
                    return headers;
                }

                using var pkg = new ExcelPackage(new FileInfo(abs));
                var ws = pkg.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null) return headers;

                int startCol = ws.Dimension.Start.Column;
                int endCol   = ws.Dimension.End.Column;
                int headerRow = ws.Dimension.Start.Row;
                for (int c = startCol; c <= endCol; c++)
                {
                    var v = ws.Cells[headerRow, c].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(v)) headers.Add(v);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ReadHeaders failed for upload {UploadId}", upload.UploadId);
            }
            return headers;
        }
    }
}
