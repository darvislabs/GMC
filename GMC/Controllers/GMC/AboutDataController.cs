using Microsoft.AspNetCore.Mvc;
using GMC.Models.GMC;

namespace GMC.Controllers.GMC
{
    /// <summary>
    /// AboutData workspace: upload a .zip archive and an Excel (.xlsx/.xls) file,
    /// and download zip files.
    /// Uploaded files are stored under wwwroot/upload/AboutData/.
    /// Downloadable zip files are served from wwwroot/zipdownload/.
    /// </summary>
    public class AboutDataController : Controller
    {
        private const string UploadRelativePath   = "upload/AboutData";
        private const string ZipDownloadRelativePath = "zipdownload";

        private static readonly string[] ZipExtensions   = { ".zip" };
        private static readonly string[] ExcelExtensions = { ".xlsx", ".xls" };

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AboutDataController> _log;

        public AboutDataController(IWebHostEnvironment env, ILogger<AboutDataController> log)
        {
            _env = env;
            _log = log;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(BuildModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = int.MaxValue)]
        [RequestSizeLimit(500 * 1024 * 1024)]   // 500 MB (zip can be large)
        public async Task<IActionResult> Upload(IFormFile? zipFile, IFormFile? excelFile)
        {
            bool hasZip   = zipFile   != null && zipFile.Length   > 0;
            bool hasExcel = excelFile != null && excelFile.Length > 0;

            if (!hasZip && !hasExcel)
            {
                TempData["UploadError"] = "Please choose a ZIP file and/or an Excel file to upload.";
                return RedirectToAction(nameof(Index));
            }

            if (hasZip && !HasAllowedExtension(zipFile!, ZipExtensions))
            {
                TempData["UploadError"] = "The archive must be a .zip file.";
                return RedirectToAction(nameof(Index));
            }

            if (hasExcel && !HasAllowedExtension(excelFile!, ExcelExtensions))
            {
                TempData["UploadError"] = "The Excel file must be a .xlsx or .xls file.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var saved = new List<string>();
                if (hasZip)   saved.Add(await SaveAsync(zipFile!));
                if (hasExcel) saved.Add(await SaveAsync(excelFile!));

                TempData["UploadSuccess"] = $"Uploaded successfully: {string.Join(", ", saved)}.";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AboutData upload failed.");
                TempData["UploadError"] = "Upload failed: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Downloads a .zip file from wwwroot/zipdownload/. When <paramref name="file"/>
        /// is empty, the most recently modified zip in that folder is served.
        /// </summary>
        [HttpGet]
        public IActionResult DownloadZip(string? file)
        {
            var zipDir = Path.Combine(_env.WebRootPath, ZipDownloadRelativePath);

            string? fullPath;
            if (string.IsNullOrWhiteSpace(file))
            {
                fullPath = Directory.Exists(zipDir)
                    ? Directory.EnumerateFiles(zipDir, "*.zip", SearchOption.AllDirectories)
                        .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                        .FirstOrDefault()
                    : null;
            }
            else
            {
                fullPath = ResolveZipWithinDownloadFolder(file);
            }

            if (string.IsNullOrEmpty(fullPath) || !System.IO.File.Exists(fullPath))
            {
                TempData["UploadError"] = "No ZIP file available to download.";
                return RedirectToAction(nameof(Index));
            }

            var downloadName = Path.GetFileName(fullPath);
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/zip", downloadName);
        }

        /// <summary>
        /// Resolves a caller-supplied relative path to an absolute path, ensuring it
        /// stays inside wwwroot/zipdownload and points to a .zip file (path-traversal guard).
        /// </summary>
        private string? ResolveZipWithinDownloadFolder(string relative)
        {
            var zipDir = Path.GetFullPath(Path.Combine(_env.WebRootPath, ZipDownloadRelativePath));

            // Accept either a bare name or a path already relative to wwwroot/zipdownload.
            var normalized = relative.Replace('/', Path.DirectorySeparatorChar)
                                     .Replace('\\', Path.DirectorySeparatorChar)
                                     .TrimStart(Path.DirectorySeparatorChar);
            if (normalized.StartsWith(ZipDownloadRelativePath + Path.DirectorySeparatorChar,
                                      StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(ZipDownloadRelativePath.Length + 1);
            }

            var candidate = Path.GetFullPath(Path.Combine(zipDir, normalized));

            var dirWithSep = zipDir.EndsWith(Path.DirectorySeparatorChar)
                ? zipDir
                : zipDir + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(dirWithSep, StringComparison.OrdinalIgnoreCase))
                return null;
            if (!string.Equals(Path.GetExtension(candidate), ".zip", StringComparison.OrdinalIgnoreCase))
                return null;

            return candidate;
        }

        // ---- helpers ---------------------------------------------------------

        private async Task<string> SaveAsync(IFormFile file)
        {
            var destDir = Path.Combine(_env.WebRootPath, UploadRelativePath);
            Directory.CreateDirectory(destDir);

            var safeName = Path.GetFileName(file.FileName);
            var stamp    = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var stored   = $"{stamp}_{safeName}";
            var fullPath = Path.Combine(destDir, stored);

            using var fs = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(fs);
            return safeName;
        }

        private static bool HasAllowedExtension(IFormFile file, string[] allowed)
        {
            var ext = Path.GetExtension(file.FileName);
            return allowed.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        private AboutDataViewModel BuildModel()
        {
            var vm   = new AboutDataViewModel();
            var root = _env.WebRootPath;
            if (!Directory.Exists(root)) return vm;

            // Zip files: any .zip inside wwwroot/zipdownload.
            var zipDir = Path.Combine(root, ZipDownloadRelativePath);
            if (Directory.Exists(zipDir))
            {
                foreach (var f in new DirectoryInfo(zipDir)
                             .GetFiles("*.zip", SearchOption.AllDirectories)
                             .OrderByDescending(f => f.LastWriteTimeUtc))
                {
                    var relative = Path.GetRelativePath(zipDir, f.FullName).Replace('\\', '/');
                    vm.ZipFiles.Add(new AboutDataFile
                    {
                        Name        = relative,
                        SizeBytes   = f.Length,
                        UploadedOn  = f.LastWriteTime,
                        DownloadUrl = Url.Action(nameof(DownloadZip), new { file = relative }) ?? "#"
                    });
                }
            }

            // Excel files: the ones uploaded through this page.
            var uploadDir = Path.Combine(root, UploadRelativePath);
            if (Directory.Exists(uploadDir))
            {
                foreach (var f in new DirectoryInfo(uploadDir)
                             .GetFiles()
                             .Where(f => ExcelExtensions.Contains(f.Extension.ToLowerInvariant()))
                             .OrderByDescending(f => f.LastWriteTimeUtc))
                {
                    vm.ExcelFiles.Add(new AboutDataFile
                    {
                        Name        = f.Name,
                        SizeBytes   = f.Length,
                        UploadedOn  = f.LastWriteTime,
                        DownloadUrl = $"/{UploadRelativePath}/{Uri.EscapeDataString(f.Name)}"
                    });
                }
            }

            return vm;
        }
    }
}
