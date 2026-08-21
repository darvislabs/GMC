using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    /// <summary>
    /// AI / heuristic column-matching service.
    /// Given the source Excel headers and the canonical DB columns, returns a
    /// list of <see cref="ColumnMapping"/> rows with a confidence score per source.
    /// The default implementation is <c>LocalFuzzyMatcher</c> — no external API
    /// calls.  Future implementations could call OpenAI / Azure OpenAI behind
    /// this same interface.
    /// </summary>
    public interface IColumnMatcher
    {
        Task<List<ColumnMapping>> MatchAsync(IEnumerable<string> sourceColumns,
                                             string? dataCategory = null);
    }
}
