using System.Data;
using System.Data.SqlClient;
using GMC.Interface.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class DbDiagnosticsRepo : IDbDiagnosticsRepo
    {
        private readonly string _conn;

        public DbDiagnosticsRepo(IConfiguration config)
        {
            _conn = config["ConnectionStrings:ConnectionToTele_Dashboard"]
                    ?? throw new InvalidOperationException("Connection string 'ConnectionToTele_Dashboard' is missing.");
        }

        public async Task<List<DbTableInfo>> GetTablesAsync(string? schema = "dbo")
        {
            var tables = new List<DbTableInfo>();
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();

            const string tableSql = @"
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND (@Schema IS NULL OR TABLE_SCHEMA = @Schema)
ORDER BY TABLE_SCHEMA, TABLE_NAME;";

            using (var cmd = new SqlCommand(tableSql, c))
            {
                cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    tables.Add(new DbTableInfo
                    {
                        SchemaName = rdr.GetString(0),
                        TableName = rdr.GetString(1)
                    });
                }
            }

            const string colSql = @"
SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE,
       CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (@Schema IS NULL OR TABLE_SCHEMA = @Schema)
ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;";

            var cols = new Dictionary<(string schemaName, string tableName), List<DbColumnInfo>>();
            using (var cmd = new SqlCommand(colSql, c))
            {
                cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    var s = rdr.GetString(0);
                    var t = rdr.GetString(1);
                    var key = (s, t);
                    if (!cols.TryGetValue(key, out var list))
                    {
                        list = new List<DbColumnInfo>();
                        cols[key] = list;
                    }

                    list.Add(new DbColumnInfo
                    {
                        ColumnName = rdr.GetString(2),
                        DataType = rdr.GetString(3),
                        MaxLength = rdr.IsDBNull(4) ? null : rdr.GetInt32(4),
                        Precision = rdr.IsDBNull(5) ? null : rdr.GetByte(5),
                        Scale = rdr.IsDBNull(6) ? null : rdr.GetInt32(6),
                        IsNullable = !rdr.IsDBNull(7) && string.Equals(rdr.GetString(7), "YES", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            foreach (var t in tables)
            {
                if (cols.TryGetValue((t.SchemaName, t.TableName), out var list))
                    t.Columns = list;
            }
            return tables;
        }

        public async Task<List<DbProcInfo>> GetStoredProceduresAsync(string? schema = "dbo")
        {
            var list = new List<DbProcInfo>();
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();

            const string sql = @"
SELECT ROUTINE_SCHEMA, ROUTINE_NAME
FROM INFORMATION_SCHEMA.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE'
  AND (@Schema IS NULL OR ROUTINE_SCHEMA = @Schema)
ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME;";

            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new DbProcInfo
                {
                    SchemaName = rdr.GetString(0),
                    ProcName = rdr.GetString(1)
                });
            }
            return list;
        }

        public async Task<List<DbProcParamInfo>> GetStoredProcedureParamsAsync(string schema, string procName)
        {
            var list = new List<DbProcParamInfo>();
            using var c = new SqlConnection(_conn);
            await c.OpenAsync();

            const string sql = @"
SELECT PARAMETER_NAME, DATA_TYPE,
       CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, PARAMETER_MODE
FROM INFORMATION_SCHEMA.PARAMETERS
WHERE SPECIFIC_SCHEMA = @Schema
  AND SPECIFIC_NAME = @Proc
ORDER BY ORDINAL_POSITION;";

            using var cmd = new SqlCommand(sql, c);
            cmd.Parameters.AddWithValue("@Schema", schema);
            cmd.Parameters.AddWithValue("@Proc", procName);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var mode = rdr.IsDBNull(5) ? "" : rdr.GetString(5);
                list.Add(new DbProcParamInfo
                {
                    ParamName = rdr.IsDBNull(0) ? "" : rdr.GetString(0),
                    ParamType = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                    MaxLength = rdr.IsDBNull(2) ? null : rdr.GetInt32(2),
                    Precision = rdr.IsDBNull(3) ? null : rdr.GetByte(3),
                    Scale = rdr.IsDBNull(4) ? null : rdr.GetInt32(4),
                    IsOutput = mode.IndexOf("OUT", StringComparison.OrdinalIgnoreCase) >= 0
                });
            }
            return list;
        }
    }
}

