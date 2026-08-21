using System.Data;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GMC.Helper
{
    public static class TrendAnalysisExcelExporter
    {
        private static readonly Color Navy = Color.FromArgb(0, 45, 98);
        private static readonly Color Red = Color.FromArgb(192, 0, 0);

        private static readonly (string Label, string Column, bool Percent)[] TrendMetrics =
        {
            ("Inception Premium", "InceptionPremium", false),
            ("End Premium", "EndPremium", false),
            ("Inception Lives", "InceptionLives", false),
            ("End Lives", "EndLives", false),
            ("Wtd. Avg. Lives", "WtdAvgLives", false),
            ("Loss Ratio", "LossRatio", true),
            ("TPA Fees", "TpaFeesPct", true),
            ("Brokerage", "BrokeragePct", true),
            ("LR incl. TPA & Brokerage", "LRInclTpaBrokerage", true),
            ("No. of claims with IBNR", "NoOfClaimsWithIBNR", false),
            ("Claims with IBNR", "ClaimsWithIBNR", false),
            ("ACS", "ACS", false),
            ("IR", "IR", true),
            ("Risk Rate", "RiskRate", false),
            ("Inflation", "Inflation", true),
            ("Premium Per Life", "PremiumPerLife", false)
        };

        public static byte[] Build(DataSet data, string policyNo, string? financialYear)
        {
            if (data.Tables.Count < 5)
                throw new InvalidOperationException("Trend Analysis data is incomplete.");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Trend Analysis");
            ws.View.ShowGridLines = false;

            var trend = data.Tables[0].Rows.Cast<DataRow>()
                .OrderBy(r => Text(r, "UWYear")).ToList();
            var width = Math.Max(2, trend.Count + 1);
            var row = 1;

            WriteTitle(ws, row++, 1, width, "Trend Analysis");
            WriteHeaders(ws, row, new[] { "UW Year" }.Concat(
                trend.Select(r => Text(r, "UWYear"))).ToArray());
            row++;

            foreach (var metric in TrendMetrics)
            {
                ws.Cells[row, 1].Value = metric.Label;
                ws.Cells[row, 1].Style.Font.Bold = true;
                for (var index = 0; index < trend.Count; index++)
                {
                    SetValue(ws.Cells[row, index + 2], trend[index][metric.Column]);
                    ws.Cells[row, index + 2].Style.Numberformat.Format =
                        metric.Percent ? "0.0\\%" : "#,##,##0";
                }
                ApplyBorder(ws.Cells[row, 1, row, width]);
                row++;
            }

            var parties = data.Tables[1];
            foreach (var party in new[]
                     {
                         ("Insurer Name", "InsurerName"),
                         ("TPA Name", "TpaName"),
                         ("Broker Name", "BrokerName")
                     })
            {
                ws.Cells[row, 1].Value = party.Item1;
                ws.Cells[row, 1].Style.Font.Bold = true;
                for (var index = 0; index < trend.Count; index++)
                {
                    var year = Text(trend[index], "UWYear");
                    var match = parties.Rows.Cast<DataRow>()
                        .FirstOrDefault(r => Text(r, "UWYear") == year);
                    ws.Cells[row, index + 2].Value =
                        match == null ? string.Empty : Text(match, party.Item2);
                }
                ApplyBorder(ws.Cells[row, 1, row, width]);
                row++;
            }

            row += 2;
            row = WriteTable(ws, row, "Relationship wise lives", data.Tables[4],
                new[]
                {
                    ("RelationGroup", "Relationship wise lives", ""),
                    ("Female", "Female", "#,##,##0"),
                    ("Male", "Male", "#,##,##0"),
                    ("Total", "Total", "#,##,##0"),
                    ("MixPct", "% Mix", "0.0\\%")
                }, "RelationGroup");

            row += 2;
            foreach (var year in DistinctYears(data.Tables[2]))
            {
                var rows = data.Tables[2].Rows.Cast<DataRow>()
                    .Where(r => Text(r, "UWYear") == year)
                    .OrderBy(r => Number(r, "SortOrder")).ToList();
                row = WriteTable(ws, row, year, rows,
                    new[]
                    {
                        ("RelationGroup", "Relationship", ""),
                        ("IncurredAmount", "Incurred Amount", "#,##,##0"),
                        ("ClaimsCount", "Claims Count", "#,##,##0"),
                        ("ClaimsCountWithIBNR", "Claims Count with IBNR", "#,##,##0"),
                        ("ACS", "ACS", "#,##,##0"),
                        ("IR", "IR", "0.0\\%")
                    }, "RelationGroup");
                row += 2;
            }

            foreach (var year in DistinctYears(data.Tables[3]))
            {
                var rows = data.Tables[3].Rows.Cast<DataRow>()
                    .Where(r => Text(r, "UWYear") == year)
                    .OrderBy(r => Number(r, "SortOrder"))
                    .ThenByDescending(r => Number(r, "IncurredAmount")).ToList();
                row = WriteTable(ws, row, year, rows,
                    new[]
                    {
                        ("DiseaseCategory", "Disease Category", ""),
                        ("IncurredAmount", "Incurred Amount", "#,##,##0"),
                        ("ClaimsCount", "Claims Count", "#,##,##0"),
                        ("ClaimsCountWithIBNR", "Claims Count with IBNR", "#,##,##0"),
                        ("ACS", "ACS", "#,##,##0"),
                        ("IR", "IR", "0.0\\%")
                    }, "DiseaseCategory");
                row += 2;
            }

            ws.Cells[ws.Dimension.Address].Style.Font.Name = "Calibri";
            ws.Cells[ws.Dimension.Address].Style.Font.Size = 10;
            ws.Cells[ws.Dimension.Address].AutoFitColumns(11, 30);
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 31);
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
            ws.HeaderFooter.OddHeader.CenteredText =
                $"&BTrend Analysis - {policyNo} - {(string.IsNullOrWhiteSpace(financialYear) ? "All Years" : financialYear)}";

            return package.GetAsByteArray();
        }

        private static int WriteTable(
            ExcelWorksheet ws,
            int startRow,
            string title,
            DataTable table,
            (string Column, string Header, string Format)[] columns,
            string totalColumn)
            => WriteTable(ws, startRow, title, table.Rows.Cast<DataRow>(), columns, totalColumn);

        private static int WriteTable(
            ExcelWorksheet ws,
            int startRow,
            string title,
            IEnumerable<DataRow> sourceRows,
            (string Column, string Header, string Format)[] columns,
            string totalColumn)
        {
            var rows = sourceRows.ToList();
            WriteTitle(ws, startRow, 1, columns.Length, title);
            WriteHeaders(ws, startRow + 1, columns.Select(c => c.Header).ToArray());

            var row = startRow + 2;
            foreach (var dataRow in rows)
            {
                for (var index = 0; index < columns.Length; index++)
                {
                    var definition = columns[index];
                    SetValue(ws.Cells[row, index + 1], dataRow[definition.Column]);
                    if (!string.IsNullOrEmpty(definition.Format))
                        ws.Cells[row, index + 1].Style.Numberformat.Format = definition.Format;
                }

                var totalLabel = Text(dataRow, totalColumn);
                if (totalLabel is "Total" or "Overall")
                    ws.Cells[row, 1, row, columns.Length].Style.Font.Bold = true;
                ApplyBorder(ws.Cells[row, 1, row, columns.Length]);
                row++;
            }
            return row;
        }

        private static void WriteTitle(
            ExcelWorksheet ws, int row, int startColumn, int width, string title)
        {
            var range = ws.Cells[row, startColumn, row, startColumn + width - 1];
            range.Merge = true;
            range.Value = title;
            range.Style.Font.Bold = true;
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Red);
            ApplyBorder(range);
        }

        private static void WriteHeaders(ExcelWorksheet ws, int row, string[] headers)
        {
            for (var index = 0; index < headers.Length; index++)
            {
                var cell = ws.Cells[row, index + 1];
                cell.Value = headers[index];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Navy);
            }
            ApplyBorder(ws.Cells[row, 1, row, headers.Length]);
        }

        private static void SetValue(ExcelRange cell, object value)
        {
            cell.Value = value == DBNull.Value ? null : value;
            cell.Style.HorizontalAlignment = value is string
                ? ExcelHorizontalAlignment.Left
                : ExcelHorizontalAlignment.Right;
        }

        private static IEnumerable<string> DistinctYears(DataTable table) =>
            table.Rows.Cast<DataRow>().Select(r => Text(r, "UWYear"))
                .Where(y => y.Length > 0).Distinct().OrderByDescending(y => y);

        private static string Text(DataRow row, string column) =>
            row[column] == DBNull.Value ? string.Empty : row[column]?.ToString()?.Trim() ?? string.Empty;

        private static decimal Number(DataRow row, string column) =>
            row[column] == DBNull.Value ? 0 : Convert.ToDecimal(row[column]);

        private static void ApplyBorder(ExcelRange range)
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }
    }
}
