using GMC.Interface.GMC;
using GMC.Models.GMC;
using OfficeOpenXml;

namespace GMC.BL.GMC
{
    public class MappedExcelIngestor : IMappedExcelIngestor
    {
        private readonly IRolloverUploadRepo _repo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MappedExcelIngestor> _log;

        public MappedExcelIngestor(
            IRolloverUploadRepo repo,
            IWebHostEnvironment env,
            ILogger<MappedExcelIngestor> log)
        {
            _repo = repo;
            _env  = env;
            _log  = log;
        }

        public async Task<int> IngestAsync(
            RolloverUpload upload,
            IEnumerable<ColumnMapping> mappings,
            string importedBy)
        {
            var map = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SourceColumn)
                         && !string.IsNullOrWhiteSpace(m.TargetColumn))
                .GroupBy(m => m.SourceColumn.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().TargetColumn!.Trim(), StringComparer.OrdinalIgnoreCase);

            if (map.Count == 0)
                throw new InvalidOperationException("No mapped columns to import.");

            var absPath = ResolveFilePath(upload.FilePath);
            if (!File.Exists(absPath))
                throw new FileNotFoundException($"Upload file not found: {upload.FileName}", absPath);

            var rows = ReadMappedRows(absPath, map, upload, importedBy);
            if (rows.Count == 0)
                throw new InvalidOperationException("The Excel file has no data rows to import.");

            var columns = BuildColumnList(map);

            await _repo.InsertPolicyDataLogAsync(upload.DataCategory, upload.PolicyNo);
            await _repo.DeletePolicyDataAsync(upload.DataCategory, upload.PolicyNo);

            var inserted = await _repo.InsertPolicyDataRowsAsync(upload.DataCategory, columns, rows);
            _log.LogInformation(
                "Ingested {Rows} rows into {Category} data table for policy {Policy} (upload {Id}).",
                inserted, upload.DataCategory, upload.PolicyNo, upload.UploadId);
            return inserted;
        }

        private string ResolveFilePath(string filePath)
        {
            var rel = (filePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
            return Path.IsPathRooted(rel) ? rel : Path.Combine(_env.WebRootPath, rel);
        }

        private static List<Dictionary<string, object?>> ReadMappedRows(
            string absPath,
            IReadOnlyDictionary<string, string> sourceToTarget,
            RolloverUpload upload,
            string importedBy)
        {
            using var pkg = new ExcelPackage(new FileInfo(absPath));
            var ws = pkg.Workbook.Worksheets.FirstOrDefault()
                     ?? throw new InvalidOperationException("Excel workbook has no worksheets.");
            if (ws.Dimension == null)
                return new List<Dictionary<string, object?>>();

            int headerRow = ws.Dimension.Start.Row;
            int startCol  = ws.Dimension.Start.Column;
            int endCol    = ws.Dimension.End.Column;
            int endRow    = ws.Dimension.End.Row;

            var colTarget = new Dictionary<int, string>();
            for (int c = startCol; c <= endCol; c++)
            {
                var header = ws.Cells[headerRow, c].Text?.Trim();
                if (string.IsNullOrEmpty(header)) continue;
                if (sourceToTarget.TryGetValue(header, out var target))
                    colTarget[c] = target;
            }

            if (colTarget.Count == 0)
                throw new InvalidOperationException("None of the mapped Excel headers were found in the file.");

            var insertStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = new List<Dictionary<string, object?>>();

            for (int r = headerRow + 1; r <= endRow; r++)
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                bool hasValue = false;

                foreach (var (colIdx, target) in colTarget)
                {
                    var val = ws.Cells[r, colIdx].Value;
                    if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                        hasValue = true;
                    row[target] = NormalizeCell(val);
                }

                if (!hasValue) continue;

                row["PolicyNo_unique"]         = upload.PolicyNo;
                row["PolicyName"]              = upload.PolicyName ?? (object)DBNull.Value;
                row["Insurance_Company_Name"]  = upload.InsuranceCompany ?? (object)DBNull.Value;
                row["TPA"]                     = upload.TPA ?? (object)DBNull.Value;
                row["SubType"]                 = upload.SubType ?? (object)DBNull.Value;
                row["Industry_Name"]           = upload.IndustryName ?? (object)DBNull.Value;
                row["created_by"]              = importedBy;
                row["insert_date"]             = insertStamp;

                result.Add(row);
            }

            return result;
        }

        private static List<string> BuildColumnList(IReadOnlyDictionary<string, string> sourceToTarget)
        {
            var mapped = sourceToTarget.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string[] meta =
            {
                "PolicyNo_unique", "PolicyName", "Insurance_Company_Name",
                "TPA", "SubType", "Industry_Name", "created_by", "insert_date"
            };

            return mapped.Concat(meta).ToList();
        }

        private static object? NormalizeCell(object? val)
        {
            if (val == null) return DBNull.Value;
            if (val is string s && string.IsNullOrWhiteSpace(s)) return DBNull.Value;
            return val;
        }
    }
}
