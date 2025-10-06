using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllOrdersAsync();
            return View(orders.OrderByDescending(o => o.DateCreated).ToList());
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View(new Order());
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (order.Quantity <= 0)
            {
                ModelState.AddModelError(nameof(order.Quantity), "Quantity must be greater than zero");
            }

            // Map selected keys to names and unit price
            var customer = await _storageService.GetCustomerProfileAsync("Customer", order.CustomerRowKey);
            var product = await _storageService.GetProductAsync("Product", order.ProductRowKey);
            if (customer == null)
            {
                ModelState.AddModelError(nameof(order.CustomerRowKey), "Please select a valid customer");
            }
            if (product == null)
            {
                ModelState.AddModelError(nameof(order.ProductRowKey), "Please select a valid product");
            }
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                TempData["ErrorMessage"] = "Please correct the highlighted errors.";
                return View(order);
            }

            order.CustomerName = $"{customer!.FirstName} {customer!.LastName}";
            order.ProductName = product!.Name;
            order.UnitPrice = product.Price;

            var ok = await _storageService.AddOrderAsync(order);
            if (ok)
            {
                TempData["SuccessMessage"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            TempData["ErrorMessage"] = "Failed to create order.";
            return View(order);
        }

        // GET: Order/Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _storageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();
            return View(order);
        }

        private async Task PopulateDropdownsAsync()
        {
            var customers = await _storageService.GetAllCustomerProfilesAsync();
            var products = await _storageService.GetAllProductsAsync();

            ViewBag.CustomerOptions = customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = $"{c.FirstName} {c.LastName}"
                })
                .ToList();

            ViewBag.ProductOptions = products
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.RowKey,
                    Text = $"{p.Name} (R{p.Price:N2})"
                })
                .ToList();
        }
    }
}

