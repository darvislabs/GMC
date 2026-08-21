using System.Data;

namespace GMC.Models.GMC
{
    public class GMCCalculatorDetailsModel
    {
        public string excelfileName { get; set; }
        public string message { get; set; }
        public string error { get; set; }
        public string bussinessType { get; set; }
        public string tablename { get; set; }
        public string strPolicyNo { get; set; }
        public string strPolicyName { get; set; }
        public List<GMCPendingDetails> GMCPendingDetailsList { get; set; }
        public string PolicyNo { get; set; }
        public DateTime PolicyStartDate { get; set; }
        public DateTime Policy_End_date { get; set; }
        public DateTime ReconDate { get; set; }
        public string QuoteNumber { get; set; }
        public DataTable dtVersion { get; set; }
        public DataTable dtBurn { get; set; }
        public DataTable dtloading { get; set; }
        public DataTable dtloadingFactor { get; set; }
        public DataTable dtStanderedLoading { get; set; }
        public DataTable OtherLoadingFactor { get; set; }
        public DataTable dt { get; set; }
        public DataTable dt1 { get; set; }
        public DataTable rolloverDT { get; set; }
        public DataTable rolloverSIDT { get; set; }
        public string  versionNo { get; set; }
        public List<versionData> versionDatalist { get; set; }
        public decimal IceptionPremium { get; set; }
        public int FinalYearPremium { get; set; }
        public int OpeningLives { get; set; }
        public int ClosingLives { get; set; }
        public int AvgLives { get; set; }
        public int PolicyServiceDays { get; set; }
        public int OpeningEmployee { get; set; }
        public int ClosingEmployee { get; set; }
        public int AvgEmployee { get; set; }
        public long InceptionPremiumperlife { get; set; }
        public long ClosingPremiumperlife { get; set; }
        public long ClaimCost { get; set; }//12466158291

        public decimal Enrollment { get; set; }
        public decimal BurnCostClaim { get; set; }
        public long RcareEnrollment { get; set; }
        public decimal LossRatio { get; set; }
        public string UWRemarks { get; set; }
        public IFormFile myFile { get; set; }
        public IFormFile myFile1 { get; set; }
        public int NormalSublimit { get; set; }
        public int LSCSSublimit { get; set; }
        public string NormalLimitResult { get; set; }
        public string lscsLimitResult { get; set; }
        public string FinalEnrollmentpremium { get; set; }
        public List<factorData> factorDataList { get; set; }
        public string version { get; set; }
        public List<string[]> Fdata { get; set; }
        public string Sdata { get; set; }
        public decimal startRejection { get; set; }
        public decimal IBNR { get; set; }
        public string Claim_costPerLife { get; set; }
        public string Claim_CostPerEmployee { get; set; }
        public DataSet firstDataset { get; set; }
        public DataSet seconfDataset { get; set; }

    }

    public class versionData
    {
        public string VersionNumber { get; set; }
    }
    public enum bussinessCType
    {
        Rollover,
        Fresh,
        Renewal
    }
    public class standerdData
    {
        public List<string> tableData { get; set; }
        public string inputValue { get; set; }
    }
    public class factorData
    {
        public string Factors { get; set; }
        public string Loading { get; set; }
        public string Discount { get; set; }
        public string Loading_Discount_Amount_burn_cost_premium { get; set; }
        public string Loading_Discount_Amount_Enrollment_Premium { get; set; }
        public string Expiring_Limit { get; set; }
        public string Proposed_Limit { get; set; }
    }
    public class GMCPendingDetails
    {
        public string PolicyNo_unique { get; set; }
        public string PolicyName { get; set; }
        public string created_by { get; set; }
        public string insert_date { get; set; }
    }
    public class StanderdLoadingData
    {
        public string Loading_Factor { get; set; }
        public string LoadingPer { get; set; }
        public string BurnpremiumLoading { get; set; }
        public string enrollmentpremium { get; set; }
    }
}
