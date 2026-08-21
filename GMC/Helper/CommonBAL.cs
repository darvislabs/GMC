using NuGet.Packaging;
using OfficeOpenXml;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace GMC.Helper
{
    /*
     Modified By:Sanjay Goyal
     
     */
    public class CommonBAL
    {
        
        public void EPPlusExportOne(DataSet ds, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial; //added by pratiksha for error Please set the ExcelPackage.LicenseContext property.
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);

                // Add dropdown list to a specific column (e.g., column 2)
                int dropdownColumnIndex = 22; // Specify the column index where you want the dropdown
                int dropdownStartRowIndex = 2; // The row index where the dropdown starts (skip the header row)

                // Define the list of options for the dropdown
                string[] dropdownOptions = { "Approved", "Reject" };

                // Create the data validation for the dropdown list
                var validation = workSheet.DataValidations.AddListValidation(workSheet.Cells[dropdownStartRowIndex, dropdownColumnIndex, dropdownStartRowIndex + dt.Rows.Count - 1, dropdownColumnIndex].Address);

                // Add the dropdown options
                validation.Formula.Values.AddRange(dropdownOptions);

                // Optional: Set error message if invalid value is entered
                validation.ShowErrorMessage = true;
                validation.ErrorTitle = "Invalid Value";
                validation.Error = "Please select a value from the dropdown list.";

                //// Optional: Set input message for the dropdown
                //validation.ShowInputMessage = true;
                //validation.InputTitle = "Select an Option";
                //validation.InputMessage = "Please select a value from the dropdown list.";





                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportExcel(DataTable dt, string TempFile)

        {
            
            var random = new Random();

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage())

            {
                
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet");

                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);
                //excelPackage.Workbook.Protection.SetPassword("12345");
                excelPackage.SaveAs(new FileInfo(TempFile));

            }


        }
        public void EPPlusExportCol(DataSet ds, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];
            DataTable dt1 = ds.Tables[1];
            DataTable dt2 = ds.Tables[2];


            using (ExcelPackage excelPackage = new ExcelPackage())
            {


                if (dt.Rows.Count > 0)
                {
                    var workSheet = excelPackage.Workbook.Worksheets.Add("Matched data");
                    workSheet.Cells[1, 1].LoadFromDataTable(dt, true);

                }


                if (dt1.Rows.Count > 0)
                {
                    var workSheet1 = excelPackage.Workbook.Worksheets.Add("Partially Match data");
                    workSheet1.Cells[1, 1].LoadFromDataTable(dt1, true);


                }


                if (dt2.Rows.Count > 0)
                {
                    var workSheet2 = excelPackage.Workbook.Worksheets.Add("Mismatched data");
                    workSheet2.Cells[1, 1].LoadFromDataTable(dt2, true);


                }

                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportExcelForRenewal(DataTable dt, string TempFile)

        {

            var random = new Random();

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage())

            {

                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet");

                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);
                //excelPackage.Workbook.Protection.SetPassword("12345");
                excelPackage.SaveAs(new FileInfo(TempFile));

            }


        }

        public void EPPlusExportExcelWithOutMasking(DataTable dt, string TempFile)

        {

            var random = new Random();

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage())

            {

                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet");

                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);
                //excelPackage.Workbook.Protection.SetPassword("12345");
                excelPackage.SaveAs(new FileInfo(TempFile));

            }


        }

        public void EPPlusExport1(DataSet ds, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];
            DataTable dt1 = ds.Tables[1];
            DataTable dt2 = ds.Tables[2];
            DataTable dt3 = ds.Tables[3];
            DataTable dt4 = ds.Tables[4];
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");
                var Dignosis = excelPackage.Workbook.Worksheets.Add("Dignosis Summary ");

                if (dt4.Rows.Count > 0)
                {


                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt4.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    Dignosis.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    Dignosis.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    Dignosis.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    Dignosis.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    Dignosis.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    Dignosis.Cells[1, 1].LoadFromDataTable(dt4, true);



                }
                if (dt.Rows.Count > 0)
                {

                    string Header = "Age wise premium(without maternity)";
                    int rowstart = 2;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet.Cells[2, 1].LoadFromDataTable(dt, true);

                    int rowstart1 = 1;
                    int colstart1 = 1;
                    int rowend1 = rowstart1;
                    int colend1 = colstart1;

                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Value = dt.TableName;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Font.Bold = true;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DeepSkyBlue);
                    workSheet.Cells[1, 1].LoadFromText(Header);


                }
                if (dt1.Rows.Count > 0)
                {

                    string Header = "Age wise Member count";
                    int rowstart = 13;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt1.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[13, 1].LoadFromDataTable(dt1, true);

                    int rowstart1 = 12;
                    int colstart1 = 1;
                    int rowend1 = rowstart1;
                    int colend1 = colstart1;

                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Value = dt.TableName;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Font.Bold = true;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DeepSkyBlue);
                    workSheet.Cells[12, 1].LoadFromText(Header);
                }


                if (dt2.Rows.Count > 0)
                {
                    string Header = "Relationship wise member count";
                    int rowstart = 25;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt2.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[25, 1].LoadFromDataTable(dt2, true);



                    int rowstart1 = 24;
                    int colstart1 = 1;
                    int rowend1 = rowstart1;
                    int colend1 = colstart1;

                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Value = dt.TableName;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Font.Bold = true;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DeepSkyBlue);
                    workSheet.Cells[24, 1].LoadFromText(Header);
                }
                //
                if (dt3.Rows.Count > 0)
                {
                    int rowstart = 35;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt3.Columns.Count;
                    string Header = "Claim status Summery";
                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[35, 1].LoadFromDataTable(dt3, true);


                    int rowstart1 = 34;
                    int colstart1 = 1;
                    int rowend1 = rowstart1;
                    int colend1 = colstart1;

                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Value = dt.TableName;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Font.Bold = true;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart1, colstart1, rowend1, colend1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DeepSkyBlue);
                    workSheet.Cells[34, 1].LoadFromText(Header);
                }






                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }

        /// <summary>
        /// Builds the compact underwriting summary used by the GMC Calculator.
        /// The eight result sets are returned by dbo.udsp_GetGMC_DownloadSummary.
        /// </summary>
        public void EPPlusExportGmcSummary(DataSet ds, string tempFile)
        {
            if (ds.Tables.Count < 8)
                throw new InvalidOperationException(
                    "The GMC summary procedure did not return all required result sets.");

            var directory = Path.GetDirectoryName(tempFile);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("GMC Summary");
            ws.View.ShowGridLines = false;

            WriteKeyValueSummarySection(ws, "Policy Details", ds.Tables[0], 1, 1, 3);
            WriteKeyValueSummarySection(ws, "Policy Features", ds.Tables[1], 10, 1, 3);
            WriteSummaryTable(ws, "Relationship wise lives", ds.Tables[2], 1, 6);
            WriteKeyValueSummarySection(ws, "Demographic Parameters", ds.Tables[3], 10, 6, 2);
            WriteSummaryTable(ws, "Paid Claims", ds.Tables[4], 18, 6);
            WriteSummaryTable(ws, "Outstanding Claims", ds.Tables[5], 23, 1);
            WriteKeyValueSummarySection(ws, "IBNR Working", ds.Tables[6], 23, 6, 2);

            // Burn details follow the policy/claim summary as requested.
            WriteSummaryTable(ws, "Burn Details", ds.Tables[7], 32, 1);

            ws.Cells[ws.Dimension.Address].Style.Font.Name = "Calibri";
            ws.Cells[ws.Dimension.Address].Style.Font.Size = 10;
            ws.Cells[ws.Dimension.Address].AutoFitColumns(11, 24);
            ws.Column(1).Width = 23;
            ws.Column(2).Width = 19;
            ws.Column(3).Width = 18;
            ws.Column(6).Width = 23;
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;

            package.SaveAs(new FileInfo(tempFile));
        }

        private static void WriteKeyValueSummarySection(
            ExcelWorksheet ws, string title, DataTable table, int startRow, int startColumn, int width)
        {
            WriteSummaryTitle(ws, title, startRow, startColumn, width);
            var row = startRow + 1;

            foreach (DataRow dataRow in table.Rows)
            {
                var label = dataRow["FieldName"]?.ToString() ?? string.Empty;
                var valueColumn = table.Columns.Contains("FieldValue") ? "FieldValue" : "NumericValue";
                var value = dataRow[valueColumn] == DBNull.Value ? string.Empty : dataRow[valueColumn];

                if (width > 2)
                    ws.Cells[row, startColumn, row, startColumn + width - 2].Merge = true;

                var labelCell = ws.Cells[row, startColumn];
                labelCell.Value = label;
                labelCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                var valueCell = ws.Cells[row, startColumn + width - 1];
                valueCell.Value = value;
                valueCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                valueCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 153));

                ApplySummaryBorder(ws.Cells[row, startColumn, row, startColumn + width - 1]);
                if (value is decimal or double or float)
                    valueCell.Style.Numberformat.Format =
                        label.Contains('%') ? "0.0\\%" : "#,##0.0";
                row++;
            }
        }

        private static void WriteSummaryTable(
            ExcelWorksheet ws, string title, DataTable table, int startRow, int startColumn)
        {
            // SortOrder is an internal positioning column and is not exported.
            var columns = table.Columns.Cast<DataColumn>()
                .Where(c => !string.Equals(c.ColumnName, "SortOrder", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var width = Math.Max(columns.Count, 1);

            WriteSummaryTitle(ws, title, startRow, startColumn, width);

            var headerRow = startRow + 1;
            for (var colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var cell = ws.Cells[headerRow, startColumn + colIndex];
                cell.Value = FriendlySummaryHeader(columns[colIndex].ColumnName);
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(217, 225, 242));
            }
            ApplySummaryBorder(ws.Cells[headerRow, startColumn, headerRow, startColumn + width - 1]);

            var rowIndex = headerRow + 1;
            foreach (DataRow dataRow in table.Rows)
            {
                for (var colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    var column = columns[colIndex];
                    var cell = ws.Cells[rowIndex, startColumn + colIndex];
                    cell.Value = dataRow[column] == DBNull.Value ? null : dataRow[column];
                    cell.Style.HorizontalAlignment = column.DataType == typeof(string)
                        ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Left
                        : OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                    if (column.ColumnName.Contains("Pct", StringComparison.OrdinalIgnoreCase)
                        || column.ColumnName.Contains("Ratio", StringComparison.OrdinalIgnoreCase))
                        cell.Style.Numberformat.Format = "0.0\\%";
                    else if (column.ColumnName.Contains("Amount", StringComparison.OrdinalIgnoreCase)
                             || column.ColumnName.Equals("ACS", StringComparison.OrdinalIgnoreCase))
                        cell.Style.Numberformat.Format = "#,##0";
                }

                if (string.Equals(dataRow[columns[0]]?.ToString(), "Total", StringComparison.OrdinalIgnoreCase))
                    ws.Cells[rowIndex, startColumn, rowIndex, startColumn + width - 1].Style.Font.Bold = true;

                ApplySummaryBorder(ws.Cells[rowIndex, startColumn, rowIndex, startColumn + width - 1]);
                rowIndex++;
            }
        }

        private static void WriteSummaryTitle(
            ExcelWorksheet ws, string title, int row, int column, int width)
        {
            ws.Cells[row, column, row, column + width - 1].Merge = true;
            var range = ws.Cells[row, column, row, column + width - 1];
            range.Value = title;
            range.Style.Font.Bold = true;
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 45, 98));
            ApplySummaryBorder(range);
        }

        private static void ApplySummaryBorder(ExcelRange range)
        {
            range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        }

        private static string FriendlySummaryHeader(string name)
        {
            return name switch
            {
                "MixPct" => "% Mix",
                "AmountPct" => "Amt%",
                "CountPct" => "Count%",
                "PaidRatio" => "Paid Ratio",
                "ClaimCount" => "Count",
                "ClaimedAmount" => "Claimed Amt",
                "PaidAmount" => "Paid Amt",
                "OutstandingAmount" => "O/s Amt",
                "NoOfClaims" => "No. of Claims",
                _ => name
            };
        }

        public void EPPlusExport(DataSet ds, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);



                //excelPackage.Encryption.Password = "12345";



                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportWithOutEmail(DataSet ds, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);



                //excelPackage.Encryption.Password = "12345";



                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportDataTable(DataTable dttemp, string TempFile)
        {
            var random = new Random();
            DataTable dt = dttemp;

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);



                //excelPackage.Encryption.Password = "12345";



                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportDataTableWithoutMasking(DataTable dttemp, string TempFile)
        {
            var random = new Random();
            DataTable dt = dttemp;

            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);



                //excelPackage.Encryption.Password = "12345";



                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }

        //public void EPPlusExport(DataSet ds, string TempFile)
        //{
        //    var random = new Random();
        //    DataTable dt = ds.Tables[0];

        //    //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //    using (ExcelPackage excelPackage = new ExcelPackage())
        //    {
        //        var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");

        //        dt.TableName = "sheet1";

        //        int rowstart = 1;
        //        int colstart = 1;
        //        int rowend = rowstart;
        //        int colend = dt.Columns.Count;

        //        // Apply header styling in bulk
        //        using (var range = workSheet.Cells[rowstart, colstart, rowend, colend])
        //        {
        //            range.Merge = true; // Merge header cells
        //            range.Value = dt.TableName; // Header text
        //            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        //            range.Style.Font.Bold = true;
        //            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
        //        }

        //        // Load data into the worksheet
        //        workSheet.Cells[1, 1].LoadFromDataTable(msking.maskingData(dt), true); // Load data starting from row 2

        //        // Save the package to a MemoryStream for better performance
        //        using (var memoryStream = new MemoryStream())
        //        {
        //            excelPackage.SaveAs(memoryStream);
        //            File.WriteAllBytes(TempFile, memoryStream.ToArray());
        //        }

        //    }
        //}

        public void ExportToCsv(DataTable dataTable, string fileName)
        {
            // Use a StreamWriter with a using block to ensure proper disposal
            using (var csvFileWriter = new StreamWriter(fileName))
            {
                // Write the header row
                var header = string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(col => EscapeCsvField(col.ColumnName)));
                csvFileWriter.WriteLine(header);

                // Use a StringBuilder for batching rows to reduce I/O operations
                var sb = new StringBuilder();

                foreach (DataRow row in dataTable.Rows)
                {
                    // Convert and escape each field in the row
                    var rowValues = row.ItemArray.Select(field => EscapeCsvField(field.ToString()));
                    sb.AppendLine(string.Join(",", rowValues));

                    // Write to the file in batches for better performance
                    if (sb.Length > 10000) // Write every 10,000 characters (adjust as needed)
                    {
                        csvFileWriter.Write(sb.ToString());
                        sb.Clear();
                    }
                }

                // Write any remaining data in the StringBuilder
                if (sb.Length > 0)
                {
                    csvFileWriter.Write(sb.ToString());
                }
            }
        }
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // Escape double quotes and wrap the field in double quotes if it contains a comma, double quotes, or newline
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                field = field.Replace("\"", "\"\""); // Escape double quotes
                return $"\"{field}\"";
            }

            return field;
        }
        public void EPPlusExportWithPassword(DataSet ds, string TempFile,string randomNumber)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];

            //ExcelPackage.License.SetNonCommercialOrganization("RGI");
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");


                dt.TableName = "sheet1";

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                workSheet.Cells[1, 1].LoadFromDataTable(dt, true);



                //excelPackage.Encryption.Password = "12345";

                excelPackage.Encryption.Algorithm = EncryptionAlgorithm.AES192;

                excelPackage.SaveAs(new FileInfo(TempFile), randomNumber);

            }

        }
      
            private static readonly char[] chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        public  string GenerateCode()
        {
                

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[8];
                rng.GetBytes(bytes);

                var result = new StringBuilder(8);
                foreach (var byteValue in bytes)
                {
                    result.Append(chars[byteValue % chars.Length]);
                }

                return result.ToString();
            }
        }
        public async  Task<DataTable> GetDataFromExcel(string sFilename)
        {
            string connString = "";
            OleDbConnection oledbConn = null;
            OleDbDataAdapter oledbDataAdapter = null;
            try
            {
               string strFileType = System.IO.Path.GetExtension(sFilename).ToLower();
                if (strFileType.Trim() == ".xls")
                {
                    connString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source='{0}';Extended Properties=\'Excel 8.0;HDR=Yes;IMEX=2\'";
                }
                else if (strFileType.Trim() == ".xlsx" || strFileType.Trim() == ".xlsb")
                {
                    connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='{0}';Extended Properties=\'Excel 8.0;HDR=Yes;IMEX=1\'";
                }
                //string connectionString = "server=10.65.15.119,7359;integrated security=false;database=RelianceCommission_Dev;user id=cpsuser;password=cpsuat@123";

                string excelConnectionString = connString.Replace("{0}", sFilename);
                oledbConn = new OleDbConnection(excelConnectionString);
               await oledbConn.OpenAsync();
                string cmdtext = "Select  * from [sheet1$]";
                oledbDataAdapter = new OleDbDataAdapter(cmdtext, oledbConn);
                System.Data.DataTable dt = new System.Data.DataTable();
                oledbDataAdapter.Fill(dt);

                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        return dt;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
            finally
            {
                if (oledbConn.State == ConnectionState.Open)
                {
                    oledbConn.Close();
                    oledbConn.Dispose();
                }
            }
        }
        public void EPPlusExportDS(DataSet ds, DataSet ds1, string TempFile)
        {
            var random = new Random();
            DataTable dt = ds.Tables[0];
            DataTable dt1 = ds.Tables[1];
            DataTable dt2 = ds.Tables[2];
            DataTable dt3 = ds.Tables[3];
            DataTable dt4 = ds.Tables[4];
            DataTable dt5 = ds.Tables[5];
            DataTable dt6 = ds.Tables[6];
            DataTable dt7 = ds.Tables[7];
            
            DataTable dt8 = ds1!=null?ds1.Tables[0]:null;

            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");
                var workSheet1 = excelPackage.Workbook.Worksheets.Add("Enrollment Data");
                var workSheet2 = excelPackage.Workbook.Worksheets.Add("Claim Data");
                var workSheet3 = excelPackage.Workbook.Worksheets.Add("Revised Data");
                var workSheet4 = excelPackage.Workbook.Worksheets.Add("Terms And Condition");

                if (dt8!=null)
                {
                    dt8.TableName = "Terms And Condition";
                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt8.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet4.Cells[rowstart, colstart, rowend, colend].Value = dt8.TableName;
                    workSheet4.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet4.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet4.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet4.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet4.Cells[1, 1].LoadFromDataTable(dt8, true);

                }

                if (dt5.Rows.Count > 0)
                {
                    dt5.TableName = "Enrollment Data";
                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt5.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet1.Cells[rowstart, colstart, rowend, colend].Value = dt5.TableName;
                    workSheet1.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet1.Cells[1, 1].LoadFromDataTable(dt5, true);

                }
                if (dt6.Rows.Count > 0)
                {
                    dt6.TableName = "Claim Data";
                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt6.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet2.Cells[rowstart, colstart, rowend, colend].Value = dt6.TableName;
                    workSheet2.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet2.Cells[1, 1].LoadFromDataTable(dt6, true);

                }
                if (dt7.Rows.Count > 0)
                {
                    dt7.TableName = "Revised Data";
                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt7.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet3.Cells[rowstart, colstart, rowend, colend].Value = dt7.TableName;
                    workSheet3.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet3.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet3.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet3.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet3.Cells[1, 1].LoadFromDataTable(dt7, true);

                }
                if (dt.Rows.Count > 0)
                {
                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet.Cells[1, 1].LoadFromDataTable(dt, true);


                }


                if (dt1.Rows.Count > 0)
                {
                    int rowstart = 4;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt1.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt1.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[4, 1].LoadFromDataTable(dt1, true);
                }


                if (dt2.Rows.Count > 0)
                {
                    int rowstart = 26;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt2.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt2.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[26, 1].LoadFromDataTable(dt2, true);
                }

                if (dt3.Rows.Count > 0)
                {

                    int rowstart = 29;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt3.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt3.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);


                    workSheet.Cells[29, 1].LoadFromDataTable(dt3, true);
                }

                if (dt4.Rows.Count > 0)
                {

                    int rowstart = 47;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt4.Columns.Count;

                    //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt4.TableName;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Cells[47, 1].LoadFromDataTable(dt4, true);
                }



                excelPackage.SaveAs(new FileInfo(TempFile));

            }

        }
        public void EPPlusExportDS(DataSet ds, string TempFile)
        {
            try
            {
                var random = new Random();
                DataTable dt = ds.Tables[0];
                DataTable dt1 = ds.Tables[1];
                DataTable dt2 = ds.Tables[2];
                DataTable dt3 = ds.Tables[3];
                DataTable dt4 = ds.Tables[4];
                DataTable dt5 = ds.Tables[5];
                DataTable dt6 = ds.Tables[6];

                using (ExcelPackage excelPackage = new ExcelPackage())
                {
                    var workSheet = excelPackage.Workbook.Worksheets.Add("sheet1");
                    var workSheet1 = excelPackage.Workbook.Worksheets.Add("Enrollment Data");
                    var workSheet2 = excelPackage.Workbook.Worksheets.Add("Claim Data");

                    if (dt5.Rows.Count > 0)
                    {
                        dt5.TableName = "Enrollment Data";
                        int rowstart = 1;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt5.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet1.Cells[rowstart, colstart, rowend, colend].Value = dt5.TableName;
                        workSheet1.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet1.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                        workSheet1.Cells[1, 1].LoadFromDataTable(dt5, true);

                    }
                    if (dt6.Rows.Count > 0)
                    {
                        dt6.TableName = "Claim Data";
                        int rowstart = 1;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt6.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet2.Cells[rowstart, colstart, rowend, colend].Value = dt6.TableName;
                        workSheet2.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet2.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                        workSheet2.Cells[1, 1].LoadFromDataTable(dt6, true);

                    }
                    if (dt.Rows.Count > 0)
                    {
                        int rowstart = 1;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                        workSheet.Cells[1, 1].LoadFromDataTable(dt, true);


                    }


                    if (dt1.Rows.Count > 0)
                    {
                        int rowstart = 4;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt1.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt1.TableName;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                        workSheet.Cells[4, 1].LoadFromDataTable(dt1, true);
                    }


                    if (dt2.Rows.Count > 0)
                    {
                        int rowstart = 26;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt2.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt2.TableName;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                        workSheet.Cells[26, 1].LoadFromDataTable(dt2, true);
                    }

                    if (dt3.Rows.Count > 0)
                    {

                        int rowstart = 29;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt3.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt3.TableName;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);


                        workSheet.Cells[29, 1].LoadFromDataTable(dt3, true);
                    }

                    if (dt4.Rows.Count > 0)
                    {

                        int rowstart = 47;
                        int colstart = 1;
                        int rowend = rowstart;
                        int colend = dt4.Columns.Count;

                        //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Value = dt4.TableName;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        workSheet.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                        workSheet.Cells[47, 1].LoadFromDataTable(dt4, true);
                    }



                    excelPackage.SaveAs(new FileInfo(TempFile));

                }
            }
            catch (Exception ee)
            {

                throw;
            }
            

        }

        public DataTable? ToExcelsSheetDataTable(string filePath)
        {
            OleDbConnectionStringBuilder sbConnection = new OleDbConnectionStringBuilder();
            String strExtendedProperties = String.Empty;
            sbConnection.DataSource = filePath;

            if (System.IO.Path.GetExtension(filePath).Equals(".xls"))//for 97-03 Excel file
            {
                sbConnection.Provider = "Microsoft.Jet.OLEDB.4.0";
                strExtendedProperties = "Excel 8.0;HDR=Yes;IMEX=1";//HDR=ColumnHeader,IMEX=InterMixed
            }
            else if (System.IO.Path.GetExtension(filePath).Equals(".xlsx"))  //for 2007 Excel file
            {
                sbConnection.Provider = "Microsoft.ACE.OLEDB.12.0";
                strExtendedProperties = "Excel 12.0;HDR=Yes;IMEX=1";
            }
            sbConnection.Add("Extended Properties", strExtendedProperties);
            using (OleDbConnection conn = new OleDbConnection(sbConnection.ToString()))
            {
                conn.Open();
                DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                return dtSheet;
            }
            
        }

        public List<string> ToExcelsSheetList(string filePath)
        {
            OleDbConnectionStringBuilder sbConnection = new OleDbConnectionStringBuilder();
            String strExtendedProperties = String.Empty;
            sbConnection.DataSource = filePath;

            if (System.IO.Path.GetExtension(filePath).Equals(".xls"))//for 97-03 Excel file
            {
                sbConnection.Provider = "Microsoft.Jet.OLEDB.4.0";
                strExtendedProperties = "Excel 8.0;HDR=Yes;IMEX=1";//HDR=ColumnHeader,IMEX=InterMixed
            }
            else if (System.IO.Path.GetExtension(filePath).Equals(".xlsx"))  //for 2007 Excel file
            {
                sbConnection.Provider = "Microsoft.ACE.OLEDB.12.0";
                strExtendedProperties = "Excel 12.0;HDR=Yes;IMEX=1";
            }
            sbConnection.Add("Extended Properties", strExtendedProperties);
            List<string> listSheet = new List<string>();
            using (OleDbConnection conn = new OleDbConnection(sbConnection.ToString()))
            {
                conn.Open();
                DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                foreach (DataRow drSheet in dtSheet.Rows)
                {
                    string s = drSheet["TABLE_NAME"].ToString();
                    if (drSheet["TABLE_NAME"].ToString().EndsWith("$"))//checks whether row contains '_xlnm#_FilterDatabase' or sheet name(i.e. sheet name always ends with $ sign)
                    {
                        
                        listSheet.Add(drSheet["TABLE_NAME"].ToString());
                    }
                }
            }
            return listSheet;
        }


       
       

       
        public string[] SourceExcelFileFormat(DataTable dt)
        {
            string[] strArray1 = null;
            int intCnt = 0;
            strArray1 = new string[dt.Columns.Count];
            try
            {
                foreach (DataColumn column in dt.Columns)
                {
                    strArray1[intCnt] = column.ColumnName;
                    intCnt++;
                }
                return strArray1;
            }
            catch (Exception ex)
            {
                strArray1 = new string[1];
                strArray1[0] = "~.~";
                return strArray1;
            }
        }


     
        public bool ArrComparison(string[] source, string[] destination)
        {
            if (source.Length == destination.Length)
            {
                for (long i = 0; i <= source.GetUpperBound(0); i++)
                {
                    if (source[i].Trim().ToUpper() != destination[i].Trim().ToUpper())
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public bool IsNumeric(string s)
        {
            try
            {
                if (s != "")
                    Decimal.Parse(s);
                else if (s == "")
                    return false;
                else
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public DataTable ValidateData(DataTable dt, string sColumnName)
        {
            DataTable dtvalidatedTable = dt;
            DataTable dtnew = new DataTable();
            dtnew = dtvalidatedTable.Clone();
            if (dt.Columns.Contains(sColumnName))
            {
                string a = sColumnName;
            }
            //dtnew.Columns.Add("Error");
            //int iRows = dt.Rows.Count;
            //int iCols = dt.Columns.Count;
            try
            {
                //    for (int i = 0; i < iRows; i++)
                //    {
                //        string status = "";
                //        for (int x = 0; x < iCols; x++)
                //        {
                //            if (IsNumeric(dt.Rows[i][x].ToString()) == false)
                //            {
                //                status = "Invalid";
                //                break;
                //            }
                //            else
                //            {
                //                status = "valid";
                //            }
                //        }
                //        if (status == "Invalid")
                //        {
                //            DataRow dr = dt.Rows[i];
                //            dtnew.ImportRow(dr);
                //        }
                //    }
                return dtnew;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }


        //Used
        public string GetSerializeDataToXml(DataTable dt, string sRootElementName)
        {
            dt.TableName = sRootElementName;
            MemoryStream ms = new MemoryStream();
            XmlDocument xmlDoc = new XmlDocument();
            string xmlString = "";          

            dt.WriteXml(ms, XmlWriteMode.IgnoreSchema, true);
            ms.Seek(0, SeekOrigin.Begin);

            xmlString = ASCIIEncoding.UTF8.GetString(ms.ToArray());
            xmlString = xmlString.Replace("encoding=\\\"utf-8\\\"", "");
            xmlDoc.LoadXml(xmlString);
            return xmlString;
        }

        


       
        public string EscapeXml(string sXML)
        {
            string toxml = sXML;
            if (!string.IsNullOrEmpty(toxml))
            {
                // replace literal values with entities ! @ # $ % ^ & * " '
                toxml = toxml.Replace("&", "&amp;");
                toxml = toxml.Replace("'", "&apos;");
                toxml = toxml.Replace(@"""", "&quot");
                //toxml = toxml.Replace("\"", "&quot;");
                //toxml = toxml.Replace(">", "&gt;");
                //toxml = toxml.Replace("<", "&lt;");
            }
            return toxml;
        }

        public string UnescapeXml(string sXML)
        {
            string unxml = sXML;
            if (!string.IsNullOrEmpty(unxml))
            {
                // replace entities with literal values
                //unxml = unxml.Replace("&quot;", "\"");
                //unxml = unxml.Replace("&gt;", ">");
                //unxml = unxml.Replace("&lt;", "<");
                unxml = unxml.Replace("&quot",@"""" );
                unxml = unxml.Replace("&apos;", "'");
                unxml = unxml.Replace("&amp;", "&");
            }
            return unxml;
        }
        public void CreateExcelFileMS(DataSet ds1, string Filename)
        {
            try
            {
                using (DataSet ds = ds1)
                {

                    if (ds != null && ds.Tables.Count > 0)
                    {
                        //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (ExcelPackage xp = new ExcelPackage())
                        {
                            foreach (DataTable dt in ds.Tables)
                            {
                                var cnt = dt.Columns.Count;

                                if (cnt == 32)
                                {
                                    dt.TableName = "Master Statement";
                                }
                                else if (cnt == 54)
                                {
                                    dt.TableName = "Remuneration Statement";

                                }


                                else if (cnt == 34)
                                {
                                    dt.TableName = "Payments Statement – Commission";

                                }
                                else if (cnt == 26)
                                {
                                    dt.TableName = "Payments Statement";

                                }
                                ExcelWorksheet ws = xp.Workbook.Worksheets.Add(dt.TableName);

                                int rowstart = 1;
                                int colstart = 1;
                                int rowend = rowstart;
                                int colend = dt.Columns.Count;

                                //ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                                ws.Cells[rowstart, colstart, rowend, colend].Value = dt.TableName;
                                ws.Cells[rowstart, colstart, rowend, colend].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                                rowstart += 0;
                                rowend = rowstart + dt.Rows.Count;
                                ws.Cells[rowstart, colstart].LoadFromDataTable(dt, true);
                                int i = 0;
                                foreach (DataColumn dc in dt.Columns)
                                {
                                    i++;
                                    if (dc.DataType == typeof(float))
                                        ws.Column(i).Style.Numberformat.Format = "#0.00";
                                }
                                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Border.Top.Style =
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Border.Bottom.Style =
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Border.Left.Style =
                                ws.Cells[rowstart, colstart, rowend, colend].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                            }
                            xp.SaveAs(new FileInfo(Filename));
                            //    Response.AddHeader("content-disposition", "attachment;filename=" + Filename);
                            //    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            //    Response.BinaryWrite(xp.GetAsByteArray());
                            //    //Response.End();
                            //    HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                            //    HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                            //    HttpContext.Current.ApplicationInstance.CompleteRequest();
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }

}
