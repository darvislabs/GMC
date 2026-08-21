using GMC.Interface.GMC;

namespace GMC.BL.GMC
{
    public interface IMasterListBL
    {
        Task<List<string>> GetCompaniesAsync();
        Task<List<string>> GetTpasAsync();
        Task<List<string>> GetIndustriesAsync();
        Task<List<string>> GetFinancialYearsAsync();

        Task<bool> AddCompanyAsync(string name);
        Task<bool> UpdateCompanyAsync(string oldName, string newName);
        Task<bool> DeleteCompanyAsync(string name);

        Task<bool> AddTpaAsync(string name);
        Task<bool> UpdateTpaAsync(string oldName, string newName);
        Task<bool> DeleteTpaAsync(string name);

        Task<bool> AddIndustryAsync(string name);
        Task<bool> UpdateIndustryAsync(string oldName, string newName);
        Task<bool> DeleteIndustryAsync(string name);

        Task<bool> AddFinancialYearAsync(string value);
        Task<bool> UpdateFinancialYearAsync(string oldValue, string newValue);
        Task<bool> DeleteFinancialYearAsync(string value);
    }

    public class MasterListBL : IMasterListBL
    {
        private readonly IMasterListRepo _repo;
        public MasterListBL(IMasterListRepo repo) => _repo = repo;

        public Task<List<string>> GetCompaniesAsync() => _repo.GetCompaniesAsync();
        public Task<List<string>> GetTpasAsync() => _repo.GetTpasAsync();
        public Task<List<string>> GetIndustriesAsync() => _repo.GetIndustriesAsync();
        public Task<List<string>> GetFinancialYearsAsync() => _repo.GetFinancialYearsAsync();

        public Task<bool> AddCompanyAsync(string name) => _repo.AddCompanyAsync(name);
        public Task<bool> UpdateCompanyAsync(string oldName, string newName) => _repo.UpdateCompanyAsync(oldName, newName);
        public Task<bool> DeleteCompanyAsync(string name) => _repo.DeleteCompanyAsync(name);

        public Task<bool> AddTpaAsync(string name) => _repo.AddTpaAsync(name);
        public Task<bool> UpdateTpaAsync(string oldName, string newName) => _repo.UpdateTpaAsync(oldName, newName);
        public Task<bool> DeleteTpaAsync(string name) => _repo.DeleteTpaAsync(name);

        public Task<bool> AddIndustryAsync(string name) => _repo.AddIndustryAsync(name);
        public Task<bool> UpdateIndustryAsync(string oldName, string newName) => _repo.UpdateIndustryAsync(oldName, newName);
        public Task<bool> DeleteIndustryAsync(string name) => _repo.DeleteIndustryAsync(name);

        public Task<bool> AddFinancialYearAsync(string value) => _repo.AddFinancialYearAsync(value);
        public Task<bool> UpdateFinancialYearAsync(string oldValue, string newValue)
            => _repo.UpdateFinancialYearAsync(oldValue, newValue);
        public Task<bool> DeleteFinancialYearAsync(string value) => _repo.DeleteFinancialYearAsync(value);
    }
}

