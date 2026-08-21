using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Net;
using GMC.Helper;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.Controllers.GMC
{
    [RoleAuth("Calculator", "Underwriter", "Admin")]
    public class GMCCalculatorDetailsController : Controller
    {
        readonly IGMCCalculatorDetails _cal;
        static DataTable dtOther = new DataTable();
        static DataTable DT = new DataTable();
        readonly IWebHostEnvironment _hosting;
        public GMCCalculatorDetailsController(IGMCCalculatorDetails cal, IWebHostEnvironment hosting)
        {
            _cal = cal;
            _hosting = hosting;
        }
        public IActionResult Index()
        {
            return View();
        }

        /// <summary> Trend Analysis report (UW year trend, relationship and disease breakups). </summary>
        public async Task<IActionResult> TrendAnalysis(string policyno, string fyyear)
        {
            var model = new TrendAnalysisModel
            {
                PolicyNo = policyno?.Trim(),
                FYYear   = fyyear?.Trim()
            };

            DataSet ds = await _cal.GetTrendAnalysis(model.PolicyNo, model.FYYear);
            if (ds.Tables.Count >= 7)
            {
                model.dtTrend        = ds.Tables[0];
                model.dtParties      = ds.Tables[1];
                model.dtRelationship = ds.Tables[2];
                model.dtDisease      = ds.Tables[3];
                model.dtEnrollLives  = ds.Tables[4];
                model.AvailableYears = ds.Tables[5].Rows.Cast<DataRow>()
                    .Select(r => r["UWYear"]?.ToString() ?? "").Where(y => y != "").ToList();
                model.AvailablePolicies = ds.Tables[6].Rows.Cast<DataRow>()
                    .Select(r => r["PolicyNo"]?.ToString()?.Trim() ?? "").Where(p => p != "").ToList();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTrendAnalysisExcel(string policyno, string fyyear)
        {
            policyno = policyno?.Trim();
            fyyear = fyyear?.Trim();
            if (string.IsNullOrWhiteSpace(policyno) && string.IsNullOrWhiteSpace(fyyear))
                return BadRequest("Select a policy number or a financial year.");

            DataSet ds = await _cal.GetTrendAnalysis(policyno, fyyear);
            if (ds.Tables.Count < 7
                || (ds.Tables[0].Rows.Count == 0 && ds.Tables[4].Rows.Count == 0))
                return NotFound("No Trend Analysis data was found for the selected policy and financial year.");

            if (!string.IsNullOrWhiteSpace(fyyear)
                && !ds.Tables[5].Rows.Cast<DataRow>()
                    .Any(r => string.Equals(r["UWYear"]?.ToString(), fyyear,
                        StringComparison.OrdinalIgnoreCase)))
                return BadRequest("Select a financial year from the Financial Year Master.");

            var policyLabel = string.IsNullOrWhiteSpace(policyno) ? "All Policies" : policyno;
            var bytes = TrendAnalysisExcelExporter.Build(ds, policyLabel, fyyear);
            var safePolicy = string.Concat(policyLabel.Select(ch =>
                Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Replace(' ', '_');
            var yearPart = string.IsNullOrWhiteSpace(fyyear) ? "All_Years" : fyyear;
            var fileName = $"Trend_Analysis_{safePolicy}_{yearPart}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public async Task<IActionResult> getCalclutaorFactor([FromForm] string Sdata, [FromForm] string finalEnrollMentPrem,
            [FromForm] string ClaimCostData
            , [FromForm] IFormFile myfileName, [FromForm] IFormFile fileName1, [FromForm] string policyno, [FromForm] string AverageLives
            , [FromForm] string ClosingLives, [FromForm] string InceptionPremiumPerlife, [FromForm] string lossRatio, [FromForm] string RcareEnrollment)
        {
            //var jsonData = JsonConvert.SerializeObject(Sdata);
            var FData = JsonConvert.DeserializeObject<List<string[]>>(Sdata);
            string ProfitableLR = "";
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            List<factorData> responce = new List<factorData>();
            if (myfileName!=null||fileName1!=null)
            {
                model = await _cal.uploadFile(myfileName, fileName1, policyno);
            }
            foreach (var item in FData)
            {
                if (item[0] == "Maternity LSCS")
                {
                    var ExpiringLimit = item[5] == null ? "" : item[5];
                    var ProposedLimit = item[6] == null ? "" : item[6];

                    if (ExpiringLimit != "" && ProposedLimit != "")
                    {
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        DataSet ds = new DataSet();
                        ds = await _cal.UpdateGridValue("LSCS", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        if (Convert.ToDecimal(ExpiringLimit) > Convert.ToDecimal(ProposedLimit))
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = dt.Rows[0]["Rate"].ToString() + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }

                }
                else if (item[0] == "Maternity Normal Delivery")
                {
                    var ExpiringLimit = item[5] == null ? "" : item[5];
                    var ProposedLimit = item[6] == null ? "" : item[6];

                    if (ExpiringLimit != "" && ProposedLimit != "")
                    {
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        DataSet ds = new DataSet();
                        ds = await _cal.UpdateGridValue("Normal", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];

                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });


                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }

                }
                else if (item[0] == "Cataract Sublimit Change")
                {
                    var ExpiringLimit = item[5] == null ? "" : item[5];
                    var ProposedLimit = item[6] == null ? "" : item[6];

                    if (ExpiringLimit != "" && ProposedLimit != "")
                    {
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        DataSet ds = new DataSet();
                        ds = await _cal.UpdateGridValue("Cataract", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];

                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Change in SI")
                {
                    var ExpiringLimit = item[5] == null ? "" : item[5];
                    var ProposedLimit = item[6] == null ? "" : item[6];

                    if (myfileName != null)
                    {
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        DataSet ds = new DataSet();
                        ds = await _cal.UpdateGridValue_new("Change in SI", model.rolloverSIDT, ClaimCost, EnrollCost, policyno);
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        if (Convert.ToDecimal(dt.Rows[0]["Rate"]) > 0)
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = dt.Rows[0]["Rate"].ToString() + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        model.message = "File uploaded";
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "OPD")
                {
                    var ExpiringLimit = item[5] == null ? "" : item[5];
                    var ProposedLimit = item[6] == null ? "" : item[6];

                    if (ExpiringLimit != "" && ProposedLimit != "")
                    {
                        int intAvgnoofLives = Convert.ToInt16(AverageLives);
                        int intClosingLivies = Convert.ToInt16(ClosingLives);
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        decimal ExpiringNoOfClaim = (intAvgnoofLives * 3 / 100);
                        decimal ProposedNoOfClaim = (intClosingLivies * 3 / 100);
                        decimal ExpiringAverageclaimsize = (Convert.ToDecimal(ExpiringLimit) * 70 / 100);
                        decimal ProposedAverageclaimsize = (Convert.ToDecimal(ProposedLimit) * 70 / 100);

                        decimal expiringOpdpremium = ExpiringNoOfClaim * ExpiringAverageclaimsize;
                        decimal ProposedopdPremium = ProposedNoOfClaim * ProposedAverageclaimsize;

                        var opdLoadingValue = ProposedopdPremium - expiringOpdpremium;
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = opdLoadingValue.ToString(), Loading_Discount_Amount_Enrollment_Premium = opdLoadingValue.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Copay")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Change in Room Rent")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Additional Corporate buffer")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Business Approval")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Profitable business- LR is less than 100")
                {

                    if ((Convert.ToDecimal(lossRatio)) >= 50 && (Convert.ToDecimal(lossRatio)) <= 75)
                    {
                        decimal Lossratio = Convert.ToDecimal(lossRatio);
                        Decimal Thisyear = Convert.ToDecimal(ClaimCostData);
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        BurnCostcal = (Convert.ToDecimal(InceptionPremiumPerlife)) * (Convert.ToDecimal(0.85)) * (Convert.ToDecimal(ClosingLives));

                        // BurnCostcal = (Lossratio) *(Convert.ToDecimal(1 -0.075))* (Convert.ToDecimal(txtClosingLives.Text)) - (Convert.ToDecimal(Session["ClaimCost"]));
                        EnrollCostCal = 0;
                        ProfitableLR = BurnCostcal.ToString();
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = "", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                    else if ((Convert.ToDecimal(lossRatio)) < 50)
                    {
                        decimal Lossratio = Convert.ToDecimal(lossRatio);
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        BurnCostcal = (Convert.ToDecimal(InceptionPremiumPerlife)) * (Convert.ToDecimal(0.75)) * (Convert.ToDecimal(ClosingLives));

                        // BurnCostcal = (Lossratio) *(Convert.ToDecimal(1 -0.075))* (Convert.ToDecimal(txtClosingLives.Text)) - (Convert.ToDecimal(Session["ClaimCost"]));
                        EnrollCostCal = 0;
                        ProfitableLR = BurnCostcal.ToString();
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = "", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }

                }
                else if (item[0] == "Cross Business Impact")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Other Loading / Discounting 1")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    if (LoadingLimit != "" || DiscountLimit != "")
                    {
                        var Loading = item[1] == null ? "" : item[1];
                        var Discount = item[2] == null ? "" : item[2];
                        decimal BurnCostcal = 0;
                        decimal EnrollCostCal = 0;
                        Discount = Discount.Replace("%", "");
                        Loading = Loading.Replace("%", "");
                        var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        if (Discount != "")
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                            EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                }
                else if (item[0] == "Changes in Lives")
                {
                    var LoadingLimit = item[1] == null ? "" : item[1];
                    var DiscountLimit = item[2] == null ? "" : item[2];
                    //string FileName = fileName1.FileName;
                    if (fileName1 != null)
                    {
                        var EnrollCost = Convert.ToDecimal(RcareEnrollment);
                        var ClaimCost = Convert.ToDecimal(ClaimCostData);
                        DataSet ds = new DataSet();
                        ds = await _cal.UpdateGridValue_new_lives("Changes in Lives", model.rolloverDT, ClaimCost, EnrollCost, policyno);
                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2].ToString(), Loading = item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });


                        if (Convert.ToDecimal(dt.Rows[0]["Rate"]) > 0)
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = dt.Rows[0]["Rate"].ToString() + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }

                        model.message = "File uploaded";
                    }
                    else
                    {
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }

                }
                else if (item[0] == "Total of Specific Loading")
                {
                    decimal BurnCostClaimAddition = 0;
                    decimal EndrollCostClaimAddition = 0;
                    foreach (var row in responce)
                    {
                        var BurnCostClaim = row.Loading_Discount_Amount_burn_cost_premium;
                        var EndrollCostClaim = row.Loading_Discount_Amount_Enrollment_Premium;
                        string Factorname_new = row.Factors;
                        if (!(Factorname_new.Equals("Total of Specific Loading") || Factorname_new.Equals("Total premium After Specific Loading") || Factorname_new.Equals("Profitable business- LR is less than 100")))
                        //if (Factorname_new != "Total of Specific Loading" || Factorname_new != "Total premium After Specific Loading")
                        {
                            if (BurnCostClaim != "0")
                            {
                                BurnCostClaimAddition += Convert.ToDecimal(BurnCostClaim);
                            }
                            if (EndrollCostClaim != "0")
                            {
                                EndrollCostClaimAddition += Convert.ToDecimal(EndrollCostClaim);
                            }
                        }
                    }
                    responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = Math.Round(BurnCostClaimAddition, 2).ToString(), Loading_Discount_Amount_Enrollment_Premium = Math.Round(EndrollCostClaimAddition, 2).ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                }
                else if (item[0] == "Total premium After Specific Loading")
                {

                    try
                    {
                        foreach (var row1 in new List<factorData>(responce))
                        {
                            var Factorname_new = row1.Factors;
                            if (Factorname_new == "Total of Specific Loading")
                            {
                                decimal BurnCostClaim = Convert.ToDecimal(row1.Loading_Discount_Amount_burn_cost_premium);
                                decimal EndrollCostClaim = Convert.ToDecimal(row1.Loading_Discount_Amount_Enrollment_Premium);
                                var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                                var ClaimCost = Convert.ToDecimal(ClaimCostData);

                                var LR = Convert.ToDecimal(ProfitableLR == "" ? "0" : ProfitableLR);
                                if (ClaimCost > LR)
                                {
                                    BurnCostClaim = BurnCostClaim + ClaimCost;
                                }
                                else
                                {
                                    BurnCostClaim = BurnCostClaim + LR;
                                }

                                //BurnCostClaim = BurnCostClaim + ClaimCost;
                                EndrollCostClaim = EndrollCostClaim + EnrollCost;

                                //BurnCostClaim =  ClaimCost;
                                //EndrollCostClaim = EnrollCost;
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = Math.Round(BurnCostClaim, 2).ToString(), Loading_Discount_Amount_Enrollment_Premium = Math.Round(EndrollCostClaim, 2).ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                                // ViewBag["Enrollment"] = item[5];
                                //ViewBag["BurnCostClaim"] = item[6];
                                model.Enrollment= Math.Round(EndrollCostClaim, 2);
                                model.BurnCostClaim= Math.Round(BurnCostClaim, 2);

                            }

                        }
                    }
                    catch (Exception ee)
                    {

                        throw;
                    }


                }
            }

            model.factorDataList = responce;
            return Json(JsonConvert.SerializeObject(new { data = model }));
        }

        public async Task<IActionResult> getRenewalCalclutaorFactor([FromForm] string Sdata, [FromForm] string finalEnrollMentPrem,
            [FromForm] string ClaimCostData
            , [FromForm] IFormFile myfileName, [FromForm] IFormFile fileName1, [FromForm] string policyno, [FromForm] string AverageLives
            , [FromForm] string ClosingLives, [FromForm] string InceptionPremiumPerlife, [FromForm] string lossRatio, [FromForm] string RcareEnrollment)
        {
            //var jsonData = JsonConvert.SerializeObject(Sdata);
            var FData = JsonConvert.DeserializeObject<List<string[]>>(Sdata);
            string ProfitableLR = "";
            decimal lives_premium = 0;
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            List<factorData> responce = new List<factorData>();
            if (myfileName != null || fileName1 != null)
            {
                model = await _cal.uploadFile(myfileName, fileName1, policyno);
            }
            try
            {
                foreach (var item in FData)
                {
                    if (item[0] == "Maternity LSCS")
                    {
                        var ExpiringLimit = item[5] == null ? "" : item[5];
                        var ProposedLimit = item[6] == null ? "" : item[6];

                        if (ExpiringLimit != "" && ProposedLimit != "")
                        {
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            DataSet ds = new DataSet();
                            ds = await _cal.UpdateGridValue("LSCS", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                            DataTable dt = new DataTable();
                            dt = ds.Tables[0];
                            if (Convert.ToDecimal(ExpiringLimit) > Convert.ToDecimal(ProposedLimit))
                            {
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = dt.Rows[0]["Rate"].ToString() + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }

                    }
                    else if (item[0] == "Maternity Normal Delivery")
                    {
                        var ExpiringLimit = item[5] == null ? "" : item[5];
                        var ProposedLimit = item[6] == null ? "" : item[6];

                        if (ExpiringLimit != "" && ProposedLimit != "")
                        {
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            DataSet ds = new DataSet();
                            ds = await _cal.UpdateGridValue("Normal", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                            DataTable dt = new DataTable();
                            dt = ds.Tables[0];

                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });


                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }

                    }
                    else if (item[0] == "Cataract Sublimit Change")
                    {
                        var ExpiringLimit = item[5] == null ? "" : item[5];
                        var ProposedLimit = item[6] == null ? "" : item[6];

                        if (ExpiringLimit != "" && ProposedLimit != "")
                        {
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            DataSet ds = new DataSet();
                            ds = await _cal.UpdateGridValue("Cataract", Convert.ToDecimal(ExpiringLimit), Convert.ToDecimal(ProposedLimit), ClaimCost, EnrollCost);
                            DataTable dt = new DataTable();
                            dt = ds.Tables[0];

                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Change in SI")
                    {
                        var ExpiringLimit = item[5] == null ? "" : item[5];
                        var ProposedLimit = item[6] == null ? "" : item[6];

                        if (myfileName != null)
                        {
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            DataSet ds = new DataSet();
                            ds = await _cal.UpdateGridValue_new("Change in SI", model.rolloverSIDT, ClaimCost, EnrollCost, policyno);
                            DataTable dt = new DataTable();
                            dt = ds.Tables[0];
                            if (Convert.ToDecimal(dt.Rows[0]["Rate"]) > 0)
                            {
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = dt.Rows[0]["Rate"].ToString() + "%", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                            }
                            else
                            {
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = dt.Rows[0]["Rate"].ToString() + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["BurnAmtPremium_Cal"].ToString(), Loading_Discount_Amount_Enrollment_Premium = dt.Rows[0]["Enrollmentpremium_Cal"].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                            }
                            model.message = "File uploaded";
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "OPD")
                    {
                        var ExpiringLimit = item[5] == null ? "" : item[5];
                        var ProposedLimit = item[6] == null ? "" : item[6];

                        if (ExpiringLimit != "" && ProposedLimit != "")
                        {
                            int intAvgnoofLives = Convert.ToInt16(AverageLives);
                            int intClosingLivies = Convert.ToInt16(ClosingLives);
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            decimal ExpiringNoOfClaim = (intAvgnoofLives * 3 / 100);
                            decimal ProposedNoOfClaim = (intClosingLivies * 3 / 100);
                            decimal ExpiringAverageclaimsize = (Convert.ToDecimal(ExpiringLimit) * 70 / 100);
                            decimal ProposedAverageclaimsize = (Convert.ToDecimal(ProposedLimit) * 70 / 100);

                            decimal expiringOpdpremium = ExpiringNoOfClaim * ExpiringAverageclaimsize;
                            decimal ProposedopdPremium = ProposedNoOfClaim * ProposedAverageclaimsize;

                            var opdLoadingValue = ProposedopdPremium - expiringOpdpremium;
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = opdLoadingValue.ToString(), Loading_Discount_Amount_Enrollment_Premium = opdLoadingValue.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Copay")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Change in Room Rent")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Additional Corporate buffer")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Business Approval")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Profitable business- LR is less than 100")
                    {

                        if ((Convert.ToDecimal(lossRatio)) >= 50 && (Convert.ToDecimal(lossRatio)) <= 75)
                        {
                            decimal Lossratio = Convert.ToDecimal(lossRatio);
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            BurnCostcal = (Convert.ToDecimal(InceptionPremiumPerlife)) * (Convert.ToDecimal(0.85)) * (Convert.ToDecimal(ClosingLives));

                            // BurnCostcal = (Lossratio) *(Convert.ToDecimal(1 -0.075))* (Convert.ToDecimal(txtClosingLives.Text)) - (Convert.ToDecimal(Session["ClaimCost"]));
                            EnrollCostCal = 0;
                            ProfitableLR = BurnCostcal.ToString();
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = "", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        else if ((Convert.ToDecimal(lossRatio)) < 50)
                        {
                            decimal Lossratio = Convert.ToDecimal(lossRatio);
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            BurnCostcal = (Convert.ToDecimal(InceptionPremiumPerlife)) * (Convert.ToDecimal(0.75)) * (Convert.ToDecimal(ClosingLives));

                            // BurnCostcal = (Lossratio) *(Convert.ToDecimal(1 -0.075))* (Convert.ToDecimal(txtClosingLives.Text)) - (Convert.ToDecimal(Session["ClaimCost"]));
                            EnrollCostCal = 0;
                            ProfitableLR = BurnCostcal.ToString();
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = "", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }

                    }
                    else if (item[0] == "Cross Business Impact")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Other Loading / Discounting 1")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        if (LoadingLimit != "" || DiscountLimit != "")
                        {
                            var Loading = item[1] == null ? "" : item[1];
                            var Discount = item[2] == null ? "" : item[2];
                            decimal BurnCostcal = 0;
                            decimal EnrollCostCal = 0;
                            Discount = Discount.Replace("%", "");
                            Loading = Loading.Replace("%", "");
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            if (Discount != "")
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Discount) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Discount) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = Discount + "%", Loading = "", Loading_Discount_Amount_burn_cost_premium = "-" + BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = "-" + EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                            else
                            {
                                BurnCostcal = Convert.ToDecimal(ClaimCost) * (Convert.ToDecimal(Loading) / 100);
                                EnrollCostCal = Convert.ToDecimal(EnrollCost) * (Convert.ToDecimal(Loading) / 100);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = "", Loading = Loading + "%", Loading_Discount_Amount_burn_cost_premium = BurnCostcal.ToString(), Loading_Discount_Amount_Enrollment_Premium = EnrollCostCal.ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });

                            }
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }
                    }
                    else if (item[0] == "Changes in Lives")
                    {
                        var LoadingLimit = item[1] == null ? "" : item[1];
                        var DiscountLimit = item[2] == null ? "" : item[2];
                        //string FileName = fileName1.FileName;
                        if (fileName1 != null)
                        {
                            var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                            var ClaimCost = Convert.ToDecimal(ClaimCostData);
                            DataSet ds = new DataSet();
                            if ((Convert.ToDecimal(lossRatio)) > 75)
                            {
                                ds = await _cal.UpdateGridValue_new_lives("Changes in Lives", model.rolloverDT, ClaimCost, EnrollCost, policyno);
                                DataTable dt = new DataTable();
                                dt = ds.Tables[0];
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2].ToString(), Loading = item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = dt.Rows[0]["premium"].ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                                lives_premium = Convert.ToDecimal(dt.Rows[0]["premium"].ToString());
                            }
                            else
                            {
                                ds = await _cal.UpdateGridValue_new_lives("Changes in Lives", model.rolloverDT, ClaimCost, EnrollCost, policyno);
                                DataTable dt = new DataTable();
                                dt = ds.Tables[0];
                                var livesCount = dt.Rows[0]["livesCount"].ToString();
                                var EmployeeCount = dt.Rows[0]["EmployeeCount"].ToString();
                                var RevisedLives = (Convert.ToDecimal(InceptionPremiumPerlife)) * Convert.ToDecimal(1 - 0.075) * Convert.ToDecimal(livesCount);
                                responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2].ToString(), Loading = item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = RevisedLives.ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                                lives_premium = Convert.ToDecimal(RevisedLives.ToString());
                            }
                            model.message = "File uploaded";
                        }
                        else
                        {
                            responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = item[3] == null ? "" : item[3].ToString(), Loading_Discount_Amount_Enrollment_Premium = item[4] == null ? "" : item[4].ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                        }

                    }
                    else if (item[0] == "Total of Specific Loading")
                    {
                        decimal BurnCostClaimAddition = 0;
                        decimal EndrollCostClaimAddition = 0;
                        foreach (var row in responce)
                        {
                            var BurnCostClaim = row.Loading_Discount_Amount_burn_cost_premium;
                            var EndrollCostClaim = row.Loading_Discount_Amount_Enrollment_Premium;
                            string Factorname_new = row.Factors;
                            if (!(Factorname_new.Equals("Total of Specific Loading") || Factorname_new.Equals("Total premium After Specific Loading") || Factorname_new.Equals("Profitable business- LR is less than 100")))
                            //if (Factorname_new != "Total of Specific Loading" || Factorname_new != "Total premium After Specific Loading")
                            {
                                if (BurnCostClaim != "0")
                                {
                                    BurnCostClaimAddition += Convert.ToDecimal(BurnCostClaim);
                                }
                                if (EndrollCostClaim != "0")
                                {
                                    EndrollCostClaimAddition += Convert.ToDecimal(EndrollCostClaim);
                                }
                            }
                        }
                        responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = Math.Round(BurnCostClaimAddition, 2).ToString(), Loading_Discount_Amount_Enrollment_Premium = Math.Round(EndrollCostClaimAddition, 2).ToString(), Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                    }
                    else if (item[0] == "Total premium After Specific Loading")
                    {

                        try
                        {
                            foreach (var row1 in new List<factorData>(responce))
                            {
                                var Factorname_new = row1.Factors;
                                if (Factorname_new == "Total of Specific Loading")
                                {
                                    decimal BurnCostClaim = Convert.ToDecimal(row1.Loading_Discount_Amount_burn_cost_premium);
                                    decimal EndrollCostClaim = Convert.ToDecimal(row1.Loading_Discount_Amount_Enrollment_Premium);
                                    var EnrollCost = Convert.ToDecimal(finalEnrollMentPrem);
                                    var ClaimCost = Convert.ToDecimal(ClaimCostData);
                                    var lives_premiumData = Convert.ToDecimal(lives_premium);
                                    var LR = Convert.ToDecimal(ProfitableLR == "" ? "0" : ProfitableLR);
                                    if (lives_premiumData > 0)
                                    {
                                        BurnCostClaim = BurnCostClaim + lives_premiumData;
                                    }
                                    else if (ClaimCost > LR)
                                    {
                                        BurnCostClaim = BurnCostClaim + ClaimCost;
                                    }
                                    else
                                    {
                                        BurnCostClaim = BurnCostClaim + LR;
                                    }

                                    EndrollCostClaim = EndrollCostClaim + EnrollCost;

                                    //BurnCostClaim =  ClaimCost;
                                    //EndrollCostClaim = EnrollCost;
                                    responce.Add(new factorData { Factors = item[0].ToString(), Discount = item[2] == null ? "" : item[2].ToString(), Loading = item[1] == null ? "" : item[1].ToString(), Loading_Discount_Amount_burn_cost_premium = Math.Round(BurnCostClaim, 2).ToString(), Loading_Discount_Amount_Enrollment_Premium = "0", Expiring_Limit = item[5] == null ? "" : item[5].ToString(), Proposed_Limit = item[6] == null ? "" : item[6].ToString() });
                                    // ViewBag["Enrollment"] = item[5];
                                    //ViewBag["BurnCostClaim"] = item[6];

                                }

                            }
                        }
                        catch (Exception ee)
                        {

                            throw;
                        }


                    }
                }
            }
            catch (Exception ee)
            {

                throw;
            }
            

            model.factorDataList = responce;
            return Json(JsonConvert.SerializeObject(new { data = model }));
        }
        public IActionResult getStanderdData(List<standerdData> Sdata, string enrolment, string BurnCostClaim, string finalEnrollMentPrem, string ClosingLives)
        {
            List<StanderdLoadingData> model = new List<StanderdLoadingData>();
            decimal GIPSA = 0, Inflation_Cliam = 0, Inflation_enrollment = 0, Management_claim = 0, Management_enrollment = 0
                , Profit_claim = 0, Profit_enrollment = 0, WithTPS_claim = 0, WithTPA_enrollment = 0, GST_claim = 0, GST_enrollment = 0
                , Final_Quote_Claim = 0, Final_Quote_enrollment = 0;
            foreach (var item in Sdata)
            {
                if (item.tableData[0] == "GIPSA Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GIPSA Loading", LoadingPer = txtloading, enrollmentpremium = "0", BurnpremiumLoading = Math.Round((ClaimCost * Convert.ToDecimal(inputLoading))).ToString() });
                        GIPSA = Convert.ToDecimal(Math.Round((ClaimCost * Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Inflation Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Inflation Loading", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + 0) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString() });
                        Inflation_Cliam = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString());
                        Inflation_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + 0) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Management Cost Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Management Cost Loading", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString() });
                        Management_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString());
                        Management_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Profit")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Profit", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        Profit_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        Profit_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "With TPA Fees")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "With TPA Fees", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment) / (1 - (Convert.ToDecimal(inputLoading))) * Convert.ToDecimal(inputLoading)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString() });
                        WithTPS_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString());
                        WithTPA_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment) / (1 - (Convert.ToDecimal(inputLoading))) * Convert.ToDecimal(inputLoading)).ToString());
                    }
                }
                else if (item.tableData[0] == "GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        GST_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        GST_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Final Quote with GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote with GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment + GST_enrollment)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment + GST_enrollment)).ToString());
                    }
                }
                else if (item.tableData[0] == "Final Quote without GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote without GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment)).ToString());
                    }
                }
                else if (item.tableData[0] == "Per Life Premium")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(finalEnrollMentPrem) > 0 ? Convert.ToDecimal(finalEnrollMentPrem) : Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Per Life Premium", LoadingPer = txtloading, enrollmentpremium = Math.Round(((Final_Quote_enrollment) / Convert.ToInt32(ClosingLives))).ToString(), BurnpremiumLoading = Math.Round(((Final_Quote_Claim) / Convert.ToInt32(ClosingLives))).ToString() });
                    }
                }

            }

            return Json(JsonConvert.SerializeObject(new { data = model }));
        }
        public IActionResult getStanderdDataWithEnrollment(List<standerdData> Sdata, string enrolment, string BurnCostClaim, string finalEnrollMentPrem, string ClosingLives)
        {
            List<StanderdLoadingData> model = new List<StanderdLoadingData>();
            decimal GIPSA = 0, Inflation_Cliam = 0, Inflation_enrollment = 0, Management_claim = 0, Management_enrollment = 0
                , Profit_claim = 0, Profit_enrollment = 0, WithTPS_claim = 0, WithTPA_enrollment = 0, GST_claim = 0, GST_enrollment = 0
                , Final_Quote_Claim = 0, Final_Quote_enrollment = 0;
            foreach (var item in Sdata)
            {
                if (item.tableData[0] == "GIPSA Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GIPSA Loading", LoadingPer = txtloading, enrollmentpremium = "0", BurnpremiumLoading = Math.Round((ClaimCost * Convert.ToDecimal(inputLoading))).ToString() });
                        GIPSA = Convert.ToDecimal(Math.Round((ClaimCost * Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Inflation Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Inflation Loading", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + 0) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString() });
                        Inflation_Cliam = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString());
                        Inflation_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + 0) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Management Cost Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Management Cost Loading", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString() });
                        Management_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString());
                        Management_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Profit")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Profit", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        Profit_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        Profit_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "With TPA Fees")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "With TPA Fees", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment) / (1 - (Convert.ToDecimal(inputLoading))) * Convert.ToDecimal(inputLoading)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString() });
                        WithTPS_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString());
                        WithTPA_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment) / (1 - (Convert.ToDecimal(inputLoading))) * Convert.ToDecimal(inputLoading)).ToString());
                    }
                }
                else if (item.tableData[0] == "GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment) * (Convert.ToDecimal(inputLoading))).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        GST_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        GST_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment) * (Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Final Quote with GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote with GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment + GST_enrollment)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment + GST_enrollment)).ToString());
                    }
                }
                else if (item.tableData[0] == "Final Quote without GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote without GST", LoadingPer = txtloading, enrollmentpremium = Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment)).ToString(), BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal(Math.Round((EnrollCost + Inflation_enrollment + Management_enrollment + Profit_enrollment + WithTPA_enrollment)).ToString());
                    }
                }
                else if (item.tableData[0] == "Per Life Premium")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Per Life Premium", LoadingPer = txtloading, enrollmentpremium = Math.Round(((Final_Quote_enrollment) / Convert.ToInt32(ClosingLives))).ToString(), BurnpremiumLoading = Math.Round(((Final_Quote_Claim) / Convert.ToInt32(ClosingLives))).ToString() });
                    }
                }

            }

            return Json(JsonConvert.SerializeObject(new { data = model }));
        }

        public IActionResult getRenewalStanderdData(List<standerdData> Sdata, string enrolment, string BurnCostClaim, string finalEnrollMentPrem, string ClosingLives)
        {
            List<StanderdLoadingData> model = new List<StanderdLoadingData>();
            decimal GIPSA = 0, Inflation_Cliam = 0, Inflation_enrollment = 0, Management_claim = 0, Management_enrollment = 0
                , Profit_claim = 0, Profit_enrollment = 0, WithTPS_claim = 0, WithTPA_enrollment = 0, GST_claim = 0, GST_enrollment = 0
                , Final_Quote_Claim = 0, Final_Quote_enrollment = 0;
            foreach (var item in Sdata)
            {
                if (item.tableData[0] == "GIPSA Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GIPSA Loading", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = "0" });
                        GIPSA = Convert.ToDecimal(Math.Round((ClaimCost * Convert.ToDecimal(inputLoading))).ToString());
                    }
                }
                else if (item.tableData[0] == "Inflation Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (inputLoading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);
                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Inflation Loading", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString() });
                        Inflation_Cliam = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost) * Convert.ToDecimal(inputLoading))).ToString());
                        Inflation_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "Management Cost Loading")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (inputLoading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Management Cost Loading", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString() });
                        Management_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam) * Convert.ToDecimal(inputLoading))).ToString());
                        Management_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "Profit")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (inputLoading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Profit", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        Profit_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        Profit_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "With TPA Fees")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "With TPA Fees", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString() });
                        WithTPS_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim) / (1 - Convert.ToDecimal(inputLoading)) * Convert.ToDecimal(inputLoading))).ToString());
                        WithTPA_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "GST", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString() });
                        GST_claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim) * Convert.ToDecimal(inputLoading))).ToString());
                        GST_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "Final Quote with GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote with GST", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim + GST_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "Final Quote without GST")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Final Quote without GST", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString() });
                        Final_Quote_Claim = Convert.ToDecimal(Math.Round(((GIPSA + ClaimCost + Inflation_Cliam + Management_claim + Profit_claim + WithTPS_claim))).ToString());
                        Final_Quote_enrollment = Convert.ToDecimal("0");
                    }
                }
                else if (item.tableData[0] == "Per Life Premium")
                {
                    var txtloading = item.inputValue;
                    var inputLoading = item.inputValue;
                    if (txtloading != "")
                    {
                        decimal EnrollCost = Convert.ToDecimal(enrolment);
                        decimal ClaimCost = Convert.ToDecimal(BurnCostClaim);

                        inputLoading = (Convert.ToDecimal(inputLoading) / 100).ToString();
                        model.Add(new StanderdLoadingData { Loading_Factor = "Per Life Premium", LoadingPer = txtloading.ToString(), enrollmentpremium = "0", BurnpremiumLoading = Convert.ToInt32(ClosingLives)==0?"0":Math.Round(((Final_Quote_Claim) / Convert.ToInt32(ClosingLives))).ToString() });
                    }
                }

            }

            return Json(JsonConvert.SerializeObject(new { data = model }));
        }

        public async Task<IActionResult> getBindVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            DataSet ds = await _cal.BindVersionDetailsToControls(Policyno, VersionNumber);
            if (ds != null)
            {
                DataTable dt = ds.Tables[0];
                model.QuoteNumber = dt.Rows[0]["Quote_number"].ToString();
                model.IceptionPremium = Convert.ToDecimal(dt.Rows[0]["inception_premium"].ToString());
                model.FinalYearPremium = Convert.ToInt32(dt.Rows[0]["FinalYearPremium"].ToString());
                model.OpeningLives = Convert.ToInt32(dt.Rows[0]["OpeningLives"].ToString());
                model.ClosingLives = Convert.ToInt32(dt.Rows[0]["ClosingLives"].ToString());
                model.AvgLives = Convert.ToInt32(dt.Rows[0]["AvgLives"].ToString());
                model.OpeningEmployee = Convert.ToInt32(dt.Rows[0]["OpeningEmp"].ToString());
                model.ClosingEmployee = Convert.ToInt32(dt.Rows[0]["ClosingEmp"].ToString());
                model.AvgEmployee = Convert.ToInt32(dt.Rows[0]["AverageEmp"].ToString());
                model.PolicyServiceDays = Convert.ToInt32(dt.Rows[0]["PolicyServiceDay"].ToString());
                model.LSCSSublimit = Convert.ToInt32(ds.Tables[2].Rows[0]["LSCSSubLimit"].ToString());
                model.lscsLimitResult = ds.Tables[2].Rows[0]["LSCSSubLimit_result"].ToString();
                model.NormalSublimit = Convert.ToInt32(ds.Tables[2].Rows[0]["NormalSubLimit"].ToString());
                model.NormalLimitResult = ds.Tables[2].Rows[0]["NormalSubLimit_result"].ToString();
                model.FinalEnrollmentpremium = ds.Tables[2].Rows[0]["Final_Enrollment_premium"].ToString();
                model.InceptionPremiumperlife = Convert.ToInt32(dt.Rows[0]["InceptionPremiumPerlife"].ToString());
                model.ClosingPremiumperlife = Convert.ToInt32(dt.Rows[0]["ClosingPremiumPerlife"].ToString());
                model.dtBurn = ds.Tables[1];
                model.dtloadingFactor = ds.Tables[3];
                model.dtStanderedLoading = ds.Tables[4];
            }

            var data = model;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }
        public async Task<IActionResult> SaveData(GMCCalculatorDetailsModel model)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            var result = await _cal.InsertGMCRollover(model);
            responce.version = result.version;
           
            var data = responce;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }
        public async Task<IActionResult> SaveBurnData(List<string[]> BData,string versionNo)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            var result = await _cal.SaveBurnCostDetails(BData,versionNo);
            
            var data = result;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }
        public async Task<IActionResult> SaveFactorData(List<string[]> FData, string versionNo)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            var result = await _cal.SaveLoadFactorDetails(FData,versionNo);
            
            var data = result;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }
        public async Task<IActionResult> SaveSTData(List<string[]> STData, string versionNo)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            var result = await _cal.SaveLoadStanderdDetails(STData,versionNo);
            var data = result;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }
        public async Task<IActionResult> getBindMaternityCost(GMCCalculatorDetailsModel model)
        {


            if (model.NormalSublimit > 0 || model.LSCSSublimit > 0)
            {
                DataTable dt = await _cal.BindMaternityCost(model);

                var LSCSLimit = ((Convert.ToDecimal(dt.Rows[0]["LSCSRate"])) * (Convert.ToDecimal(model.Enrollment)));
                var NormalLimit = ((Convert.ToDecimal(dt.Rows[0]["NormalRate"])) * (Convert.ToDecimal(model.Enrollment)));
                model.NormalLimitResult = Math.Round(NormalLimit).ToString();
                model.lscsLimitResult = Math.Round(LSCSLimit).ToString();
                var FinalEnrollMent = NormalLimit + LSCSLimit + (Convert.ToDecimal(model.Enrollment));
                model.FinalEnrollmentpremium = Math.Round(FinalEnrollMent).ToString();
            }
            else
            {
                model.NormalLimitResult = "";
                model.lscsLimitResult = "";
                var FinalEnrollMent = (Convert.ToDecimal(model.Enrollment));
                model.FinalEnrollmentpremium = FinalEnrollMent.ToString();
            }
            model.OtherLoadingFactor = dtOther;
            var data = model;
            return Json(JsonConvert.SerializeObject(new { data = data }));
        }

        public async Task<IActionResult> getBurnCostDetailsData(GMCCalculatorDetailsModel model)
        {
            try
            {
            var JsonString = JsonConvert.DeserializeObject<List<string[]>>(model.Sdata);
                if (JsonString.Count>0)
                {
                    foreach (var item in JsonString)
                    {
                        if (item[0].ToString()== "Star Rejection")
                        {
                            if (item[3].ToString()!="")
                            {
                                model.startRejection= Convert.ToDecimal(item[3].ToString());
                            }
                        }
                        if (item[0].ToString() == "IBNR")
                        {
                            if (item[3].ToString() != "")
                            {
                                model.IBNR = Convert.ToDecimal(item[3].ToString());
                            }
                        }
                    }
                }
            DataSet ds = await _cal.BindBurnCost(model);

            //model.GMCPendingDetailsList = new List<GMCPendingDetails>();
            //model.GMCPendingDetailsList = DataTableToList.ConvertDataTableToListForCommon<GMCPendingDetails>(dt);
            model.dtBurn = ds.Tables[0];
            model.dtloading = ds.Tables[1];
            model.dtloadingFactor = ds.Tables[2];
            model.dt = ds.Tables[3];
            model.dt1 = ds.Tables[5];
            model.OtherLoadingFactor = ds.Tables[4];
            dtOther = ds.Tables[4];
            model.PolicyServiceDays = ds.Tables[3].Rows[0]["totalReconDay"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["totalReconDay"].ToString());
            model.AvgLives = ds.Tables[3].Rows[0]["Avelives"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["Avelives"].ToString());
            model.AvgEmployee = ds.Tables[3].Rows[0]["Avg_Employee"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["Avg_Employee"].ToString());
            model.InceptionPremiumperlife = ds.Tables[3].Rows[0]["Inception Premium per life"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["Inception Premium per life"].ToString());
            model.ClosingPremiumperlife = ds.Tables[3].Rows[0]["closing premium per life"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["closing premium per life"].ToString());
            model.ClaimCost = ds.Tables[3].Rows[0]["BurnCost"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["BurnCost"].ToString());
            model.Enrollment = ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString());
            model.BurnCostClaim = ds.Tables[5].Rows[0]["TotalAmount"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[5].Rows[0]["TotalAmount"].ToString());
            model.RcareEnrollment = ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString());
            model.LossRatio = ds.Tables[3].Rows[0]["LossRatio"].ToString() == "" ? 0 : Convert.ToDecimal(ds.Tables[3].Rows[0]["LossRatio"].ToString());
            var data = model;

            //var result = JsonConvert.SerializeObject(new { data = data });



            return Json(JsonConvert.SerializeObject(new { data = data }));

            }
            catch (Exception)
            {

                throw;
            }

        }
        public async Task<IActionResult> getGMCPremuiumDetails(string types)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();

            DataTable dt = await _cal.PolicyPendingDetails(types);
            model.GMCPendingDetailsList = DataTableToList.ConvertDataTableToListForCommon<GMCPendingDetails>(dt);
            var data = model.GMCPendingDetailsList;
            var result = JsonConvert.SerializeObject(new { data = dt });



            return Content(result);
        }
      
        public async Task<IActionResult> GMCCalculatorpremium(string policyno, int? uploadId = null)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            DataTable dt = await _cal.GetGMCPolicyLevelData(policyno);
            DataTable dtLiv = await _cal.GetGMCRolloverLiveData(policyno);
            int closingLives = dtLiv.Rows.Count > 0 ? Convert.ToInt32(dtLiv.Rows[0]["ClosingLives"]) : 0;
            int closingEmp   = dtLiv.Rows.Count > 0 ? Convert.ToInt32(dtLiv.Rows[0]["ClosingEmp"]) : 0;
            if (dt.Rows.Count>0)
            {
                model.PolicyStartDate = dt.Rows[0]["PolicyStartDate"].ToString() == "" ? DateTime.Now.Date : validateDate(dt.Rows[0]["PolicyStartDate"].ToString());
                model.Policy_End_date = dt.Rows[0]["Policy_End_date"].ToString() == "" ? DateTime.Now.Date : validateDate(dt.Rows[0]["Policy_End_date"].ToString());
                model.ReconDate = validateDate(dt.Rows[0]["ReconDate"].ToString());
                model.PolicyNo = dt.Rows[0]["PolicyNo"].ToString();
                model.dtVersion = await _cal.BindVersionDetails(policyno);
                model.versionDatalist = DataTableToList.ConvertDataTableToListForCommon<versionData>(model.dtVersion);
                model.ClosingLives = closingLives;
                model.ClosingEmployee = closingEmp;
                return View(model);
            }
            model.PolicyStartDate = DateTime.Now.Date;
            model.Policy_End_date = DateTime.Now.Date;
            model.ReconDate = DateTime.Now.Date;
            model.PolicyNo = policyno;
            model.dtVersion = await _cal.BindVersionDetails(policyno);
            model.versionDatalist = DataTableToList.ConvertDataTableToListForCommon<versionData>(model.dtVersion);
            model.ClosingLives = closingLives;
            model.ClosingEmployee = closingEmp;
            return View(model);

        }
        public async Task<IActionResult> getRenewalBurnCostDetailsData(GMCCalculatorDetailsModel model)
        {
            try
            {
                var JsonString = JsonConvert.DeserializeObject<List<string[]>>(model.Sdata);
                if (JsonString.Count > 0)
                {
                    foreach (var item in JsonString)
                    {
                        if (item[0].ToString() == "Star Rejection")
                        {
                            if (item[3].ToString() != "")
                            {
                                model.startRejection = Convert.ToDecimal(item[3].ToString());
                            }
                        }
                        if (item[0].ToString() == "IBNR")
                        {
                            if (item[3].ToString() != "")
                            {
                                model.IBNR = Convert.ToDecimal(item[3].ToString());
                            }
                        }
                    }
                }
                DataSet ds = await _cal.BindBurnCostForRenewal(model);

                //model.GMCPendingDetailsList = new List<GMCPendingDetails>();
                //model.GMCPendingDetailsList = DataTableToList.ConvertDataTableToListForCommon<GMCPendingDetails>(dt);
                model.dtBurn = ds.Tables[0];
                model.dtloading = ds.Tables[1];
                model.dtloadingFactor = ds.Tables[2];
                model.dt = ds.Tables[3];
                model.dt1 = ds.Tables[5];
                model.OtherLoadingFactor = ds.Tables[4];
                dtOther = ds.Tables[4];
                //model.PolicyStartDate = validateDate(ds.Tables[6].Rows[0]["PolicyStartDate"].ToString());
                //model.Policy_End_date = validateDate(ds.Tables[6].Rows[0]["Policy_End_date"].ToString());
                //model.ReconDate = validateDate(ds.Tables[6].Rows[0]["ReconDate"].ToString());
                //model.PolicyNo = ds.Tables[6].Rows[0]["PolicyNo"].ToString();
               
                
               
                var data = model;

                var result = JsonConvert.SerializeObject(new { data = model });



                return Json(result);

            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<IActionResult> GMC_Renewal_CalculatorPremium(string policyno)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            model.PolicyNo = policyno;
            DataSet ds = await _cal.BindBurnCostForRenewal(model);
            model.PolicyStartDate = validateDate(ds.Tables[6].Rows[0]["PolicyStartDate"].ToString());
            model.Policy_End_date = validateDate(ds.Tables[6].Rows[0]["Policy_End_date"].ToString());
            model.ReconDate = validateDate(ds.Tables[6].Rows[0]["ReconDate"].ToString());
            model.PolicyNo = ds.Tables[6].Rows[0]["PolicyNo"].ToString();
            model.dtVersion = await _cal.BindVersionDetails(policyno);
            model.IceptionPremium =Convert.ToDecimal(ds.Tables[3].Rows[0]["InceptionPremium"].ToString());
            model.FinalYearPremium = Convert.ToInt32(ds.Tables[3].Rows[0]["FinalPremium"].ToString());
            model.OpeningLives= Convert.ToInt32(ds.Tables[3].Rows[0]["OpngLives"].ToString());
            model.ClosingLives= Convert.ToInt32(ds.Tables[3].Rows[0]["ClsgLives"].ToString());
            model.ClosingEmployee= Convert.ToInt32(ds.Tables[3].Rows[0]["Closing_Employee"].ToString());
            model.OpeningEmployee= Convert.ToInt32(ds.Tables[3].Rows[0]["Opening_Employee"].ToString());
            model.versionDatalist = DataTableToList.ConvertDataTableToListForCommon<versionData>(model.dtVersion);
            model.Claim_costPerLife = ds.Tables[6].Rows[0]["Claim_costPerLife"].ToString();
            model.Claim_CostPerEmployee = ds.Tables[6].Rows[0]["Claim_CostPerEmployee"].ToString();
            model.PolicyServiceDays = ds.Tables[3].Rows[0]["totalReconDay"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["totalReconDay"].ToString());
            model.AvgLives = ds.Tables[3].Rows[0]["Avelives"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["Avelives"].ToString());
            model.AvgEmployee = ds.Tables[3].Rows[0]["Avg_Employee"].ToString() == "" ? 0 : Convert.ToInt32(ds.Tables[3].Rows[0]["Avg_Employee"].ToString());
            model.InceptionPremiumperlife = ds.Tables[3].Rows[0]["Inception Premium per life"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[3].Rows[0]["Inception Premium per life"].ToString());
            model.ClosingPremiumperlife = ds.Tables[3].Rows[0]["closing premium per life"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[3].Rows[0]["closing premium per life"].ToString());
            model.ClaimCost = ds.Tables[3].Rows[0]["BurnCost"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[3].Rows[0]["BurnCost"].ToString());
            model.Enrollment = ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString());
            model.BurnCostClaim = ds.Tables[5].Rows[0]["TotalAmount"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[5].Rows[0]["TotalAmount"].ToString());
            model.RcareEnrollment = ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString() == "" ? 0 : Convert.ToInt64(ds.Tables[3].Rows[0]["EndrollmentPremium"].ToString());
            model.LossRatio = ds.Tables[3].Rows[0]["LossRatio"].ToString() == "" ? 0 : Convert.ToDecimal(ds.Tables[3].Rows[0]["LossRatio"].ToString());
            return View(model);
        }
        public FileResult DownloadDataFile(string filename)
        {
            string path = filename;
            string excelfilename = Path.GetFileName(filename);
            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            System.IO.File.Delete(path);
            return File(bytes, "application/octet-stream", excelfilename);
        }
        public async Task<IActionResult> downloadVersionDetailsData(string Policyno, string VersionNumber)
        {
            try
            {
                var response = await _cal.DownloadVersionDetailsToControls(Policyno,VersionNumber);
                if (response.excelfileName == null)
                {

                    response.error = "No record found!!!!";
                    return Json(response);
                }

                return Json(response);
            }
            catch (Exception ee)
            {

                throw ee;
            }


        }
        public async Task<IActionResult> downloadRenewalVersionDetailsData(string Policyno, string VersionNumber)
        {
            try
            {
                var response = await _cal.DownloadRenewalVersionDetailsToControls(Policyno, VersionNumber);
                if (response.excelfileName == null)
                {

                    response.error = "No record found!!!!";
                    return Json(response);
                }

                return Json(response);
            }
            catch (Exception ee)
            {

                throw ee;
            }


        }
        public async Task<IActionResult> downloadRenewalSummeryVersionDetailsData(string Policyno, string VersionNumber)
        {
            try
            {
                var response = await _cal.DownloadSummeryVersionDetailsToControls(Policyno, VersionNumber);
                if (response.excelfileName == null)
                {

                    response.error = "No record found!!!!";
                    return Json(response);
                }

                return Json(response);
            }
            catch (Exception ee)
            {

                throw ee;
            }


        }
        public async Task<IActionResult> downloadVersionSummeryDetailsData(string Policyno,string version)
        {
            try
            {
                var response = await _cal.DownloadSummeryVersionDetailsToControls(Policyno, version);
                if (string.IsNullOrWhiteSpace(response.excelfileName))
                    response.error = "No summary data found for this policy/version.";

                return Json(response);
            }
            catch
            {
                throw;
            }


        }
        public DateTime validateDate(string date)
        {
            string dateString = date;
            string format = "dd/MM/yyyy"; // Specify the expected date format
            DateTime parsedDate;
            if (DateTime.TryParseExact(dateString, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                return parsedDate;
            }
            return parsedDate;
        }
    }
}
