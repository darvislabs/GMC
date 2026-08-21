using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    public interface IUnderwriterBL
    {
        Task<List<RolloverUpload>> GetQueueAsync(string role, string userName, string? status = null);

        /// <summary>
        /// Loads (and on first open generates) the AI-suggested column mapping
        /// for the given upload.  Idempotent — repeated calls return what was
        /// previously persisted unless <paramref name="forceRematch"/> is set.
        /// </summary>
        Task<UploadReviewModel> GetReviewAsync(int uploadId, bool forceRematch = false);

        /// <summary>
        /// Persists the underwriter's corrected mapping, learns new synonyms,
        /// optionally transitions status to Mapped (Approve=true) so the file
        /// is ready for the calculator.
        /// </summary>
        Task<RolloverUpload?> SaveMappingAsync(SaveMappingRequest req, string reviewedBy);

        /// <summary>Reject an upload (e.g. unusable file).</summary>
        Task RejectAsync(int uploadId, string reason, string reviewedBy);

        /// <summary>
        /// After one file is approved, decides whether to open the sibling Review tab
        /// or the GMC calculator (only when both Enrollment and Claim are Mapped).
        /// </summary>
        Task<PostApproveRedirect> GetPostApproveRedirectAsync(string policyNo);
    }

    public sealed class PostApproveRedirect
    {
        public bool OpenCalculator { get; init; }
        public int? ReviewUploadId { get; init; }
        public string? Message { get; init; }
    }
}
