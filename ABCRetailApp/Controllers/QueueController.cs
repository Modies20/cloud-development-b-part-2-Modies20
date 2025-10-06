using Microsoft.AspNetCore.Mvc;
using ABCRetailApp.Services;

namespace ABCRetailApp.Controllers
{
    public class QueueController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public QueueController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Queue
        public async Task<IActionResult> Index()
        {
            var messages = await _storageService.GetQueueMessagesAsync(20);
            return View(messages);
        }

        // GET: Queue/SendMessage
        public IActionResult SendMessage()
        {
            return View();
        }

        // POST: Queue/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string message, string messageType = "General")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Please enter a message.";
                return View();
            }

            try
            {
                // Format the message with timestamp and type
                var formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [{messageType}] {message}";
                var success = await _storageService.SendMessageAsync(formattedMessage);

                if (success)
                {
                    TempData["SuccessMessage"] = "Message sent successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send message.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending message: {ex.Message}";
            }

            return View();
        }

        // POST: Queue/SendOrderProcessingMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOrderProcessingMessage(string orderId, string customerName, decimal orderAmount)
        {
            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(customerName))
            {
                TempData["ErrorMessage"] = "Order ID and Customer Name are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var message = $"Processing order #{orderId} for customer '{customerName}' - Amount: ${orderAmount:F2}";
                var formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [ORDER_PROCESSING] {message}";
                
                var success = await _storageService.SendMessageAsync(formattedMessage);

                if (success)
                {
                    TempData["SuccessMessage"] = "Order processing message sent successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send order processing message.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending order processing message: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Queue/SendInventoryMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInventoryMessage(string productName, string action, int quantity)
        {
            if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(action))
            {
                TempData["ErrorMessage"] = "Product Name and Action are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var message = $"Inventory {action}: {quantity} units of '{productName}'";
                var formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [INVENTORY_MANAGEMENT] {message}";
                
                var success = await _storageService.SendMessageAsync(formattedMessage);

                if (success)
                {
                    TempData["SuccessMessage"] = "Inventory management message sent successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send inventory management message.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending inventory management message: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Queue/OrderProcessing
        public IActionResult OrderProcessing()
        {
            return View();
        }

        // GET: Queue/InventoryManagement
        public IActionResult InventoryManagement()
        {
            return View();
        }

        // POST: Queue/ProcessMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessMessage()
        {
            try
            {
                var message = await _storageService.ReceiveMessageAsync();
                if (!string.IsNullOrEmpty(message))
                {
                    TempData["SuccessMessage"] = $"Processed message: {message}";
                }
                else
                {
                    TempData["InfoMessage"] = "No messages available to process.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing message: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Simulators removed at user's request (seed/sample data)
        // If needed in the future, consider adding them back behind a feature flag or admin-only route.

    }
}
