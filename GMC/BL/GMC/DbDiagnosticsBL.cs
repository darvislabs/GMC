using GMC.Interface.GMC;

namespace GMC.BL.GMC
{
    public interface IDbDiagnosticsBL
    {
        Task<DbDiagnosticsViewModel> BuildAsync(string? schema = "dbo");
    }

    public sealed class DbDiagnosticsBL : IDbDiagnosticsBL
    {
        private readonly IDbDiagnosticsRepo _repo;
        private readonly ILogger<DbDiagnosticsBL> _log;

        public DbDiagnosticsBL(IDbDiagnosticsRepo repo, ILogger<DbDiagnosticsBL> log)
        {
            _repo = repo;
            _log = log;
        }

        public async Task<DbDiagnosticsViewModel> BuildAsync(string? schema = "dbo")
        {
            try
            {
                var tables = await _repo.GetTablesAsync(schema);
                var procs = await _repo.GetStoredProceduresAsync(schema);
                return new DbDiagnosticsViewModel
                {
                    Schema = schema ?? "dbo",
                    Tables = tables,
                    Procedures = procs
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "DB diagnostics failed.");
                return new DbDiagnosticsViewModel
                {
                    Schema = schema ?? "dbo",
                    Error = ex.Message
                };
            }
        }
    }

    public sealed class DbDiagnosticsViewModel
    {
        public string Schema { get; set; } = "dbo";
        public string? Error { get; set; }
        public List<DbTableInfo> Tables { get; set; } = new();
        public List<DbProcInfo> Procedures { get; set; } = new();
    }
}

