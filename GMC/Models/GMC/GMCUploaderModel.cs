using System.ComponentModel.DataAnnotations;
using System.Data;

namespace GMC.Models.GMC
{
    public class GMCUploaderModel
    {
        public string message { get; set; }
        public string error { get; set; }
        public List<bussinessType> bussinessTypeList { get; set; }
        public string bussinessType { get; set; }
        public List<unit> unitList { get; set; }
        public string unit { get; set; }
        public List<subType> subTypeList { get; set; }
        public string subType { get; set; }
        public List<industryName> industryNameList { get; set; }
        public string industryName { get; set; }
        public List<typeofData> typeofDataList { get; set; }
        public string typeofData { get; set; }
        [RegularExpression(@"^\S.*?\S$", ErrorMessage = "Policy No cannot have leading or trailing whitespace.")]
        public string strPolicyNo { get; set; }
        public string strPolicyName { get; set; }
        public IFormFile myFile { get; set; }
        public string InsuranceCompanyName { get; set; }
        public string TPA { get; set; }
        public List<columnName> columnList { get; set; }
        public DataTable errorDT { get; set; }
        public string tablename { get; set; }
    }
    public enum bussinessType
    {
        Rollover,
        Fresh,
        Renewal
    }
    public enum unit
    {
        Internal,
        External
    }
    public enum subType
    {
        Main,
        Parent,
        Topup
    }
    public enum typeofData
    {
        Claim,
        Enrollment
    }

}
public class industryName
{
    public string Nature_of_Industry { get; set; }
}
public class columnName
{
    public string column { get; set; }
}
public class updatecolumnName
{
    public string masterColumn { get; set; }
    public string updateColumn { get; set; }
}