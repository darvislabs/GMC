using GMC.Models.GMC;
using System.Data;

namespace GMC.Interface.GMC
{
    public interface IGMCCalculatorDetailsRepo
    {
        Task<GMCCalculatorDetailsModel> InsertGMCRollover(GMCCalculatorDetailsModel model);
        Task<int> SaveBurnCostDetails(List<string[]> Sdata, string version);
        Task<int> SaveLoadFactorDetails(List<string[]> Sdata,string version);
        Task<int> SaveLoadStanderdDetails(List<string[]> Sdata, string version);
        Task<GMCCalculatorDetailsModel> uploadFile(IFormFile myfilename, IFormFile filename1, string policyno);
        Task<GMCCalculatorDetailsModel> DownloadSummeryVersionDetailsToControls(string Policyno, string VersionNumber);
        Task<GMCCalculatorDetailsModel> DownloadVersionDetailsToControls(string Policyno, string VersionNumber);
        Task<GMCCalculatorDetailsModel> DownloadRenewalVersionDetailsToControls(string Policyno, string VersionNumber);
        Task<DataSet> UpdateGridValue_new(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno);
        Task<DataSet> UpdateGridValue_new_lives(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno);
        Task<DataSet> UpdateGridValue(string Factor, decimal ExistingLimit, decimal ProposedLimit, decimal BurnAmtPremium, decimal Enrollmentpremium);
        Task<DataSet> BindVersionDetailsToControls(string Policyno, string VersionNumber);
        Task<DataTable> BindMaternityCost(GMCCalculatorDetailsModel model);
        Task<DataTable> PolicyPendingDetails(string PolicyNo);
        Task<DataSet> BindBurnCost(GMCCalculatorDetailsModel model);
        Task<DataSet> BindBurnCostForRenewal(GMCCalculatorDetailsModel model);
        Task<DataTable> BindVersionDetails(string PolicyNo);
        Task<DataTable> GetGMCPolicyLevelData(string PolicyNo);
        Task<DataTable> GetGMCRolloverLiveData(string PolicyNo);
        Task<DataSet> GetTrendAnalysis(string PolicyNo, string FYYear);

    }
}
