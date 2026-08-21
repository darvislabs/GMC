using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.BL.GMC
{
    public class GMCUploaderBL : IGMCUploader
    {
        readonly IGMCUploaderRepo _uploader;
        public GMCUploaderBL(IGMCUploaderRepo uploader)
        {
            _uploader = uploader;
        }
        public GMCUploaderModel getIndustryName() {
         
            return _uploader.getIndustryName();
        }

        public List<string> getSearchInsuraceCompanyName(string prifix)
        {
            return _uploader.getSearchInsuraceCompanyName(prifix);
        }

        public List<string> getSearchTPA(string prifix)
        {
            return _uploader.getSearchTPA(prifix);
        }

        public Task<GMCUploaderModel> updateMaster(List<updatecolumnName> model, string typeofData, string tablename)
        {
            return _uploader.updateMaster(model,typeofData,tablename);
        }

        public async Task<GMCUploaderModel> uploadData(GMCUploaderModel model)
        {
            return await _uploader.uploadData(model);
        }
    }
}
