using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailFunctions.Services;
using System.Net;
using System.Text.Json;

namespace ABCRetailFunctions
{
    public class QueueStorageFunction
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<QueueStorageFunction> _logger;

        public QueueStorageFunction(IAzureStorageService storageService, ILogger<QueueStorageFunction> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("SendQueueMessage")]
        public async Task<HttpResponseData> SendQueueMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "queue/send")] HttpRequestData req)
        {
            _logger.LogInformation("Processing SendQueueMessage function");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<QueueMessageRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null || string.IsNullOrWhiteSpace(data.Message))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Message is required");
                    return badResponse;
                }

                // Format the message with timestamp and type
                var formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [{data.MessageType ?? "GENERAL"}] {data.Message}";
                var success = await _storageService.SendMessageAsync(formattedMessage);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "Message sent to queue successfully" : "Failed to send message to queue",
                    queueMessage = formattedMessage,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue message");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("ReceiveQueueMessage")]
        public async Task<HttpResponseData> ReceiveQueueMessage(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/receive")] HttpRequestData req)
        {
            _logger.LogInformation("Processing ReceiveQueueMessage function");

            try
            {
                var message = await _storageService.ReceiveMessageAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = !string.IsNullOrEmpty(message),
                    message = message ?? "No messages in queue",
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving queue message");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetQueueMessages")]
        public async Task<HttpResponseData> GetQueueMessages(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/messages")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GetQueueMessages function");

            try
            {
                var maxMessages = 10;
                if (req.Query["maxMessages"] != null && int.TryParse(req.Query["maxMessages"], out var max))
                {
                    maxMessages = max;
                }

                var messages = await _storageService.GetQueueMessagesAsync(maxMessages);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    count = messages.Count,
                    messages = messages,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue messages");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("SubmitOrder")]
        public async Task<HttpResponseData> SubmitOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/submit")] HttpRequestData req)
        {
            _logger.LogInformation("Processing SubmitOrder function - Adding order to queue");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var orderRequest = JsonSerializer.Deserialize<OrderSubmitRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orderRequest == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid order data");
                    return badResponse;
                }

                // Create Order entity
                var order = new Order
                {
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerRowKey = orderRequest.CustomerRowKey,
                    ProductRowKey = orderRequest.ProductRowKey,
                    CustomerName = orderRequest.CustomerName,
                    ProductName = orderRequest.ProductName,
                    Quantity = orderRequest.Quantity,
                    UnitPrice = orderRequest.UnitPrice,
                    TotalAmount = orderRequest.Quantity * orderRequest.UnitPrice,
                    Status = "Pending",
                    Notes = orderRequest.Notes ?? string.Empty,
                    DateCreated = DateTime.UtcNow
                };

                // Serialize the order to JSON and send to queue
                var orderJson = JsonSerializer.Serialize(order);
                var success = await _storageService.SendMessageAsync(orderJson);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "Order submitted successfully and queued for processing" : "Failed to submit order",
                    orderId = order.RowKey,
                    totalAmount = order.TotalAmount,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// QUEUE-TRIGGERED FUNCTION - This is automatically triggered when a message arrives in the queue
        /// This function processes orders from the queue and stores them in Table Storage
        /// </summary>
        [Function("ProcessOrderFromQueue")]
        public async Task ProcessOrderFromQueue(
            [QueueTrigger("order-processing", Connection = "AzureStorage__ConnectionString")] string queueMessage)
        {
            _logger.LogInformation($"Queue trigger function processing order: {queueMessage}");

            try
            {
                // Deserialize the order from the queue message
                var order = JsonSerializer.Deserialize<Order>(queueMessage, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (order == null)
                {
                    _logger.LogError("Failed to deserialize order from queue message");
                    return;
                }

                // Update status to Processing
                order.Status = "Processing";

                // Store the order in Table Storage using the service
                var success = await _storageService.AddOrderAsync(order);

                if (success)
                {
                    _logger.LogInformation($"Successfully processed and stored order {order.RowKey} from queue");
                }
                else
                {
                    _logger.LogError($"Failed to store order {order.RowKey} in Table Storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing order from queue: {queueMessage}");
                // The message will be retried automatically by Azure Functions
                throw; // Re-throw to trigger retry mechanism
            }
        }
    }

    // Helper classes for request models
    public class QueueMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? MessageType { get; set; }
    }

    public class OrderSubmitRequest
    {
        public string CustomerRowKey { get; set; } = string.Empty;
        public string ProductRowKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string? Notes { get; set; }
    }
}

