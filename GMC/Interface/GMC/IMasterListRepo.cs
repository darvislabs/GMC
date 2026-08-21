namespace GMC.Interface.GMC
{
    public interface IMasterListRepo
    {
        Task<List<string>> GetCompaniesAsync(int top = 5000);
        Task<List<string>> GetTpasAsync(int top = 5000);
        Task<List<string>> GetIndustriesAsync(int top = 5000);
        Task<List<string>> GetFinancialYearsAsync(int top = 100);

        Task<bool> AddCompanyAsync(string companyName);
        Task<bool> UpdateCompanyAsync(string oldName, string newName);
        Task<bool> DeleteCompanyAsync(string companyName);

        Task<bool> AddTpaAsync(string tpaName);
        Task<bool> UpdateTpaAsync(string oldName, string newName);
        Task<bool> DeleteTpaAsync(string tpaName);

        Task<bool> AddIndustryAsync(string industryName);
        Task<bool> UpdateIndustryAsync(string oldName, string newName);
        Task<bool> DeleteIndustryAsync(string industryName);

        Task<bool> AddFinancialYearAsync(string financialYear);
        Task<bool> UpdateFinancialYearAsync(string oldValue, string newValue);
        Task<bool> DeleteFinancialYearAsync(string financialYear);
    }
}

