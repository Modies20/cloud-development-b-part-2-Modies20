using Microsoft.AspNetCore.Mvc;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllCustomerProfilesAsync();
            return View(customers);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _storageService.GetCustomerProfileAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile customer)
        {
            if (ModelState.IsValid)
            {
                var success = await _storageService.AddCustomerProfileAsync(customer);
                if (success)
                {
                    TempData["SuccessMessage"] = "Customer profile created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create customer profile.";
                }
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _storageService.GetCustomerProfileAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerProfile customer)
        {
            if (ModelState.IsValid)
            {
                var success = await _storageService.UpdateCustomerProfileAsync(customer);
                if (success)
                {
                    TempData["SuccessMessage"] = "Customer profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update customer profile.";
                }
            }
            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _storageService.GetCustomerProfileAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var success = await _storageService.DeleteCustomerProfileAsync(partitionKey, rowKey);
            if (success)
            {
                TempData["SuccessMessage"] = "Customer profile deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete customer profile.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
