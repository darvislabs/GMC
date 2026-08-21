using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.BL.GMC
{
    public class DashboardBL : IDashboardBL
    {
        private readonly IRolloverUploadRepo _repo;
        public DashboardBL(IRolloverUploadRepo repo) => _repo = repo;

        public async Task<DashboardViewModel> BuildAsync(string role, string userName, int timeSeriesDays = 30)
        {
            var countsTask = _repo.GetDashboardCountsAsync(role, userName);
            var seriesTask = _repo.GetDashboardTimeSeriesAsync(role, userName, timeSeriesDays);
            var recentTask = _repo.GetPendingUploadsAsync(role, userName);

            await Task.WhenAll(countsTask, seriesTask, recentTask);

            return new DashboardViewModel
            {
                Role          = role,
                UserName      = userName,
                Counts        = countsTask.Result,
                TimeSeries    = seriesTask.Result,
                RecentUploads = recentTask.Result.Take(10).ToList()
            };
        }
    }
}
