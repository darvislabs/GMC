using System.Data;

namespace GMC.Helper
{
    public class CommonUti
    {
        
        public static void exportToExcel(DataTable source, string fileName, int RowCount)
        {
            if (RowCount == 0 || RowCount == 1)
            {
                RowCount = 2;
            }
            System.IO.StreamWriter excelDoc;
            excelDoc = new System.IO.StreamWriter(fileName);
            const string startExcelXML = "<xml version>\r\n<Workbook " +
                  "xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n" +
                  " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n " +
                  "xmlns:x=\"urn:schemas-    microsoft-com:office:" +
                  "excel\"\r\n xmlns:ss=\"urn:schemas-microsoft-com:" +
                  "office:spreadsheet\">\r\n <Styles>\r\n " +
                  "<Style ss:ID=\"Default\" ss:Name=\"Normal\">\r\n " +
                  "<Alignment ss:Vertical=\"Bottom\"/>\r\n <Borders/>" +
                  "\r\n <Font/>\r\n <Interior/>\r\n <NumberFormat/>" +
                  "\r\n <Protection/>\r\n </Style>\r\n " +
                  "<Style ss:ID=\"BoldColumn\">\r\n <Font " +
                  "x:Family=\"Swiss\" ss:Bold=\"1\"/>\r\n </Style>\r\n " +
                  "<Style     ss:ID=\"StringLiteral\">\r\n <NumberFormat" +
                  " ss:Format=\"@\"/>\r\n </Style>\r\n <Style " +
                  "ss:ID=\"Decimal\">\r\n <NumberFormat " +
                  "ss:Format=\"0.0000\"/>\r\n </Style>\r\n " +
                  "<Style ss:ID=\"Integer\">\r\n <NumberFormat " +
                  "ss:Format=\"0\"/>\r\n </Style>\r\n <Style " +
                  "ss:ID=\"DateLiteral\">\r\n <NumberFormat " +
                  "ss:Format=\"mm/dd/yyyy;@\"/>\r\n </Style>\r\n " +
                  "<Style ss:ID=\"Amar\"><Font ss:FontName=\"Arial\" ss:Bold=\"1\"/><Interior ss:Color=\"#C5D9F1\" ss:Pattern=\"Solid\"/></Style>\r \n" +
                  "</Styles>\r\n ";
            const string endExcelXML = "</Workbook>";

            int rowCount = 0;
            int sheetCount = 1;
            excelDoc.Write(startExcelXML);
            excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
            excelDoc.Write("<Table>");
            excelDoc.Write("<Row>");
            for (int x = 0; x < source.Columns.Count; x++)
            {
                excelDoc.Write("<Cell ss:StyleID=\"Amar\"><Data ss:Type=\"String\">");
                excelDoc.Write(source.Columns[x].ColumnName);
                excelDoc.Write("</Data></Cell>");
            }
            excelDoc.Write("</Row>");
            foreach (DataRow x in source.Rows)
            {
                rowCount++;
                //if the number of rows is > 64000 create a new page to continue output
                if (rowCount == RowCount)
                {
                    rowCount = 0;
                    sheetCount++;

                    //excelDoc.Write("<Row>");
                    //for (int j = 0; j < source.Tables[0].Columns.Count; j++)
                    //{
                    //    excelDoc.Write("<Cell ss:StyleID=\"BoldColumn\"><Data ss:Type=\"String\">");
                    //    excelDoc.Write(source.Tables[0].Columns[j].ColumnName);
                    //    excelDoc.Write("</Data></Cell>");
                    //}
                    //excelDoc.Write("</Row>");

                    excelDoc.Write("</Table>");
                    excelDoc.Write(" </Worksheet>");
                    excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
                    excelDoc.Write("<Table>");
                    excelDoc.Write("<Row>");
                    for (int j = 0; j < source.Columns.Count; j++)
                    {
                        excelDoc.Write("<Cell ss:StyleID=\"Amar\"><Data ss:Type=\"String\">");
                        excelDoc.Write(source.Columns[j].ColumnName);
                        excelDoc.Write("</Data></Cell>");
                    }
                    excelDoc.Write("</Row>");
                }
                excelDoc.Write("<Row>"); //ID=" + rowCount + "
                for (int y = 0; y < source.Columns.Count; y++)
                {
                    System.Type rowType;
                    rowType = x[y].GetType();
                    switch (rowType.ToString())
                    {
                        case "System.String":
                            string XMLstring = x[y].ToString();
                            XMLstring = XMLstring.Trim();
                            XMLstring = XMLstring.Replace("&", "&");
                            XMLstring = XMLstring.Replace(">", ">");
                            XMLstring = XMLstring.Replace("<", "<");
                            excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                           "<Data ss:Type=\"String\">");
                            excelDoc.Write(XMLstring);
                            excelDoc.Write("</Data></Cell>");
                            break;
                        case "System.DateTime":
                            //Excel has a specific Date Format of YYYY-MM-DD followed by  
                            //the letter 'T' then hh:mm:sss.lll Example 2005-01-31T24:01:21.000
                            //The Following Code puts the date stored in XMLDate 
                            //to the format above
                            DateTime XMLDate = (DateTime)x[y];
                            string XMLDatetoString = ""; //Excel Converted Date
                            XMLDatetoString = XMLDate.Year.ToString() +
                                 "-" +
                                 (XMLDate.Month < 10 ? "0" +
                                 XMLDate.Month.ToString() : XMLDate.Month.ToString()) +
                                 "-" +
                                 (XMLDate.Day < 10 ? "0" +
                                 XMLDate.Day.ToString() : XMLDate.Day.ToString()) +
                                 "T" +
                                 (XMLDate.Hour < 10 ? "0" +
                                 XMLDate.Hour.ToString() : XMLDate.Hour.ToString()) +
                                 ":" +
                                 (XMLDate.Minute < 10 ? "0" +
                                 XMLDate.Minute.ToString() : XMLDate.Minute.ToString()) +
                                 ":" +
                                 (XMLDate.Second < 10 ? "0" +
                                 XMLDate.Second.ToString() : XMLDate.Second.ToString()) +
                                 ".000";
                            excelDoc.Write("<Cell ss:StyleID=\"DateLiteral\">" +
                                         "<Data ss:Type=\"DateTime\">");
                            excelDoc.Write(XMLDatetoString);
                            excelDoc.Write("</Data></Cell>");
                            break;
                        case "System.Boolean":
                            excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                        "<Data ss:Type=\"String\">");
                            excelDoc.Write(x[y].ToString());
                            excelDoc.Write("</Data></Cell>");
                            break;
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                        case "System.Byte":
                            excelDoc.Write("<Cell ss:StyleID=\"Integer\">" +
                                    "<Data ss:Type=\"Number\">");
                            excelDoc.Write(x[y].ToString());
                            excelDoc.Write("</Data></Cell>");
                            break;
                        case "System.Decimal":
                        case "System.Double":
                            excelDoc.Write("<Cell ss:StyleID=\"Decimal\">" +
                                  "<Data ss:Type=\"Number\">");
                            excelDoc.Write(x[y].ToString());
                            excelDoc.Write("</Data></Cell>");
                            break;
                        case "System.DBNull":
                            excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                  "<Data ss:Type=\"String\">");
                            excelDoc.Write("");
                            excelDoc.Write("</Data></Cell>");
                            break;
                        default:
                            throw (new Exception(rowType.ToString() + " not handled."));
                    }
                }
                excelDoc.Write("</Row>");
            }
            excelDoc.Write("</Table>");
            excelDoc.Write(" </Worksheet>");
            excelDoc.Write(endExcelXML);
            excelDoc.Close();
        }
        public static void exportToExcelMultipleSheets(DataSet source, string fileName, string[] SheetNames = null)
        {

            System.IO.StreamWriter excelDoc;
            excelDoc = new System.IO.StreamWriter(fileName);
            try
            {
                const string startExcelXML = "<xml version>\r\n<Workbook " +
                      "xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n" +
                      " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n " +
                      "xmlns:x=\"urn:schemas-    microsoft-com:office:" +
                      "excel\"\r\n xmlns:ss=\"urn:schemas-microsoft-com:" +
                      "office:spreadsheet\">\r\n <Styles>\r\n " +
                      "<Style ss:ID=\"Default\" ss:Name=\"Normal\">\r\n " +
                      "<Alignment ss:Vertical=\"Bottom\"/>\r\n <Borders/>" +
                      "\r\n <Font/>\r\n <Interior/>\r\n <NumberFormat/>" +
                      "\r\n <Protection/>\r\n </Style>\r\n " +
                      "<Style ss:ID=\"BoldColumn\">\r\n <Font " +
                      "x:Family=\"Swiss\" ss:Bold=\"1\"/>\r\n </Style>\r\n " +
                      "<Style     ss:ID=\"StringLiteral\">\r\n <NumberFormat" +
                      " ss:Format=\"@\"/>\r\n </Style>\r\n <Style " +
                      "ss:ID=\"Decimal\">\r\n <NumberFormat " +
                      "ss:Format=\"0.0000\"/>\r\n </Style>\r\n " +
                      "<Style ss:ID=\"Integer\">\r\n <NumberFormat " +
                      "ss:Format=\"0\"/>\r\n </Style>\r\n <Style " +
                      "ss:ID=\"DateLiteral\">\r\n <NumberFormat " +
                      "ss:Format=\"mm/dd/yyyy;@\"/>\r\n </Style>\r\n " +
                      "<Style ss:ID=\"s80\">\r\n"+
   "<Font ss:FontName=\"Arial\" x:Family=\"Swiss\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>\r\n"+
           
            "<Interior ss:Color=\"#5B9BD5\" ss:Pattern=\"Solid\"/>\r\n"+
                
                  "</Style>\r\n"+
                      "<Style ss:ID=\"s81\" >\r\n"+

     "<Font ss:FontName=\"Arial\" x:Family=\"Swiss\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>\r\n" +

       "<Interior ss:Color=\"#5B9BD5\" ss:Pattern=\"Solid\"/>\r\n" +
         
            "<NumberFormat ss:Format=\"@\"/>\r\n"+
           
             "</Style>\r\n"+

                                 "</Styles>\r\n ";
                const string endExcelXML = "</Workbook>";
                int sheetCount = 0;
                string sheetName = "";
                excelDoc.Write(startExcelXML);
                for (int i = 0; i < source.Tables.Count; i++)
                {

                    int rowCount = 0;
                    sheetCount++;
                    if (SheetNames != null)
                    {
                        if (SheetNames.Length < source.Tables.Count)
                        {
                            sheetName = SheetNames[i];
                        }
                        else
                        {
                            sheetName = "Sheet" + sheetCount.ToString();
                        }
                    }
                    else
                    {
                        sheetName = "Sheet" + sheetCount.ToString();
                    }

                    excelDoc.Write("<Worksheet ss:Name=\"" + sheetName + "\">");
                    excelDoc.Write("<Table>");
                    excelDoc.Write("<Row>");
                    for (int x = 0; x < source.Tables[i].Columns.Count; x++)
                    {
                        excelDoc.Write("<Cell ss:StyleID=\"s80\"><Data ss:Type=\"String\">");
                        excelDoc.Write(source.Tables[i].Columns[x].ColumnName);
                        excelDoc.Write("</Data></Cell>");
                    }
                    excelDoc.Write("</Row>");
                    foreach (DataRow x in source.Tables[i].Rows)
                    {
                        rowCount++;
                        //if the number of rows is > 64000 create a new page to continue output
                        if (rowCount == 64000)
                        {
                            rowCount = 0;
                            sheetCount++;
                            excelDoc.Write("</Table>");
                            excelDoc.Write(" </Worksheet>");
                            excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
                            excelDoc.Write("<Table>");
                        }
                        excelDoc.Write("<Row>"); //ID=" + rowCount + "
                        for (int y = 0; y < source.Tables[i].Columns.Count; y++)
                        {
                            System.Type rowType;
                            rowType = x[y].GetType();
                            switch (rowType.ToString())
                            {
                                case "System.String":
                                    string XMLstring = x[y].ToString();
                                    XMLstring = XMLstring.Trim();
                                    XMLstring = XMLstring.Replace("&", "&");
                                    XMLstring = XMLstring.Replace(">", ">");
                                    XMLstring = XMLstring.Replace("<", "<");
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                   "<Data ss:Type=\"String\">");
                                    excelDoc.Write(XMLstring);
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.DateTime":
                                    //Excel has a specific Date Format of YYYY-MM-DD followed by  
                                    //the letter 'T' then hh:mm:sss.lll Example 2005-01-31T24:01:21.000
                                    //The Following Code puts the date stored in XMLDate 
                                    //to the format above
                                    DateTime XMLDate = (DateTime)x[y];
                                    string XMLDatetoString = ""; //Excel Converted Date
                                    XMLDatetoString = XMLDate.Year.ToString() +
                                         "-" +
                                         (XMLDate.Month < 10 ? "0" +
                                         XMLDate.Month.ToString() : XMLDate.Month.ToString()) +
                                         "-" +
                                         (XMLDate.Day < 10 ? "0" +
                                         XMLDate.Day.ToString() : XMLDate.Day.ToString()) +
                                         "T" +
                                         (XMLDate.Hour < 10 ? "0" +
                                         XMLDate.Hour.ToString() : XMLDate.Hour.ToString()) +
                                         ":" +
                                         (XMLDate.Minute < 10 ? "0" +
                                         XMLDate.Minute.ToString() : XMLDate.Minute.ToString()) +
                                         ":" +
                                         (XMLDate.Second < 10 ? "0" +
                                         XMLDate.Second.ToString() : XMLDate.Second.ToString()) +
                                         ".000";
                                    excelDoc.Write("<Cell ss:StyleID=\"DateLiteral\">" +
                                                 "<Data ss:Type=\"DateTime\">");
                                    excelDoc.Write(XMLDatetoString);
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Boolean":
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                "<Data ss:Type=\"String\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Int16":
                                case "System.Int32":
                                case "System.Int64":
                                case "System.Byte":
                                    excelDoc.Write("<Cell ss:StyleID=\"Integer\">" +
                                            "<Data ss:Type=\"Number\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Decimal":
                                case "System.Double":
                                    excelDoc.Write("<Cell ss:StyleID=\"Decimal\">" +
                                          "<Data ss:Type=\"Number\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.DBNull":
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                          "<Data ss:Type=\"String\">");
                                    excelDoc.Write("");
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                default:
                                    throw (new Exception(rowType.ToString() + " not handled."));
                            }
                        }
                        excelDoc.Write("</Row>");
                    }
                    excelDoc.Write("</Table>");
                    excelDoc.Write(" </Worksheet>");
                   


                }
                excelDoc.Write(endExcelXML);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //below code added in finally to avoid error "File being accessed by another user"
                excelDoc.Close();
            }
        }
        public static void exportToExcelMultipleSheetswithcolors(string strBooster, int ProcessID, DataSet source, DataSet master, string fileName, string[] SheetNames = null)
        {

            System.IO.StreamWriter excelDoc;
            excelDoc = new System.IO.StreamWriter(fileName);
            try
            {
                const string startExcelXML = "<xml version>\r\n<Workbook " +
                      "xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n" +
                      " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n " +
                      "xmlns:x=\"urn:schemas-    microsoft-com:office:" +
                      "excel\"\r\n xmlns:ss=\"urn:schemas-microsoft-com:" +
                      "office:spreadsheet\">\r\n <Styles>\r\n " +
                      "<Style ss:ID=\"Default\" ss:Name=\"Normal\">\r\n " +
                      "<Alignment ss:Vertical=\"Bottom\"/>\r\n <Borders/>" +
                      "\r\n <Font/>\r\n <Interior/>\r\n <NumberFormat/>" +
                      "\r\n <Protection/>\r\n </Style>\r\n " +
                      "<Style ss:ID=\"BoldColumn\">\r\n <Font " +
                      "x:Family=\"Swiss\" ss:Bold=\"1\"/>\r\n </Style>\r\n " +
                      "<Style     ss:ID=\"StringLiteral\">\r\n <NumberFormat" +
                      " ss:Format=\"@\"/>\r\n </Style>\r\n <Style " +
                      "ss:ID=\"Decimal\">\r\n <NumberFormat " +
                      "ss:Format=\"0.0000\"/>\r\n </Style>\r\n " +
                      "<Style ss:ID=\"Integer\">\r\n <NumberFormat " +
                      "ss:Format=\"0\"/>\r\n </Style>\r\n <Style " +
                      "ss:ID=\"DateLiteral\">\r\n <NumberFormat " +
                      "ss:Format=\"mm/dd/yyyy;@\"/>\r\n </Style>\r\n " +
                      " <Style ss:ID=\"s80\">\r\n" +
   "<Font ss:FontName=\"Arial\" x:Family=\"Swiss\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>\r\n" +

            "<Interior ss:Color=\"#5B9BD5\" ss:Pattern=\"Solid\"/>\r\n" +

                  "</Style>\r\n" +
                      "<Style ss:ID=\"s81\" >\r\n" +

     "<Font ss:FontName=\"Arial\" x:Family=\"Swiss\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>\r\n" +

       "<Interior ss:Color=\"#5B9BD5\" ss:Pattern=\"Solid\"/>\r\n" +

            "<NumberFormat ss:Format=\"@\"/>\r\n" +

             "</Style>\r\n" +

                                 "</Styles>\r\n ";
                const string endExcelXML = "</Workbook>";
                int sheetCount = 0;
                string sheetName = "";
                excelDoc.Write(startExcelXML);

                for (int i = 0; i < source.Tables.Count - 2; i++)
                {

                    int rowCount = 0;
                    sheetCount++;
                    if (SheetNames != null)
                    {
                        if (SheetNames.Length > i)
                        {
                            sheetName = SheetNames[i];
                        }
                        else
                        {
                            sheetName = "Sheet" + sheetCount.ToString();
                        }
                    }
                    else
                    {
                        sheetName = "Sheet" + sheetCount.ToString();
                    }

                    excelDoc.Write("<Worksheet ss:Name=\"" + sheetName + "\">");
                    excelDoc.Write("<Table>");
                    excelDoc.Write("<Row>");
                    for (int x = 0; x < source.Tables[i].Columns.Count; x++)
                    {
                        excelDoc.Write("<Cell ss:StyleID=\"s80\"><Data ss:Type=\"String\">");
                        
                        string columnName = source.Tables[i].Columns[x].ColumnName;
                       
                        if (columnName.Contains("&lt;=35 Yr Ind. Policy with >=5 Lac SI"))
                        {
                            columnName = columnName.Replace("&lt;=35 Yr Ind. Policy with >=5 Lac SI", strBooster);
                        }
                       
                        excelDoc.Write(columnName);
                        excelDoc.Write("</Data></Cell>");
                    }
                    excelDoc.Write("</Row>");
                    foreach (DataRow x in source.Tables[i].Rows)
                    {
                        rowCount++;
                        //if the number of rows is > 64000 create a new page to continue output
                        if (rowCount == 500000)
                        {
                            rowCount = 0;
                            sheetCount++;
                            excelDoc.Write("</Table>");
                            excelDoc.Write(" </Worksheet>");
                            excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
                            excelDoc.Write("<Table>");
                        }
                        excelDoc.Write("<Row>"); //ID=" + rowCount + "
                       
                        for (int y = 0; y < source.Tables[i].Columns.Count; y++)
                        {
                            System.Type rowType;
                            rowType = x[y].GetType();
                            switch (rowType.ToString())
                            {
                                case "System.String":
                                    string XMLstring = x[y].ToString();
                                    XMLstring = XMLstring.Trim();
                                    XMLstring = XMLstring.Replace("&", "&");
                                    XMLstring = XMLstring.Replace(">", ">");
                                    XMLstring = XMLstring.Replace("<", "<");
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                   "<Data ss:Type=\"String\">");
                                  
                                    excelDoc.Write(XMLstring);
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.DateTime":
                                    //Excel has a specific Date Format of YYYY-MM-DD followed by  
                                    //the letter 'T' then hh:mm:sss.lll Example 2005-01-31T24:01:21.000
                                    //The Following Code puts the date stored in XMLDate 
                                    //to the format above
                                    DateTime XMLDate = (DateTime)x[y];
                                    string XMLDatetoString = ""; //Excel Converted Date
                                    XMLDatetoString = XMLDate.Year.ToString() +
                                         "-" +
                                         (XMLDate.Month < 10 ? "0" +
                                         XMLDate.Month.ToString() : XMLDate.Month.ToString()) +
                                         "-" +
                                         (XMLDate.Day < 10 ? "0" +
                                         XMLDate.Day.ToString() : XMLDate.Day.ToString()) +
                                         "T" +
                                         (XMLDate.Hour < 10 ? "0" +
                                         XMLDate.Hour.ToString() : XMLDate.Hour.ToString()) +
                                         ":" +
                                         (XMLDate.Minute < 10 ? "0" +
                                         XMLDate.Minute.ToString() : XMLDate.Minute.ToString()) +
                                         ":" +
                                         (XMLDate.Second < 10 ? "0" +
                                         XMLDate.Second.ToString() : XMLDate.Second.ToString()) +
                                         ".000";
                                    excelDoc.Write("<Cell ss:StyleID=\"DateLiteral\">" +
                                                 "<Data ss:Type=\"DateTime\">");
                                    excelDoc.Write(XMLDatetoString);
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Boolean":
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                "<Data ss:Type=\"String\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Int16":
                                case "System.Int32":
                                case "System.Int64":
                                case "System.Byte":
                                    excelDoc.Write("<Cell ss:StyleID=\"Integer\">" +
                                            "<Data ss:Type=\"Number\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.Decimal":
                                case "System.Double":
                                    excelDoc.Write("<Cell ss:StyleID=\"Decimal\">" +
                                          "<Data ss:Type=\"Number\">");
                                    excelDoc.Write(x[y].ToString());
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                case "System.DBNull":
                                    excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                          "<Data ss:Type=\"String\">");
                                    excelDoc.Write("");
                                    excelDoc.Write("</Data></Cell>");
                                    break;
                                default:
                                    throw (new Exception(rowType.ToString() + " not handled."));
                            }
                        }
                        excelDoc.Write("</Row>");
                    }
                    excelDoc.Write("</Table>");
                    excelDoc.Write(" </Worksheet>");


                }
                //add single sheet
                excelDoc.Write("<Worksheet ss:Name=\"Masters\">");
                excelDoc.Write("<Table>");
                for (int i = 0; i < master.Tables.Count; i++)
                {

                    int rowCount = 0;
                   
                    
                    if (master.Tables[i].Rows.Count > 0)//Checking Master Table is mty or not
                    {


                        excelDoc.Write("<Row>");
                        for (int x = 0; x < master.Tables[i].Columns.Count; x++)
                        {

                            excelDoc.Write("<Cell ss:StyleID=\"s80\"><Data ss:Type=\"String\">");
                            excelDoc.Write(master.Tables[i].Columns[x].ColumnName);
                            excelDoc.Write("</Data></Cell>");
                        }
                        excelDoc.Write("</Row>");
                        foreach (DataRow x in master.Tables[i].Rows)
                        {
                            rowCount++;
                            //if the number of rows is > 64000 create a new page to continue output
                            if (rowCount == 64000)
                            {
                                rowCount = 0;
                                sheetCount++;
                                excelDoc.Write("</Table>");
                                excelDoc.Write(" </Worksheet>");
                                excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
                                excelDoc.Write("<Table>");
                            }
                            excelDoc.Write("<Row>"); //ID=" + rowCount + "
                            for (int y = 0; y < master.Tables[i].Columns.Count; y++)
                            {
                                System.Type rowType;
                                rowType = x[y].GetType();
                                switch (rowType.ToString())
                                {
                                    case "System.String":
                                        string XMLstring = x[y].ToString();
                                        XMLstring = XMLstring.Trim();
                                        XMLstring = XMLstring.Replace("&", "&");
                                        XMLstring = XMLstring.Replace(">", ">");
                                        XMLstring = XMLstring.Replace("<", "<");
                                        excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                       "<Data ss:Type=\"String\">");

                                        excelDoc.Write(XMLstring);
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    case "System.DateTime":
                                        //Excel has a specific Date Format of YYYY-MM-DD followed by  
                                        //the letter 'T' then hh:mm:sss.lll Example 2005-01-31T24:01:21.000
                                        //The Following Code puts the date stored in XMLDate 
                                        //to the format above
                                        DateTime XMLDate = (DateTime)x[y];
                                        string XMLDatetoString = ""; //Excel Converted Date
                                        XMLDatetoString = XMLDate.Year.ToString() +
                                             "-" +
                                             (XMLDate.Month < 10 ? "0" +
                                             XMLDate.Month.ToString() : XMLDate.Month.ToString()) +
                                             "-" +
                                             (XMLDate.Day < 10 ? "0" +
                                             XMLDate.Day.ToString() : XMLDate.Day.ToString()) +
                                             "T" +
                                             (XMLDate.Hour < 10 ? "0" +
                                             XMLDate.Hour.ToString() : XMLDate.Hour.ToString()) +
                                             ":" +
                                             (XMLDate.Minute < 10 ? "0" +
                                             XMLDate.Minute.ToString() : XMLDate.Minute.ToString()) +
                                             ":" +
                                             (XMLDate.Second < 10 ? "0" +
                                             XMLDate.Second.ToString() : XMLDate.Second.ToString()) +
                                             ".000";
                                        excelDoc.Write("<Cell ss:StyleID=\"DateLiteral\">" +
                                                     "<Data ss:Type=\"DateTime\">");
                                        excelDoc.Write(XMLDatetoString);
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    case "System.Boolean":
                                        excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                                    "<Data ss:Type=\"String\">");
                                        excelDoc.Write(x[y].ToString());
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    case "System.Int16":
                                    case "System.Int32":
                                    case "System.Int64":
                                    case "System.Byte":
                                        excelDoc.Write("<Cell ss:StyleID=\"Integer\">" +
                                                "<Data ss:Type=\"Number\">");
                                        excelDoc.Write(x[y].ToString());
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    case "System.Decimal":
                                    case "System.Double":
                                        excelDoc.Write("<Cell ss:StyleID=\"Decimal\">" +
                                              "<Data ss:Type=\"Number\">");
                                        excelDoc.Write(x[y].ToString());
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    case "System.DBNull":
                                        excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" +
                                              "<Data ss:Type=\"String\">");
                                        excelDoc.Write("");
                                        excelDoc.Write("</Data></Cell>");
                                        break;
                                    default:
                                        throw (new Exception(rowType.ToString() + " not handled."));
                                }
                            }
                            excelDoc.Write("</Row>");

                        }

                        excelDoc.Write("<Row>");
                        excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\"></Cell>");
                        excelDoc.Write("</Row>");
                    }
                }
                excelDoc.Write("</Table>");
                excelDoc.Write(" </Worksheet>");


                excelDoc.Write(endExcelXML);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //below code added in finally to avoid error "File being accessed by another user"
                excelDoc.Close();
            }
        }
    }
}
