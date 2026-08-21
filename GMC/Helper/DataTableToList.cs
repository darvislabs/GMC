using System.Data;
using System.Reflection;

namespace GMC.Helper
{
    public class DataTableToList
    {
        public static List<T> ConvertDataTable<T>(DataTable table)
        {
            try
            {
                List<T> objList = new List<T>();
                foreach (DataRow dr in table.Rows)
                {
                    T CopyObj = (T)Activator.CreateInstance(typeof(T));
                    PropertyInfo[] pinfo = typeof(T).GetProperties();
                    foreach (DataColumn dc in table.Columns)
                    {
                        PropertyInfo p = pinfo.FirstOrDefault(x => x.Name.ToUpper() == dc.ColumnName.ToUpper());
                        if (p != null)
                        {
                            Type Conversion = p.PropertyType;
                            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                Conversion = p.PropertyType.GetGenericArguments()[0];

                            }
                            if (dr[p.Name] != null)
                            {
                                if (!String.IsNullOrEmpty(dr[p.Name].ToString()))
                                {
                                    if (dc.ColumnName == "Exclude_Branch_Code")
                                    {
                                        List<string> branch = new List<string>();
                                        if (dr[p.Name].ToString().Contains(","))
                                        {
                                            branch = dr[p.Name].ToString().Split(',').ToList();
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }
                                        else
                                        {
                                            branch.Add(dr[p.Name].ToString());
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }

                                    }
                                    else if (dc.ColumnName == "Branch_Code")
                                    {
                                        List<string> branch = new List<string>();
                                        if (dr[p.Name].ToString().Contains(","))
                                        {
                                            branch = dr[p.Name].ToString().Split(',').ToList();
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }
                                        else
                                        {
                                            branch.Add(dr[p.Name].ToString());
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }

                                    }
                                    else if (dc.ColumnName == "Include_Branch_Code")
                                    {
                                        List<string> branch = new List<string>();
                                        if (dr[p.Name].ToString().Contains(","))
                                        {
                                            branch = dr[p.Name].ToString().Split(',').ToList();
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }
                                        else
                                        {
                                            branch.Add(dr[p.Name].ToString());
                                            p.SetValue(CopyObj, Convert.ChangeType(branch, Conversion), null);
                                        }

                                    }

                                    else if (dc.ColumnName == "Region_Code")
                                    {
                                        List<string> region = new List<string>();
                                        if (dr[p.Name].ToString().Contains(","))
                                        {
                                            region = dr[p.Name].ToString().Split(',').ToList();
                                            p.SetValue(CopyObj, Convert.ChangeType(region, Conversion), null);
                                        }
                                        else
                                        {
                                            region.Add(dr[p.Name].ToString());
                                            p.SetValue(CopyObj, Convert.ChangeType(region, Conversion), null);

                                        }
                                    }
                                    else if (dc.ColumnName == "Product_Code")
                                    {
                                        List<string> product = new List<string>();
                                        if (dr[p.Name].ToString().Contains(","))
                                        {
                                            product = dr[p.Name].ToString().Split(',').ToList();
                                            p.SetValue(CopyObj, Convert.ChangeType(product, Conversion), null);
                                        }
                                        else
                                        {
                                            product.Add(dr[p.Name].ToString());
                                            p.SetValue(CopyObj, Convert.ChangeType(product, Conversion), null);
                                        }

                                    }
                                    else
                                    {
                                        p.SetValue(CopyObj, Convert.ChangeType(dr[p.Name], Conversion), null);
                                    }

                                }

                            }

                        }

                    }
                    objList.Add(CopyObj);

                }

                return objList;
            }
            catch (Exception ee)
            {

                throw;
            }
            

          
        }
        public static List<T> ConvertDataTableToListForCommon<T>(DataTable table)
        {
            try
            {
                List<T> objList = new List<T>();
                foreach (DataRow dr in table.Rows)
                {
                    T CopyObj = (T)Activator.CreateInstance(typeof(T));
                    PropertyInfo[] pinfo = typeof(T).GetProperties();
                    foreach (DataColumn dc in table.Columns)
                    {
                        PropertyInfo p = pinfo.FirstOrDefault(x => x.Name.ToUpper() == dc.ColumnName.ToUpper());
                        if (p != null)
                        {
                            Type Conversion = p.PropertyType;
                            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                Conversion = p.PropertyType.GetGenericArguments()[0];

                            }
                            if (dr[p.Name] != null)
                            {


                                p.SetValue(CopyObj, Convert.ChangeType(dr[p.Name], Conversion), null);


                            }

                        }

                    }
                    objList.Add(CopyObj);

                }

                return objList;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public static List<T> ConvertDataTableToListForCommonAll<T>(DataTable dt) where T : class, new()
        {
            List<T> lstItems = new List<T>();
            if (dt != null && dt.Rows.Count > 0)
                foreach (DataRow row in dt.Rows)
                    lstItems.Add(ConvertDataRowToGenericType<T>(row));
            else
                lstItems = null;
            return lstItems;
        }
        private static T ConvertDataRowToGenericType<T>(DataRow row) where T : class, new()
        {
            Type entityType = typeof(T);
            T objEntity = new T();
            foreach (DataColumn column in row.Table.Columns)
            {
                object value = row[column.ColumnName];
                if (value == DBNull.Value) value = null;
                PropertyInfo property = entityType.GetProperty(column.ColumnName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                try
                {
                    if (property != null && property.CanWrite)
                        property.SetValue(objEntity, value, null);

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return objEntity;
        }
        //public static List<T> ConvertDataTable<T>(DataTable dt)
        //{
        //    List<T> data = new List<T>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        T item = GetItem<T>(row);
        //        data.Add(item);
        //    }
        //    return data;
        //}
        public static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }
    }
}
