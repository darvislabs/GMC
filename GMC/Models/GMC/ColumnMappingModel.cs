using System.ComponentModel.DataAnnotations;

namespace GMC.Models.GMC
{
    /// <summary> Mirrors dbo.tbl_GMC_StandardColumns. </summary>
    public class StandardColumn
    {
        public int    ColumnId     { get; set; }
        public string ColumnName   { get; set; } = string.Empty;
        public string DataType     { get; set; } = "string";
        public string DataCategory { get; set; } = "Both";   // Enrollment / Claim / Both
        public bool   IsRequired   { get; set; }
        public string? Description { get; set; }
        public int?   DisplayOrder { get; set; }
    }

    /// <summary> One row of the proposed / saved mapping. </summary>
    public class ColumnMapping
    {
        public int     MappingId      { get; set; }
        public int     UploadId       { get; set; }
        public string  SourceColumn   { get; set; } = string.Empty;
        public string? TargetColumn   { get; set; }
        public decimal ConfidencePct  { get; set; }
        public bool    IsManual       { get; set; }
        public bool    IsApproved     { get; set; }
        /// <summary> Exact / Synonym / Fuzzy / Manual </summary>
        public string? SuggestedBy    { get; set; }
    }

    /// <summary> Full review payload returned to the Underwriter UI. </summary>
    public class UploadReviewModel
    {
        public RolloverUpload         Upload            { get; set; } = new();
        public RolloverUpload?        EnrollmentUpload  { get; set; }
        public RolloverUpload?        ClaimUpload       { get; set; }
        /// <summary>Enrollment or Claim — matches <see cref="Upload.DataCategory"/>.</summary>
        public string                 ActiveTab         { get; set; } = "Enrollment";
        public List<ColumnMapping>    Mappings          { get; set; } = new();
        public List<StandardColumn>   StandardColumns   { get; set; } = new();
        public decimal                AvgConfidence     { get; set; }
        public bool                   NeedsAttention    { get; set; }
    }

    /// <summary> Payload posted from the Underwriter Save / Approve modal. </summary>
    public class SaveMappingRequest
    {
        [Required]
        public int UploadId { get; set; }

        [Required]
        public List<ColumnMapping> Mappings { get; set; } = new();

        /// <summary>If true, finalises the upload to Mapped → ready for calculator.</summary>
        public bool Approve { get; set; }

        public string? Remarks { get; set; }
    }
}
