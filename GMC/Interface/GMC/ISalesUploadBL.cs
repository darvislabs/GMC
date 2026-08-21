using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    public interface ISalesUploadBL
    {
        Task<List<string>> GetIndustryListAsync();
        /// <summary>Top-N rows from dbo.tbl_company_list, alpha-sorted.</summary>
        Task<List<string>> GetCompanyListAsync(int top = 500);
        /// <summary>Top-N rows from dbo.tbl_TPA_list, alpha-sorted.</summary>
        Task<List<string>> GetTPAListAsync(int top = 500);
        Task<SalesUploadResponse> UploadAsync(SalesUploadForm form, string uploadedBy);
    }
}
