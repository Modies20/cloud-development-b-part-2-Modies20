using Microsoft.AspNetCore.Mvc;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public ProductController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Try to bind the uploaded image from Request
            var file = Request.Form.Files["productImage"];
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _storageService.UploadImageAsync(stream, file.FileName, file.ContentType);
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    product.ImageUrl = imageUrl;
                }
            }

            var success = await _storageService.AddProductAsync(product);
            if (success)
            {
                await _storageService.SendMessageAsync($"New product added: {product.Name}");
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create product.";
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                var success = await _storageService.UpdateProductAsync(product);
                if (success)
                {
                    // Send queue message about product update
                    await _storageService.SendMessageAsync($"Product updated: {product.Name}");
                    
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update product.";
                }
            }
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _storageService.GetProductAsync(partitionKey, rowKey);
            var success = await _storageService.DeleteProductAsync(partitionKey, rowKey);
            
            if (success)
            {
                // Send queue message about product deletion
                if (product != null)
                {
                    await _storageService.SendMessageAsync($"Product deleted: {product.Name}");
                }
                
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete product.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
