using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    /// <summary>
    /// After underwriter approve, loads the mapped Excel rows into
    /// <c>tbl_GMC_Claim_Data_new</c> or <c>tbl_GMC_Enrollment_Data</c>.
    /// </summary>
    public interface IMappedExcelIngestor
    {
        Task<int> IngestAsync(RolloverUpload upload, IEnumerable<ColumnMapping> mappings, string importedBy);
    }

}
