using GMC.Interface;
using Microsoft.ApplicationBlocks.Data;
using System.Data;
using System.Data.SqlClient;

namespace GMC.DAL
{
	public class SqlHelperQuery: ISqlHelperQuery
    {
        
        
        
        public SqlHelperQuery()
        {
            

        }
        SqlConnection con = new SqlConnection();
        public DataSet ExecuteDataset(string connectionString, string storedProcedureName, params object[] parameters)
        {
            return SqlHelper.ExecuteDataset(connectionString, storedProcedureName, parameters);
        }

        public object ExecuteScalar(string connectionString, string storedProcedureName, params object[] parameters)
        {
            //return SqlHelper.ExecuteScalar(connectionString, storedProcedureName,CommandType.StoredProcedure, parameters);
            return SqlHelper.ExecuteScalar(connectionString, storedProcedureName, parameters);
        }

        public  int ExecuteNonQuery(string connectionString, string storedProcedureName, params object[] parameters)
        {
            //return SqlHelper.ExecuteNonQuery(connectionString, storedProcedureName,
            //    CommandType.StoredProcedure, parameters);
            return SqlHelper.ExecuteNonQuery(connectionString, storedProcedureName, parameters);
        }

        public  object ExecuteReader(string connectionString, string storedProcedureName, params object[] parameters)
        {
            //return SqlHelper.ExecuteReader(connectionString, storedProcedureName,
            //    CommandType.StoredProcedure, parameters);
            return SqlHelper.ExecuteReader(connectionString, storedProcedureName, parameters);

        }
        public int ExecuteSQLQuery(string connectionString, string SQLQuery)
        {
            try
            {


            SqlConnection con = new SqlConnection(connectionString);
            if (con.State==ConnectionState.Closed)
            {
                con.ConnectionString = connectionString;
                con.Open();
            }
            

            SqlCommand cmd = new SqlCommand(SQLQuery, con);
            cmd.CommandTimeout = 0;
            int a= cmd.ExecuteNonQuery();

            con.Close();
            return a;

            }
            catch (Exception)
            {
                con.Close();
                con.Dispose();

                throw;
            }

        }

        public DataSet GetDataset(string connectionString, string SQLQuery)
        {
            try
            {
                SqlConnection con = new SqlConnection(connectionString);   
                if (con.State == ConnectionState.Closed)
                {
                    con.ConnectionString = connectionString;
                    con.Open();
                }

                SqlCommand cmd = new SqlCommand(SQLQuery, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(ds);
                con.Close();
                return ds;
            }
            catch (Exception ee)
            {
                con.Close();

                throw;
            }
            finally
            {
                con.Dispose();
                
            }
            
        }

        public object GetExecuteScalar(string connectionString, string SQLQuery)
        {
            try
            {
                SqlConnection con = new SqlConnection(connectionString);  
                if (con.State == ConnectionState.Closed)
                {
                    con.ConnectionString = connectionString;
                    con.Open();
                }

                SqlCommand cmd = new SqlCommand(SQLQuery, con);
                object obj= cmd.ExecuteScalar();
                return obj;
            }
            catch (Exception ee)
            {
                con.Close();
                throw;
            }
        }

        public DataTable GetDataTable(string connectionString, string SQLQuery)
        {
            try
            {
                SqlConnection con = new SqlConnection(connectionString);
                if (con.State == ConnectionState.Closed)
                {
                    con.ConnectionString = connectionString;
                    con.Open();
                }

                SqlCommand cmd = new SqlCommand(SQLQuery, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.SelectCommand.CommandTimeout = 300;
                da.Fill(ds);
                con.Close();
                return ds.Tables[0];
            }
            catch (Exception ee)
            {
                con.Close();
                throw;
            }
        }

        public List<string> GetExecuteReader(string connectionString, string SQLQuery,string columnName)
        {
            try
            {
                SqlConnection con = new SqlConnection(connectionString);
                List<string> strList = new List<string>();
                if (con.State == ConnectionState.Closed)
                {
                    con.ConnectionString = connectionString;
                    con.Open();
                }
                

                SqlCommand cmd = new SqlCommand(SQLQuery, con);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        strList.Add(sdr[columnName].ToString());
                    }
                }
                con.Close();
                return strList;
            }
            catch (Exception ee)
            {
                con.Close();
                throw;
            }
        }
    }
}
