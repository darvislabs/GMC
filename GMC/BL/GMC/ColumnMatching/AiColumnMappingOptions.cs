namespace GMC.BL.GMC.ColumnMatching
{
    /// <summary>
    /// Google Gemini free-tier settings (https://aistudio.google.com/apikey).
    /// When <see cref="Enabled"/> is false or <see cref="ApiKey"/> is empty,
    /// the matcher falls back to local fuzzy matching.
    /// </summary>
    public class AiColumnMappingOptions
    {
        public const string SectionName = "ColumnMappingAi";

        public bool Enabled { get; set; } = true;

        /// <summary>Free Gemini API key from Google AI Studio.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-2.0-flash";

        public string BaseUrl { get; set; } =
            "https://generativelanguage.googleapis.com/v1beta/models/";
    }
}
