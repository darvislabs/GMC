using System.Data;

namespace GMC.Models.GMC
{
    /// <summary> View model for /GMCCalculatorDetails/TrendAnalysis. </summary>
    public class TrendAnalysisModel
    {
        public string? PolicyNo { get; set; }

        /// <summary>Selected financial year filter (e.g. "2024-25"); null = all years.</summary>
        public string? FYYear { get; set; }

        /// <summary>One row per UW year (rendered transposed).</summary>
        public DataTable dtTrend { get; set; } = new();

        /// <summary>Insurer / TPA / Broker per UW year.</summary>
        public DataTable dtParties { get; set; } = new();

        /// <summary>Relationship-wise claims per UW year.</summary>
        public DataTable dtRelationship { get; set; } = new();

        /// <summary>Disease-category claims per UW year.</summary>
        public DataTable dtDisease { get; set; } = new();

        /// <summary>Relationship-wise lives (Female/Male/Total/%Mix) from the enrollment table.</summary>
        public DataTable dtEnrollLives { get; set; } = new();

        /// <summary>UW years available for the selected policy (FY filter options).</summary>
        public List<string> AvailableYears { get; set; } = new();

        /// <summary>Policy numbers that have uploaded data (dropdown options).</summary>
        public List<string> AvailablePolicies { get; set; } = new();

        public bool HasData => dtTrend.Rows.Count > 0 || dtEnrollLives.Rows.Count > 0;
    }
}
