using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    public interface IDashboardBL
    {
        Task<DashboardViewModel> BuildAsync(string role, string userName, int timeSeriesDays = 30);
    }
}
