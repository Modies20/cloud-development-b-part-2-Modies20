using Microsoft.AspNetCore.Mvc;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class FileController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public FileController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: File
        public async Task<IActionResult> Index()
        {
            var files = await _storageService.GetAllFilesAsync();
            return View(files);
        }

        // GET: File/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: File/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return View();
            }

            // Validate file type (contracts - typically PDF, DOC, DOCX, TXT)
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Only document files (pdf, doc, docx, txt, rtf) are allowed for contracts.";
                return View();
            }

            // Validate file size (max 10MB for documents)
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size must be less than 10MB.";
                return View();
            }

            try
            {
                using var stream = file.OpenReadStream();
                var fileName = $"Contract_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{file.FileName}";
                var success = await _storageService.UploadFileAsync(stream, fileName);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Contract '{file.FileName}' uploaded successfully as '{fileName}'!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload contract file.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading contract file: {ex.Message}";
            }

            return View();
        }

        // GET: File/Download/{fileName}
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var stream = await _storageService.DownloadFileAsync(fileName);
                if (stream == Stream.Null)
                {
                    return NotFound();
                }

                var contentType = GetContentType(fileName);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading contract file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: File/Delete/{fileName}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var success = await _storageService.DeleteFileAsync(fileName);
                if (success)
                {
                    TempData["SuccessMessage"] = "Contract file deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete contract file.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting contract file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Removed UploadSampleContracts at user's request (seed/sample data)


        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                _ => "application/octet-stream"
            };
        }
    }
}
