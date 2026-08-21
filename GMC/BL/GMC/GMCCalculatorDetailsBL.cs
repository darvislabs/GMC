using GMC.Interface.GMC;
using GMC.Models.GMC;
using System.Data;

namespace GMC.BL.GMC
{
    public class GMCCalculatorDetailsBL : IGMCCalculatorDetails
    {
        readonly IGMCCalculatorDetailsRepo _cal;
        public GMCCalculatorDetailsBL(IGMCCalculatorDetailsRepo cal)
        {
            _cal=cal;
        }
        public async Task<DataSet> BindBurnCost(GMCCalculatorDetailsModel model)
        {
            return await _cal.BindBurnCost(model);
        }

        public async Task<DataSet> BindBurnCostForRenewal(GMCCalculatorDetailsModel model)
        {
            return await _cal.BindBurnCostForRenewal(model);
        }

        public async Task<DataTable> BindMaternityCost(GMCCalculatorDetailsModel model)
        {
            return await _cal.BindMaternityCost(model);
        }

        public async Task<DataTable> BindVersionDetails(string PolicyNo)
        {
            return await _cal.BindVersionDetails(PolicyNo);
        }

        public async Task<DataSet> BindVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            return await _cal.BindVersionDetailsToControls(Policyno,VersionNumber);
        }

        public async Task<GMCCalculatorDetailsModel> DownloadRenewalVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            return await _cal.DownloadRenewalVersionDetailsToControls(Policyno, VersionNumber);
        }

        public async Task<GMCCalculatorDetailsModel> DownloadSummeryVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            return await _cal.DownloadSummeryVersionDetailsToControls(Policyno, VersionNumber);
        }

        public async Task<GMCCalculatorDetailsModel> DownloadVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            return await _cal.DownloadVersionDetailsToControls(Policyno, VersionNumber);
        }

        public async Task<DataTable> GetGMCPolicyLevelData(string PolicyNo)
        {
            return await _cal.GetGMCPolicyLevelData(PolicyNo);
        }

        public async Task<DataSet> GetTrendAnalysis(string PolicyNo, string FYYear)
        {
            return await _cal.GetTrendAnalysis(PolicyNo, FYYear);
        }

        public async Task<DataTable> GetGMCRolloverLiveData(string PolicyNo)
        {
            return await _cal.GetGMCRolloverLiveData(PolicyNo);
        }

        public async Task<GMCCalculatorDetailsModel> InsertGMCRollover(GMCCalculatorDetailsModel model)
        {
            return await _cal.InsertGMCRollover(model);
        }

        public async Task<DataTable> PolicyPendingDetails(string PolicyNo)
        {
            return await _cal.PolicyPendingDetails(PolicyNo); 
        }

        public async Task<int> SaveBurnCostDetails(List<string[]> Sdata, string version)
        {
            return await _cal.SaveBurnCostDetails(Sdata, version);
        }

        public async Task<int> SaveLoadFactorDetails(List<string[]> Sdata, string version)
        {
            return await _cal.SaveLoadFactorDetails(Sdata,version);
        }

        public Task<int> SaveLoadStanderdDetails(List<string[]> Sdata, string version)
        {
           return _cal.SaveLoadStanderdDetails(Sdata,version);
        }

        public async Task<DataSet> UpdateGridValue(string Factor, decimal ExistingLimit, decimal ProposedLimit, decimal BurnAmtPremium, decimal Enrollmentpremium)
        {
            return await _cal.UpdateGridValue(Factor,ExistingLimit,ProposedLimit,BurnAmtPremium,Enrollmentpremium);
        }

        public async Task<DataSet> UpdateGridValue_new(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno)
        {
           return await _cal.UpdateGridValue_new(Factor,DT,BurnAmtPremium,Enrollmentpremium,policyno);   
        }

        public async Task<DataSet> UpdateGridValue_new_lives(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno)
        {
            return await _cal.UpdateGridValue_new_lives(Factor,DT,BurnAmtPremium,Enrollmentpremium,policyno);
        }

        public async Task<GMCCalculatorDetailsModel> uploadFile(IFormFile myfilename, IFormFile filename1, string policyno)
        {
            return await _cal.uploadFile(myfilename, filename1,policyno);
        }
    }
}
