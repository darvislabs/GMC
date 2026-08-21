using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    /// <summary>
    /// Persistence for the rollover-upload pipeline.  All SQL is
    /// parameterised; no new stored procedures introduced — the only
    /// legacy SPs called are <c>udsp_GMS_Column_Plotting*</c> and
    /// <c>udsp_Save_GMC_*_MappingDatta</c>.
    /// </summary>
    public interface IRolloverUploadRepo
    {
        // ----- new tracking tables (inline parameterised SQL) ----------------
        Task<int>  InsertUploadAsync(RolloverUpload upload);
        Task<RolloverUpload?> GetUploadByIdAsync(int uploadId);
        /// <summary>Latest active Claim + Enrollment uploads for a policy (one per type).</summary>
        Task<List<RolloverUpload>> GetUploadsByPolicyNoAsync(string policyNo);
        Task<List<ColumnMapping>> GetMappingsAsync(int uploadId);
        Task<List<RolloverUpload>> GetPendingUploadsAsync(string role, string userName, string? status = null);
        Task UpdateStatusAsync(int uploadId, string status, string updatedBy,
                                string? remarks = null, decimal? confidenceAvg = null);
        Task SaveMappingsAsync(int uploadId, IEnumerable<ColumnMapping> mappings);

        // ----- master columns ------------------------------------------------
        /// <summary>
        /// Returns every standard column defined in <c>tbl_GMC_Master_Column</c>,
        /// optionally filtered to a DataCategory (Enrollment / Claim).  The
        /// reader is schema-tolerant — it auto-detects which physical column
        /// holds the name / category / synonyms / required / order, so the
        /// code keeps working even if the master table evolves.
        /// </summary>
        Task<List<MasterColumnRow>> GetMasterColumnsAsync(string? dataCategory = null);

        /// <summary>
        /// Inserts Excel→DB mapping rows into <c>tbl_GMC_Master_Column</c> when the
        /// pair (<c>Master Parameter</c>, <c>CurrentColumnName</c>) does not exist.
        /// Returns the number of rows inserted.
        /// </summary>
        Task<int> EnsureMasterColumnMappingsAsync(
            IEnumerable<(string masterColumn, string sourceColumn)> mappings);

        /// <summary>Legacy log SP before replacing policy rows in claim/enrollment tables.</summary>
        Task InsertPolicyDataLogAsync(string dataCategory, string policyNo);

        /// <summary>Deletes existing rows for a policy (Sp_delete_*_data).</summary>
        Task DeletePolicyDataAsync(string dataCategory, string policyNo);

        /// <summary>Bulk-insert mapped Excel rows into claim/enrollment data table.</summary>
        Task<int> InsertPolicyDataRowsAsync(
            string dataCategory,
            IReadOnlyList<string> columnNames,
            IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);

        // ----- legacy SPs (no new SQL objects) -------------------------------
        /// <summary>
        /// Calls the existing <c>udsp_GMS_Column_Plotting_enrollment</c> (or
        /// <c>udsp_GMS_Column_Plotting</c> for Claim).  Returns one row per
        /// MasterColumn with a possibly-empty matched input column.  Kept as a
        /// secondary alias source so the matcher still benefits from past
        /// underwriter corrections persisted by
        /// <c>udsp_Save_GMC_*_MappingDatta</c>.
        /// </summary>
        Task<List<LegacyMasterMatch>> RunLegacyColumnPlottingAsync(string excelColumnsCsv, string dataCategory);

        /// <summary>
        /// Calls the existing <c>udsp_Save_GMC_Enrollment_MappingDatta</c> (or
        /// the Claim variant) to persist manual corrections into the legacy
        /// master mapping table — same call path the existing uploader uses.
        /// </summary>
        Task SaveLegacyMasterMappingAsync(string dataCategory, IEnumerable<(string masterColumn, string sourceColumn)> mappings);

        // ----- dashboard (inline SQL) ----------------------------------------
        Task<DashboardCounts> GetDashboardCountsAsync(string role, string userName);
        Task<List<DashboardTimePoint>> GetDashboardTimeSeriesAsync(string role, string userName, int days = 30);
    }

    /// <summary> One row from <c>udsp_GMS_Column_Plotting*</c>. </summary>
    public sealed class LegacyMasterMatch
    {
        public string MasterColumn { get; set; } = string.Empty;
        public string MatchedInput { get; set; } = string.Empty;
        public int    Flag         { get; set; }   // 0 = unmatched, 1 = matched
    }

    /// <summary> One row from <c>tbl_GMC_Master_Column</c>. </summary>
    public sealed class MasterColumnRow
    {
        /// <summary>Standardised target column name (e.g. "MemberId").</summary>
        public string  ColumnName   { get; set; } = string.Empty;

        /// <summary>"Enrollment" / "Claim" / "Both" / null if not categorised.</summary>
        public string? DataCategory { get; set; }

        /// <summary>Comma- or pipe-separated alternative names from the table (optional).</summary>
        public string? Synonyms     { get; set; }

        public bool    IsRequired   { get; set; }
        public int     DisplayOrder { get; set; }
        public string? DataType     { get; set; }
    }
}
