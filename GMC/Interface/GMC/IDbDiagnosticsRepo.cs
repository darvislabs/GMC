using System.Data;

namespace GMC.Interface.GMC
{
    public interface IDbDiagnosticsRepo
    {
        Task<List<DbTableInfo>> GetTablesAsync(string? schema = "dbo");
        Task<List<DbProcInfo>> GetStoredProceduresAsync(string? schema = "dbo");
        Task<List<DbProcParamInfo>> GetStoredProcedureParamsAsync(string schema, string procName);
    }

    public sealed class DbTableInfo
    {
        public string SchemaName { get; set; } = "dbo";
        public string TableName { get; set; } = string.Empty;
        public List<DbColumnInfo> Columns { get; set; } = new();
    }

    public sealed class DbColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public byte? Precision { get; set; }
        public int? Scale { get; set; }
        public bool IsNullable { get; set; }
    }

    public sealed class DbProcInfo
    {
        public string SchemaName { get; set; } = "dbo";
        public string ProcName { get; set; } = string.Empty;
    }

    public sealed class DbProcParamInfo
    {
        public string ParamName { get; set; } = string.Empty;
        public string ParamType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public byte? Precision { get; set; }
        public int? Scale { get; set; }
        public bool IsOutput { get; set; }
    }
}

