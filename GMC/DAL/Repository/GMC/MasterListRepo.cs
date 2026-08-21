using System.Data;
using System.Data.SqlClient;
using GMC.Interface.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class MasterListRepo : IMasterListRepo
    {
        private readonly string _conn;
        private readonly ILogger<MasterListRepo> _log;

        public MasterListRepo(IConfiguration config, ILogger<MasterListRepo> log)
        {
            _conn = config["ConnectionStrings:ConnectionToTele_Dashboard"]
                    ?? throw new InvalidOperationException("Connection string 'ConnectionToTele_Dashboard' is missing.");
            _log = log;
        }

        public Task<List<string>> GetCompaniesAsync(int top = 5000)
            => GetListAsync("tbl_company_list", "company_name", top);

        public Task<List<string>> GetTpasAsync(int top = 5000)
            => GetListAsync("tbl_TPA_list", "TPA_Name", top);

        public Task<List<string>> GetIndustriesAsync(int top = 5000)
            => GetListAsync("tbl_GMC_industry_master", "Nature of Industry", top);

        public Task<List<string>> GetFinancialYearsAsync(int top = 100)
            => GetListAsync("tbl_GMC_FinancialYear_Master", "FinancialYear", top);

        private async Task<List<string>> GetListAsync(string table, string col, int top)
        {
            top = Math.Clamp(top, 1, 5000);
            var list = new List<string>();
            var sql = $@"
SELECT TOP ({top}) [{col}]
FROM dbo.[{table}] WITH (NOLOCK)
WHERE [{col}] IS NOT NULL AND LTRIM(RTRIM([{col}])) <> ''
ORDER BY [{col}];";

            using var c = new SqlConnection(_conn);
            using var cmd = new SqlCommand(sql, c) { CommandType = CommandType.Text, CommandTimeout = 60 };
            await c.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var v = rdr[0]?.ToString();
                if (!string.IsNullOrWhiteSpace(v)) list.Add(v.Trim());
            }
            return list;
        }

        public Task<bool> AddCompanyAsync(string companyName)
            => AddValueAsync("tbl_company_list", "company_name", companyName);

        public Task<bool> UpdateCompanyAsync(string oldName, string newName)
            => UpdateValueAsync("tbl_company_list", "company_name", oldName, newName);

        public Task<bool> DeleteCompanyAsync(string companyName)
            => DeleteValueAsync("tbl_company_list", "company_name", companyName);

        public Task<bool> AddTpaAsync(string tpaName)
            => AddValueAsync("tbl_TPA_list", "TPA_Name", tpaName);

        public Task<bool> UpdateTpaAsync(string oldName, string newName)
            => UpdateValueAsync("tbl_TPA_list", "TPA_Name", oldName, newName);

        public Task<bool> DeleteTpaAsync(string tpaName)
            => DeleteValueAsync("tbl_TPA_list", "TPA_Name", tpaName);

        public Task<bool> AddIndustryAsync(string industryName)
            => AddValueAsync("tbl_GMC_industry_master", "Nature of Industry", industryName);

        public Task<bool> UpdateIndustryAsync(string oldName, string newName)
            => UpdateValueAsync("tbl_GMC_industry_master", "Nature of Industry", oldName, newName);

        public Task<bool> DeleteIndustryAsync(string industryName)
            => DeleteValueAsync("tbl_GMC_industry_master", "Nature of Industry", industryName);

        public Task<bool> AddFinancialYearAsync(string financialYear)
            => AddValueAsync("tbl_GMC_FinancialYear_Master", "FinancialYear", financialYear);

        public Task<bool> UpdateFinancialYearAsync(string oldValue, string newValue)
            => UpdateValueAsync("tbl_GMC_FinancialYear_Master", "FinancialYear", oldValue, newValue);

        public Task<bool> DeleteFinancialYearAsync(string financialYear)
            => DeleteValueAsync("tbl_GMC_FinancialYear_Master", "FinancialYear", financialYear);

        private async Task<bool> AddValueAsync(string table, string col, string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length == 0) return false;

            var sql = $@"
IF NOT EXISTS (SELECT 1 FROM dbo.[{table}] WITH (NOLOCK) WHERE [{col}] = @v)
BEGIN
    INSERT INTO dbo.[{table}] ([{col}]) VALUES (@v);
END";

            try
            {
                using var c = new SqlConnection(_conn);
                using var cmd = new SqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@v", value);
                await c.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "AddValueAsync failed for {Table}.{Col}", table, col);
                return false;
            }
        }

        private async Task<bool> UpdateValueAsync(string table, string col, string oldValue, string newValue)
        {
            oldValue = (oldValue ?? string.Empty).Trim();
            newValue = (newValue ?? string.Empty).Trim();
            if (oldValue.Length == 0 || newValue.Length == 0) return false;

            var sql = $@"
UPDATE dbo.[{table}]
SET    [{col}] = @New
WHERE  [{col}] = @Old;";

            try
            {
                using var c = new SqlConnection(_conn);
                using var cmd = new SqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@Old", oldValue);
                cmd.Parameters.AddWithValue("@New", newValue);
                await c.OpenAsync();
                var n = await cmd.ExecuteNonQueryAsync();
                return n > 0;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "UpdateValueAsync failed for {Table}.{Col}", table, col);
                return false;
            }
        }

        private async Task<bool> DeleteValueAsync(string table, string col, string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length == 0) return false;

            var sql = $@"DELETE FROM dbo.[{table}] WHERE [{col}] = @v;";
            try
            {
                using var c = new SqlConnection(_conn);
                using var cmd = new SqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@v", value);
                await c.OpenAsync();
                var n = await cmd.ExecuteNonQueryAsync();
                return n > 0;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "DeleteValueAsync failed for {Table}.{Col}", table, col);
                return false;
            }
        }
    }
}

