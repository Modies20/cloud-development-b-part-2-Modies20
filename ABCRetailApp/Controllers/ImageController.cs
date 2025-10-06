using Microsoft.AspNetCore.Mvc;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class ImageController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public ImageController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Image
        public async Task<IActionResult> Index()
        {
            var images = await _storageService.GetAllImagesAsync();
            return View(images);
        }

        // GET: Image/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Image/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return View();
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Only image files (jpg, jpeg, png, gif, bmp) are allowed.";
                return View();
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size must be less than 5MB.";
                return View();
            }

            try
            {
                using var stream = file.OpenReadStream();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var imageUrl = await _storageService.UploadImageAsync(stream, fileName, file.ContentType);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    TempData["SuccessMessage"] = $"Image '{file.FileName}' uploaded successfully!";
                    TempData["ImageUrl"] = imageUrl;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload image.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading image: {ex.Message}";
            }

            return View();
        }

        // GET: Image/Download/{fileName}
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var stream = await _storageService.DownloadImageAsync(fileName);
                if (stream == Stream.Null)
                {
                    return NotFound();
                }

                var contentType = GetContentType(fileName);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading image: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Image/Delete/{fileName}
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
                var success = await _storageService.DeleteImageAsync(fileName);
                if (success)
                {
                    TempData["SuccessMessage"] = "Image deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete image.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting image: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Image/ViewImage/{fileName}
        public async Task<IActionResult> ViewImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var imageUrl = await _storageService.GetImageUrlAsync(fileName);
            if (string.IsNullOrEmpty(imageUrl))
            {
                return NotFound();
            }

            ViewBag.ImageUrl = imageUrl;
            ViewBag.FileName = fileName;
            return View();
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}
