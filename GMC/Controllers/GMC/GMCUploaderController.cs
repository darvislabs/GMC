using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.Controllers.GMC
{
    //[Authorize]
    
    public class GMCUploaderController : Controller
    {
        readonly IGMCUploader _uploader;
        public GMCUploaderController(IGMCUploader uploader)
        {
            _uploader = uploader;
        }
        public IActionResult Index()
        {
            GMCUploaderModel model = new GMCUploaderModel();
            model = _uploader.getIndustryName();
            return View(model);
        }
        [HttpPost]
        [RequestFormLimits(ValueCountLimit = int.MaxValue)]
        public async Task<IActionResult> Index(GMCUploaderModel model)
        {
            GMCUploaderModel responce = new GMCUploaderModel();
            try
            {
                if (model.bussinessType == "0" || model.bussinessType == "1")
                {
                    if (Path.GetExtension(model.myFile.FileName).ToUpper() == ".XLSX")
                    {
                        responce = await _uploader.uploadData(model);
                        var result2 = _uploader.getIndustryName();
                        responce.industryNameList = result2.industryNameList;
                        //if (responce.errorDT!=null)
                        //{
                        //    var data = JsonConvert.SerializeObject(new { data = responce.errorDT,list=responce.columnList });
                        //    return Content(data);
                        //}
                        return Json(JsonConvert.SerializeObject(new { data = responce }));
                    }
                    responce.message = "File Extension Allow only .xlsx file";
                    return Json(JsonConvert.SerializeObject(new { data = responce }));

                }
                if (model.bussinessType == "2")
                {
                    if (model.unit == "1")
                    {
                        if (Path.GetExtension(model.myFile.FileName).ToUpper() == ".XLSX")
                        {
                            responce = await _uploader.uploadData(model);
                            var result1 = _uploader.getIndustryName();
                            responce.industryNameList = result1.industryNameList;
                            //if (responce.errorDT!=null)
                            //{
                            //    var data = JsonConvert.SerializeObject(new { data = responce.errorDT,list=responce.columnList });
                            //    return Content(data);
                            //}
                            return Json(JsonConvert.SerializeObject(new { data = responce }));
                        }
                        responce.message = "File Extension Allow only .xlsx file";
                        return Json(JsonConvert.SerializeObject(new { data = responce }));
                    }
                }
                responce = await _uploader.uploadData(model);
                var result = _uploader.getIndustryName();
                responce.industryNameList = result.industryNameList;
                //if (responce.errorDT!=null)
                //{
                //    var data = JsonConvert.SerializeObject(new { data = responce.errorDT,list=responce.columnList });
                //    return Content(data);
                //}
                return Json(JsonConvert.SerializeObject(new { data = responce }));

            }
            catch (Exception ee)
            {

                throw;
            }


        }
        public IActionResult SearchInsuraceCompanyName(string prefixText)
        {
            try
            {

                var company_name = _uploader.getSearchInsuraceCompanyName(prefixText);

                return Json(company_name, new System.Text.Json.JsonSerializerOptions());
            }
            catch
            {
                throw;
            }
        }
        public async Task<IActionResult> UpdateMasterTable(List<updatecolumnName> mdata, string typeofData, string tnames)
        {
            var responce = await _uploader.updateMaster(mdata, typeofData, tnames);


            return Json(responce.message);
        }
        public IActionResult SearchTPA(string prefixText)
        {
            try
            {

                var TPA_Name = _uploader.getSearchTPA(prefixText);

                return Json(TPA_Name, new System.Text.Json.JsonSerializerOptions());
            }
            catch
            {
                throw;
            }
        }
    }
}
