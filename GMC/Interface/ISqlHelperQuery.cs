using System.Data;

namespace GMC.Interface
{
    public interface ISqlHelperQuery
    {
        public DataSet ExecuteDataset(string connectionString,string storedProcedureName, params object[] parameters);



        public object ExecuteScalar(string connectionString, string storedProcedureName, params object[] parameters);


        public int ExecuteNonQuery(string connectionString, string storedProcedureName, params object[] parameters);


        public object ExecuteReader(string connectionString, string storedProcedureName, params object[] parameters);

        public int ExecuteSQLQuery(string connectionString, string SQLQuery);
        public DataSet GetDataset(string connectionString, string SQLQuery);
        public object GetExecuteScalar(string connectionString, string SQLQuery);
        public List<string> GetExecuteReader(string connectionString, string SQLQuery,string columnName);
        public DataTable GetDataTable(string connectionString, string SQLQuery);

    }
}
