namespace GMC.Models.GMC
{
    /// <summary> One stored file (zip or excel) shown on the AboutData page. </summary>
    public class AboutDataFile
    {
        public string Name        { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long   SizeBytes   { get; set; }
        public DateTime UploadedOn { get; set; }

        public string SizeDisplay =>
            SizeBytes >= 1024 * 1024
                ? $"{SizeBytes / (1024d * 1024d):0.0} MB"
                : $"{SizeBytes / 1024d:0.0} KB";
    }

    /// <summary> View model for the AboutData upload / download page. </summary>
    public class AboutDataViewModel
    {
        public List<AboutDataFile> ZipFiles   { get; set; } = new();
        public List<AboutDataFile> ExcelFiles { get; set; } = new();

        /// <summary>Most recent zip, used by the prominent download button.</summary>
        public AboutDataFile? LatestZip => ZipFiles.Count > 0 ? ZipFiles[0] : null;
    }
}
