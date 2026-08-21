using System.ComponentModel.DataAnnotations;

namespace GMC.Models.GMC
{
    /// <summary>
    /// Status values that a rollover upload moves through.  Keep the strings
    /// in lock-step with the values used by the SQL procedures (no enum on the
    /// DB side intentionally — leaves room to add new statuses without a
    /// schema migration).
    /// </summary>
    public static class UploadStatus
    {
        public const string Pending          = "Pending";
        public const string MappingRequired  = "MappingRequired";
        public const string UnderReview      = "UnderReview";
        public const string Mapped           = "Mapped";
        public const string Completed        = "Completed";
        public const string Rejected         = "Rejected";
    }

    /// <summary> View-model for the Sales-person upload form. </summary>
    public class SalesUploadForm
    {
        [Required, RegularExpression(@"^\S.*?\S$|^\S$",
            ErrorMessage = "Policy No cannot have leading or trailing whitespace.")]
        [Display(Name = "Policy Number")]
        public string PolicyNo { get; set; } = string.Empty;

        [Display(Name = "Policy Name")]
        public string? PolicyName { get; set; }

        [Display(Name = "Insurance Company")]
        public string? InsuranceCompany { get; set; }

        public string? TPA { get; set; }

        [Display(Name = "Nature of Industry")]
        public string? IndustryName { get; set; }

        [Display(Name = "Sub Type")]
        public string? SubType { get; set; }     // Main / Parent / Topup

        [Display(Name = "Data Category")]
        public string DataCategory { get; set; } = "Enrollment";  // legacy single-file field

        /// <summary>Legacy single-file slot.  Prefer EnrollmentFile / ClaimFile below.</summary>
        public IFormFile? UploadFile { get; set; }

        [Display(Name = "Enrollment file (.xlsx)")]
        public IFormFile? EnrollmentFile { get; set; }

        [Display(Name = "Claim file (.xlsx)")]
        public IFormFile? ClaimFile { get; set; }

        public List<string>? IndustryOptions { get; set; }
        public List<string>? CompanyOptions  { get; set; }
        public List<string>? TpaOptions      { get; set; }
    }

    /// <summary> Persisted upload header — mirrors dbo.tbl_GMC_RolloverUpload. </summary>
    public class RolloverUpload
    {
        public int UploadId { get; set; }
        public string PolicyNo { get; set; } = string.Empty;
        public string? PolicyName { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? TPA { get; set; }
        public string? IndustryName { get; set; }
        public string? SubType { get; set; }
        public string DataCategory { get; set; } = "Enrollment";
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? TotalRows { get; set; }
        public int? TotalColumns { get; set; }
        public string Status { get; set; } = UploadStatus.Pending;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
        public string? AssignedUnderwriter { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedOn { get; set; }
        public decimal? MappingConfidenceAvg { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary> Shaped response from sales upload submit. </summary>
    public class SalesUploadResponse
    {
        public bool   Success     { get; set; }
        public int    UploadId    { get; set; }
        public string Message     { get; set; } = string.Empty;
        public string Status      { get; set; } = string.Empty;
        public string? RedirectTo { get; set; }
    }
}
