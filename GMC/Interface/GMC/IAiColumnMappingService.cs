namespace GMC.Interface.GMC
{
    public sealed class AiColumnSuggestion
    {
        public string? TargetColumn { get; init; }
        public decimal ConfidencePct { get; init; }
    }

    /// <summary>
    /// Free AI column-mapping provider (Gemini). Maps Excel headers to master DB columns.
    /// </summary>
    public interface IAiColumnMappingService
    {
        Task<IReadOnlyDictionary<string, AiColumnSuggestion>> SuggestAsync(
            IReadOnlyList<string> sourceColumns,
            IReadOnlyList<string> masterColumns,
            string dataCategory,
            CancellationToken cancellationToken = default);
    }
}
