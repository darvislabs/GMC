using System.Data;
using System.Data.SqlClient;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    /// <summary>
    /// Persists rollover-upload tracking + per-upload draft mappings via inline
    /// parameterised SQL against the two new tables (tbl_GMC_RolloverUpload,
    /// tbl_GMC_ColumnMapping).  AI-mapping work is delegated to the existing
    /// stored procedures <c>udsp_GMS_Column_Plotting*</c> +
    /// <c>udsp_Save_GMC_*_MappingDatta</c> — no new SPs are introduced.
    /// </summary>
    public class RolloverUploadRepo : IRolloverUploadRepo
    {
        private readonly string _conn;
        private readonly ILogger<RolloverUploadRepo> _log;

        public RolloverUploadRepo(IConfiguration config, ILogger<RolloverUploadRepo> log)
        {
            _conn = config["ConnectionStrings:ConnectionToTele_Dashboard"]
                    ?? throw new InvalidOperationException("Connection string 'ConnectionToTele_Dashboard' is missing.");
            _log  = log;
        }

        // =====================================================================
        //  New tracking tables — inline parameterised SQL only
        // =====================================================================

        public async Task<int> InsertUploadAsync(RolloverUpload u)
        {
            const string sql = @"
INSERT INTO dbo.tbl_GMC_RolloverUpload
    (PolicyNo, PolicyName, InsuranceCompany, TPA, IndustryName, SubType,
     DataCategory, FileName, FilePath, TotalRows, TotalColumns,
     UploadedBy, Status)
VALUES
    (@PolicyNo, @PolicyName, @InsuranceCompany, @TPA, @IndustryName, @SubType,
     @DataCategory, @FileName, @FilePath, @TotalRows, @TotalColumns,
     @UploadedBy, @Status);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@PolicyNo",         u.PolicyNo);
            cmd.Parameters.AddWithValue("@PolicyName",       (object?)u.PolicyName       ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InsuranceCompany", (object?)u.InsuranceCompany ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TPA",              (object?)u.TPA              ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IndustryName",     (object?)u.IndustryName     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SubType",          (object?)u.SubType          ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataCategory",     u.DataCategory ?? "Enrollment");
            cmd.Parameters.AddWithValue("@FileName",         u.FileName);
            cmd.Parameters.AddWithValue("@FilePath",         u.FilePath);
            cmd.Parameters.AddWithValue("@TotalRows",        (object?)u.TotalRows        ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TotalColumns",     (object?)u.TotalColumns     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UploadedBy",       u.UploadedBy);
            cmd.Parameters.AddWithValue("@Status",           u.Status ?? UploadStatus.Pending);

            await c.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<RolloverUpload?> GetUploadByIdAsync(int uploadId)
        {
            const string sql = @"
SELECT UploadId, PolicyNo, PolicyName, InsuranceCompany, TPA, IndustryName, SubType,
       DataCategory, FileName, FilePath, TotalRows, TotalColumns, Status,
       UploadedBy, UploadedOn, AssignedUnderwriter, ReviewedBy, ReviewedOn,
       MappingConfidenceAvg, Remarks
FROM   dbo.tbl_GMC_RolloverUpload WITH (NOLOCK)
WHERE  UploadId = @UploadId;";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@UploadId", uploadId);

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? ReadUpload(rdr) : null;
        }

        public async Task<List<RolloverUpload>> GetUploadsByPolicyNoAsync(string policyNo)
        {
            if (string.IsNullOrWhiteSpace(policyNo))
                return new List<RolloverUpload>();

            const string sql = @"
SELECT UploadId, PolicyNo, PolicyName, InsuranceCompany, TPA, IndustryName, SubType,
       DataCategory, FileName, FilePath, TotalRows, TotalColumns, Status,
       UploadedBy, UploadedOn, AssignedUnderwriter, ReviewedBy, ReviewedOn,
       MappingConfidenceAvg, Remarks
FROM   dbo.tbl_GMC_RolloverUpload WITH (NOLOCK)
WHERE  IsActive = 1
  AND  PolicyNo = @PolicyNo
ORDER  BY UploadedOn DESC;";

            var list = new List<RolloverUpload>();
            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@PolicyNo", policyNo.Trim());

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(ReadUpload(rdr));
            return list;
        }

        public async Task<List<ColumnMapping>> GetMappingsAsync(int uploadId)
        {
            const string sql = @"
SELECT MappingId, UploadId, SourceColumn, TargetColumn, ConfidencePct,
       IsManual, IsApproved, SuggestedBy, CreatedOn
FROM   dbo.tbl_GMC_ColumnMapping WITH (NOLOCK)
WHERE  UploadId = @UploadId
ORDER  BY MappingId;";

            var list = new List<ColumnMapping>();
            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@UploadId", uploadId);

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new ColumnMapping
                {
                    MappingId     = rdr.GetInt32(rdr.GetOrdinal("MappingId")),
                    UploadId      = rdr.GetInt32(rdr.GetOrdinal("UploadId")),
                    SourceColumn  = rdr.GetString(rdr.GetOrdinal("SourceColumn")),
                    TargetColumn  = rdr.IsDBNull(rdr.GetOrdinal("TargetColumn")) ? null : rdr.GetString(rdr.GetOrdinal("TargetColumn")),
                    ConfidencePct = rdr.GetDecimal(rdr.GetOrdinal("ConfidencePct")),
                    IsManual      = rdr.GetBoolean(rdr.GetOrdinal("IsManual")),
                    IsApproved    = rdr.GetBoolean(rdr.GetOrdinal("IsApproved")),
                    SuggestedBy   = rdr.IsDBNull(rdr.GetOrdinal("SuggestedBy")) ? null : rdr.GetString(rdr.GetOrdinal("SuggestedBy"))
                });
            }
            return list;
        }

        public async Task<List<RolloverUpload>> GetPendingUploadsAsync(string role, string userName, string? status = null)
        {
            const string sql = @"
SELECT UploadId, PolicyNo, PolicyName, InsuranceCompany, TPA, IndustryName, SubType,
       DataCategory, FileName, FilePath, TotalRows, TotalColumns, Status,
       UploadedBy, UploadedOn, AssignedUnderwriter, ReviewedBy, ReviewedOn,
       MappingConfidenceAvg, Remarks
FROM   dbo.tbl_GMC_RolloverUpload WITH (NOLOCK)
WHERE  IsActive = 1
  AND  (@Status IS NULL OR Status = @Status)
  AND  (
            @Role = 'Admin'
         OR (@Role = 'SalesPerson' AND UploadedBy = @UserName)
         OR (@Role = 'Underwriter' AND Status <> 'Completed')
       )
ORDER BY UploadedOn DESC;";

            var list = new List<RolloverUpload>();
            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@Role",     role ?? string.Empty);
            cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);
            cmd.Parameters.AddWithValue("@Status",   (object?)status ?? DBNull.Value);

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(ReadUpload(rdr));
            return list;
        }

        public async Task UpdateStatusAsync(int uploadId, string status, string updatedBy,
                                            string? remarks = null, decimal? confidenceAvg = null)
        {
            const string sql = @"
UPDATE dbo.tbl_GMC_RolloverUpload
   SET Status               = @Status,
       Remarks              = COALESCE(@Remarks, Remarks),
       MappingConfidenceAvg = COALESCE(@ConfidenceAvg, MappingConfidenceAvg),
       ReviewedBy           = CASE WHEN @Status IN ('UnderReview','Mapped','Completed','Rejected')
                                   THEN @UpdatedBy ELSE ReviewedBy END,
       ReviewedOn           = CASE WHEN @Status IN ('UnderReview','Mapped','Completed','Rejected')
                                   THEN GETDATE() ELSE ReviewedOn END,
       AssignedUnderwriter  = CASE WHEN @Status = 'UnderReview' AND AssignedUnderwriter IS NULL
                                   THEN @UpdatedBy ELSE AssignedUnderwriter END
 WHERE UploadId = @UploadId;";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@UploadId",      uploadId);
            cmd.Parameters.AddWithValue("@Status",        status);
            cmd.Parameters.AddWithValue("@UpdatedBy",     updatedBy ?? string.Empty);
            cmd.Parameters.AddWithValue("@Remarks",       (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConfidenceAvg", (object?)confidenceAvg ?? DBNull.Value);

            await c.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveMappingsAsync(int uploadId, IEnumerable<ColumnMapping> mappings)
        {
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();
            using var tx = (SqlTransaction)await c.BeginTransactionAsync();

            // 1) clear existing
            using (var del = new SqlCommand("DELETE FROM dbo.tbl_GMC_ColumnMapping WHERE UploadId = @UploadId", c, tx))
            {
                del.Parameters.AddWithValue("@UploadId", uploadId);
                await del.ExecuteNonQueryAsync();
            }

            // 2) insert each row (small N — one per Excel column, typically <50)
            const string insertSql = @"
INSERT INTO dbo.tbl_GMC_ColumnMapping
    (UploadId, SourceColumn, TargetColumn, ConfidencePct, IsManual, IsApproved, SuggestedBy)
VALUES
    (@UploadId, @SourceColumn, @TargetColumn, @ConfidencePct, @IsManual, @IsApproved, @SuggestedBy);";

            decimal sumConf = 0m; int count = 0;
            foreach (var m in mappings)
            {
                using var cmd = new SqlCommand(insertSql, c, tx);
                cmd.Parameters.AddWithValue("@UploadId",      uploadId);
                cmd.Parameters.AddWithValue("@SourceColumn",  m.SourceColumn ?? string.Empty);
                cmd.Parameters.AddWithValue("@TargetColumn",  (object?)m.TargetColumn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ConfidencePct", m.ConfidencePct);
                cmd.Parameters.AddWithValue("@IsManual",      m.IsManual);
                cmd.Parameters.AddWithValue("@IsApproved",    m.IsApproved);
                cmd.Parameters.AddWithValue("@SuggestedBy",   (object?)m.SuggestedBy ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                sumConf += m.ConfidencePct;
                count++;
            }

            // 3) refresh average confidence on header
            decimal avg = count == 0 ? 0m : Math.Round(sumConf / count, 2);
            using (var upd = new SqlCommand(
                "UPDATE dbo.tbl_GMC_RolloverUpload SET MappingConfidenceAvg = @Avg WHERE UploadId = @UploadId", c, tx))
            {
                upd.Parameters.AddWithValue("@Avg", avg);
                upd.Parameters.AddWithValue("@UploadId", uploadId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        // =====================================================================
        //  Master columns — read from tbl_GMC_Master_Column (schema-tolerant)
        // =====================================================================

        public async Task<List<MasterColumnRow>> GetMasterColumnsAsync(string? dataCategory = null)
        {
            var list = new List<MasterColumnRow>();

            using var c = new SqlConnection(_conn);
            await c.OpenAsync();

            // 1) Discover which columns the master table actually has so we
            //    don't blow up on a slightly different schema.
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var sch = new SqlCommand(@"
SELECT COLUMN_NAME
FROM   INFORMATION_SCHEMA.COLUMNS
WHERE  TABLE_NAME = 'tbl_GMC_Master_Column'", c))
            using (var r = await sch.ExecuteReaderAsync())
            {
                while (await r.ReadAsync()) cols.Add(r.GetString(0));
            }

            if (cols.Count == 0)
            {
                _log.LogWarning("tbl_GMC_Master_Column does not exist or has no columns.");
                return list;
            }

            // Production GMC schema: [Master Parameter] = DB target column,
            // [CurrentColumnName] = Excel / alias label, [Ismandatory] = 0/1.
            string? nameCol = PickCol(cols, "Master Parameter", "MasterColumn", "ColumnName",
                                           "MasterColumnName", "DBColumnName", "Column_Name", "Name");
            if (nameCol == null)
            {
                _log.LogWarning("tbl_GMC_Master_Column has no recognisable name column " +
                                "(expected Master Parameter / MasterColumn / ColumnName / Name).");
                return list;
            }

            string? catCol = PickCol(cols, "DataCategory", "Category", "TypeOfData");
            string? synCol = PickCol(cols, "CurrentColumnName", "Synonyms", "Aliases", "AliasNames",
                                          "InputColumns", "InputColumn");
            string? reqCol = PickCol(cols, "Ismandatory", "IsRequired", "Required", "Mandatory");
            string? ordCol = PickCol(cols, "DisplayOrder", "SortOrder", "OrderNo", "Sequence");
            string? actCol = PickCol(cols, "IsActive", "Active", "IsDeleted");
            string? dtCol  = PickCol(cols, "ColumnDataType", "SqlType");

            // If catCol was picked from "DataType" but it's also the only datatype
            // column, don't reuse it for dtCol — but never select the same physical
            // column twice or T-SQL will complain.
            if (dtCol != null && string.Equals(dtCol, catCol, StringComparison.OrdinalIgnoreCase))
                dtCol = null;

            // 2) Build the SELECT.
            var select = new System.Text.StringBuilder();
            select.Append("SELECT [").Append(nameCol).Append("] AS Name");
            if (catCol != null) select.Append(", [").Append(catCol).Append("] AS DataCategory");
            if (synCol != null) select.Append(", [").Append(synCol).Append("] AS Synonyms");
            if (reqCol != null) select.Append(", [").Append(reqCol).Append("] AS IsRequired");
            if (ordCol != null) select.Append(", [").Append(ordCol).Append("] AS DisplayOrder");
            if (dtCol  != null) select.Append(", [").Append(dtCol).Append("] AS DataType");

            select.Append(" FROM dbo.tbl_GMC_Master_Column WITH (NOLOCK)");

            var wheres = new List<string>();
            if (actCol != null)
            {
                // IsDeleted columns are inverted
                if (string.Equals(actCol, "IsDeleted", StringComparison.OrdinalIgnoreCase))
                    wheres.Add($"ISNULL([{actCol}], 0) = 0");
                else
                    wheres.Add($"ISNULL([{actCol}], 1) = 1");
            }
            bool useCatFilter = catCol != null && !string.IsNullOrWhiteSpace(dataCategory);
            if (useCatFilter)
            {
                wheres.Add($"([{catCol}] = @DataCategory OR [{catCol}] = 'Both' OR " +
                           $"[{catCol}] = 'All' OR [{catCol}] IS NULL OR LTRIM(RTRIM([{catCol}])) = '')");
            }
            if (wheres.Count > 0) select.Append(" WHERE ").Append(string.Join(" AND ", wheres));

            if (ordCol != null) select.Append($" ORDER BY [{ordCol}], [{nameCol}]");
            else                select.Append($" ORDER BY [{nameCol}]");

            using var cmd = new SqlCommand(select.ToString(), c);
            if (useCatFilter) cmd.Parameters.AddWithValue("@DataCategory", dataCategory!);

            using var rdr = await cmd.ExecuteReaderAsync();
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rdr.FieldCount; i++) present.Add(rdr.GetName(i));

            int order = 1;
            while (await rdr.ReadAsync())
            {
                var name = rdr["Name"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                int? parsedOrder = null;
                if (present.Contains("DisplayOrder"))
                {
                    var v = rdr["DisplayOrder"];
                    if (v != null && v != DBNull.Value &&
                        int.TryParse(v.ToString(), out var n)) parsedOrder = n;
                }

                list.Add(new MasterColumnRow
                {
                    ColumnName   = name!,
                    DataCategory = present.Contains("DataCategory") ? rdr["DataCategory"]?.ToString() : null,
                    Synonyms     = present.Contains("Synonyms")     ? rdr["Synonyms"]?.ToString()     : null,
                    IsRequired   = present.Contains("IsRequired")   && TryBool(rdr["IsRequired"]),
                    DisplayOrder = parsedOrder ?? order,
                    DataType     = present.Contains("DataType")     ? rdr["DataType"]?.ToString()     : null
                });
                order++;
            }

            return list;
        }

        private static string? PickCol(HashSet<string> available, params string[] candidates)
        {
            foreach (var c in candidates)
                if (available.Contains(c)) return c;
            return null;
        }

        private static bool TryBool(object? v)
        {
            if (v == null || v == DBNull.Value) return false;
            switch (v)
            {
                case bool b:   return b;
                case int i:    return i != 0;
                case long l:   return l != 0;
                case short s:  return s != 0;
                case byte by:  return by != 0;
                case string s:
                    if (bool.TryParse(s, out var bs)) return bs;
                    if (int.TryParse(s, out var ns))  return ns != 0;
                    return s.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                           s.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }

        public async Task<int> EnsureMasterColumnMappingsAsync(
            IEnumerable<(string masterColumn, string sourceColumn)> mappings)
        {
            var list = mappings?
                .Where(m => !string.IsNullOrWhiteSpace(m.masterColumn)
                         && !string.IsNullOrWhiteSpace(m.sourceColumn)
                         && !m.masterColumn.Contains(',', StringComparison.Ordinal))
                .Select(m => (master: m.masterColumn.Trim(), source: m.sourceColumn.Trim()))
                .ToList();
            if (list == null || list.Count == 0) return 0;

            const string sql = @"
IF NOT EXISTS (
    SELECT 1
    FROM   dbo.tbl_GMC_Master_Column WITH (NOLOCK)
    WHERE  [Master Parameter]  = @Master
      AND  [CurrentColumnName] = @Source
)
BEGIN
    INSERT INTO dbo.tbl_GMC_Master_Column ([Master Parameter], [CurrentColumnName], [Ismandatory])
    VALUES (@Master, @Source, @Mandatory);
END";

            int inserted = 0;
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();
            foreach (var (master, source) in list)
            {
                using var cmd = new SqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@Master",    master);
                cmd.Parameters.AddWithValue("@Source",    source);
                cmd.Parameters.AddWithValue("@Mandatory", 0);
                var n = await cmd.ExecuteNonQueryAsync();
                if (n > 0) inserted++;
            }
            return inserted;
        }

        public async Task InsertPolicyDataLogAsync(string dataCategory, string policyNo)
        {
            var isClaim = string.Equals(dataCategory, "Claim", StringComparison.OrdinalIgnoreCase);
            var sp = isClaim ? "Sp_insert_Claim_log" : "Sp_insert_Enrollment_log";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sp, c) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@policyno", policyNo ?? string.Empty);
            await c.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePolicyDataAsync(string dataCategory, string policyNo)
        {
            var isClaim = string.Equals(dataCategory, "Claim", StringComparison.OrdinalIgnoreCase);
            var sp = isClaim ? "Sp_delete_Claim_data" : "Sp_delete_Enrollment_data";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sp, c) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@policyno", policyNo ?? string.Empty);
            await c.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> InsertPolicyDataRowsAsync(
            string dataCategory,
            IReadOnlyList<string> columnNames,
            IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            if (columnNames.Count == 0 || rows.Count == 0) return 0;

            var table = string.Equals(dataCategory, "Claim", StringComparison.OrdinalIgnoreCase)
                ? "dbo.tbl_GMC_Claim_Data_new"
                : "dbo.tbl_GMC_Enrollment_Data";

            var bracketedCols = columnNames.Select(SqlBracket).ToList();
            var paramNames    = columnNames.Select((_, i) => $"@p{i}").ToList();
            var sql = $"INSERT INTO {table} ({string.Join(", ", bracketedCols)}) VALUES ({string.Join(", ", paramNames)})";

            int inserted = 0;
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();
            using var cmd = new SqlCommand(sql, c);

            foreach (var row in rows)
            {
                cmd.Parameters.Clear();
                for (int i = 0; i < columnNames.Count; i++)
                {
                    var col = columnNames[i];
                    row.TryGetValue(col, out var val);
                    cmd.Parameters.AddWithValue(paramNames[i], val ?? DBNull.Value);
                }
                inserted += await cmd.ExecuteNonQueryAsync();
            }

            return inserted;
        }

        private static string SqlBracket(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "[Unknown]";
            return "[" + name.Trim().Replace("]", "]]") + "]";
        }

        // =====================================================================
        //  Legacy SP wrappers — no new SQL objects
        // =====================================================================

        public async Task<List<LegacyMasterMatch>> RunLegacyColumnPlottingAsync(string excelColumnsCsv, string dataCategory)
        {
            var spName = string.Equals(dataCategory, "Claim", StringComparison.OrdinalIgnoreCase)
                ? "udsp_GMS_Column_Plotting"
                : "udsp_GMS_Column_Plotting_enrollment";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(spName, c) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            cmd.Parameters.AddWithValue("@string",     excelColumnsCsv ?? string.Empty);
            cmd.Parameters.AddWithValue("@typeofData", dataCategory ?? "Enrollment");

            var result = new List<LegacyMasterMatch>();
            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                // Existing SP returns at least MasterColumn, ipcolumn, and a numeric flag at ordinal 2
                var master = rdr["MasterColumn"]?.ToString() ?? string.Empty;
                var matched = rdr["ipcolumn"]?.ToString() ?? string.Empty;
                int flag = 0;
                if (rdr.FieldCount > 2)
                {
                    var v = rdr.GetValue(2);
                    if (v != null && v != DBNull.Value && int.TryParse(v.ToString(), out var f)) flag = f;
                }
                result.Add(new LegacyMasterMatch { MasterColumn = master, MatchedInput = matched, Flag = flag });
            }
            return result;
        }

        public async Task SaveLegacyMasterMappingAsync(
            string dataCategory,
            IEnumerable<(string masterColumn, string sourceColumn)> mappings)
        {
            var list = mappings?.Where(m => !string.IsNullOrWhiteSpace(m.masterColumn) &&
                                            !string.IsNullOrWhiteSpace(m.sourceColumn)).ToList();
            if (list == null || list.Count == 0) return;

            // Existing SP signature: @MasterParameter = "m1,m2,m3*s1,s2,s3"
            var masters = string.Join(",", list.Select(x => x.masterColumn));
            var inputs  = string.Join(",", list.Select(x => x.sourceColumn));
            var param   = masters + "*" + inputs;

            var spName = string.Equals(dataCategory, "Claim", StringComparison.OrdinalIgnoreCase)
                ? "udsp_Save_GMC_Claim_MappingDatta"
                : "udsp_Save_GMC_Enrollment_MappingDatta";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(spName, c) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            cmd.Parameters.AddWithValue("@MasterParameter", param);
            await c.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // =====================================================================
        //  Dashboard — inline SQL, no new SPs
        // =====================================================================

        public async Task<DashboardCounts> GetDashboardCountsAsync(string role, string userName)
        {
            const string sql = @"
SELECT
    SUM(CASE WHEN Status = 'Pending'          THEN 1 ELSE 0 END) AS Pending,
    SUM(CASE WHEN Status = 'MappingRequired'  THEN 1 ELSE 0 END) AS MappingRequired,
    SUM(CASE WHEN Status = 'UnderReview'      THEN 1 ELSE 0 END) AS UnderReview,
    SUM(CASE WHEN Status = 'Mapped'           THEN 1 ELSE 0 END) AS Mapped,
    SUM(CASE WHEN Status = 'Completed'        THEN 1 ELSE 0 END) AS Completed,
    SUM(CASE WHEN Status = 'Rejected'         THEN 1 ELSE 0 END) AS Rejected,
    COUNT(*)                                                     AS Total,
    SUM(CASE WHEN CAST(UploadedOn AS DATE) = CAST(GETDATE() AS DATE)
             THEN 1 ELSE 0 END)                                  AS Today,
    SUM(CASE WHEN UploadedOn >= DATEADD(DAY, -7, GETDATE())
             THEN 1 ELSE 0 END)                                  AS Last7Days
FROM dbo.tbl_GMC_RolloverUpload WITH (NOLOCK)
WHERE IsActive = 1
  AND (
        @Role = 'Admin'
     OR (@Role = 'SalesPerson' AND UploadedBy = @UserName)
     OR  @Role = 'Underwriter'
      );";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@Role",     role     ?? string.Empty);
            cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return new DashboardCounts();

            int Z(string col) => rdr.IsDBNull(rdr.GetOrdinal(col)) ? 0 : Convert.ToInt32(rdr[col]);
            return new DashboardCounts
            {
                Pending         = Z("Pending"),
                MappingRequired = Z("MappingRequired"),
                UnderReview     = Z("UnderReview"),
                Mapped          = Z("Mapped"),
                Completed       = Z("Completed"),
                Rejected        = Z("Rejected"),
                Total           = Z("Total"),
                Today           = Z("Today"),
                Last7Days       = Z("Last7Days")
            };
        }

        public async Task<List<DashboardTimePoint>> GetDashboardTimeSeriesAsync(string role, string userName, int days = 30)
        {
            const string sql = @"
;WITH d AS
(
    SELECT CAST(DATEADD(DAY, -n, GETDATE()) AS DATE) AS Day
    FROM   (SELECT TOP (@Days) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) - 1 AS n
            FROM   sys.all_objects) x
)
SELECT  d.Day,
        ISNULL(SUM(CASE WHEN u.Status <> 'Completed' THEN 1 ELSE 0 END), 0) AS InProgress,
        ISNULL(SUM(CASE WHEN u.Status =  'Completed' THEN 1 ELSE 0 END), 0) AS Completed,
        ISNULL(COUNT(u.UploadId), 0)                                        AS Total
FROM    d
LEFT JOIN dbo.tbl_GMC_RolloverUpload u WITH (NOLOCK)
       ON CAST(u.UploadedOn AS DATE) = d.Day
      AND u.IsActive = 1
      AND (
              @Role = 'Admin'
           OR (@Role = 'SalesPerson' AND u.UploadedBy = @UserName)
           OR  @Role = 'Underwriter'
          )
GROUP BY d.Day
ORDER BY d.Day;";

            var list = new List<DashboardTimePoint>();
            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@Role",     role     ?? string.Empty);
            cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);
            cmd.Parameters.AddWithValue("@Days",     days);

            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new DashboardTimePoint
                {
                    Day        = rdr.GetDateTime(rdr.GetOrdinal("Day")),
                    InProgress = Convert.ToInt32(rdr["InProgress"]),
                    Completed  = Convert.ToInt32(rdr["Completed"]),
                    Total      = Convert.ToInt32(rdr["Total"])
                });
            }
            return list;
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static RolloverUpload ReadUpload(SqlDataReader rdr)
        {
            string? S(string col) => rdr.IsDBNull(rdr.GetOrdinal(col)) ? null : rdr.GetString(rdr.GetOrdinal(col));
            int?    I(string col) => rdr.IsDBNull(rdr.GetOrdinal(col)) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal(col));
            decimal? D(string col) => rdr.IsDBNull(rdr.GetOrdinal(col)) ? (decimal?)null : rdr.GetDecimal(rdr.GetOrdinal(col));
            DateTime? T(string col) => rdr.IsDBNull(rdr.GetOrdinal(col)) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal(col));

            return new RolloverUpload
            {
                UploadId             = rdr.GetInt32(rdr.GetOrdinal("UploadId")),
                PolicyNo             = rdr.GetString(rdr.GetOrdinal("PolicyNo")),
                PolicyName           = S("PolicyName"),
                InsuranceCompany     = S("InsuranceCompany"),
                TPA                  = S("TPA"),
                IndustryName         = S("IndustryName"),
                SubType              = S("SubType"),
                DataCategory         = rdr.GetString(rdr.GetOrdinal("DataCategory")),
                FileName             = rdr.GetString(rdr.GetOrdinal("FileName")),
                FilePath             = rdr.GetString(rdr.GetOrdinal("FilePath")),
                TotalRows            = I("TotalRows"),
                TotalColumns         = I("TotalColumns"),
                Status               = rdr.GetString(rdr.GetOrdinal("Status")),
                UploadedBy           = rdr.GetString(rdr.GetOrdinal("UploadedBy")),
                UploadedOn           = rdr.GetDateTime(rdr.GetOrdinal("UploadedOn")),
                AssignedUnderwriter  = S("AssignedUnderwriter"),
                ReviewedBy           = S("ReviewedBy"),
                ReviewedOn           = T("ReviewedOn"),
                MappingConfidenceAvg = D("MappingConfidenceAvg"),
                Remarks              = S("Remarks")
            };
        }
    }
}
