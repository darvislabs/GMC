namespace GMC.Models.GMC
{
    /// <summary> Single row of status-bucket counts (matches usp_GetDashboardCounts). </summary>
    public class DashboardCounts
    {
        public int Pending         { get; set; }
        public int MappingRequired { get; set; }
        public int UnderReview     { get; set; }
        public int Mapped          { get; set; }
        public int Completed       { get; set; }
        public int Rejected        { get; set; }
        public int Total           { get; set; }
        public int Today           { get; set; }
        public int Last7Days       { get; set; }
    }

    /// <summary> One daily point of the time-series chart. </summary>
    public class DashboardTimePoint
    {
        public DateTime Day         { get; set; }
        public int      InProgress  { get; set; }
        public int      Completed   { get; set; }
        public int      Total       { get; set; }
    }

    /// <summary> Composite VM for the Dashboard view. </summary>
    public class DashboardViewModel
    {
        public DashboardCounts         Counts      { get; set; } = new();
        public List<DashboardTimePoint> TimeSeries { get; set; } = new();
        public List<RolloverUpload>    RecentUploads { get; set; } = new();
        public string                  Role        { get; set; } = string.Empty;
        public string                  UserName    { get; set; } = string.Empty;
    }
}
