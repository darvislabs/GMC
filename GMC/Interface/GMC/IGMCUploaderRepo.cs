using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    public interface IGMCUploaderRepo
    {
        public GMCUploaderModel getIndustryName();
        public List<string> getSearchInsuraceCompanyName(string prifix);
        public List<string> getSearchTPA(string prifix);
        public Task<GMCUploaderModel> uploadData(GMCUploaderModel model);
        public Task<GMCUploaderModel> updateMaster(List<updatecolumnName> model, string typeofData, string tablename);
    }
}
