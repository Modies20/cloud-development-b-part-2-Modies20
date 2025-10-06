using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailFunctions.Services;
using System.Net;
using System.Text.Json;

namespace ABCRetailFunctions
{
    public class StoreTableDataFunction
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<StoreTableDataFunction> _logger;

        public StoreTableDataFunction(IAzureStorageService storageService, ILogger<StoreTableDataFunction> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("StoreCustomerProfile")]
        public async Task<HttpResponseData> StoreCustomerProfile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
        {
            _logger.LogInformation("Processing StoreCustomerProfile function");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonSerializer.Deserialize<CustomerProfile>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (customer == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer data");
                    return badResponse;
                }

                var success = await _storageService.AddCustomerProfileAsync(customer);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "Customer profile stored successfully" : "Failed to store customer profile",
                    customer = success ? customer : null
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing customer profile");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("StoreProduct")]
        public async Task<HttpResponseData> StoreProduct(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Processing StoreProduct function");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var product = JsonSerializer.Deserialize<Product>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (product == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid product data");
                    return badResponse;
                }

                var success = await _storageService.AddProductAsync(product);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "Product stored successfully" : "Failed to store product",
                    product = success ? product : null
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing product");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAllCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GetAllCustomers function");

            try
            {
                var customers = await _storageService.GetAllCustomerProfilesAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    count = customers.Count,
                    customers = customers
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAllProducts(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GetAllProducts function");

            try
            {
                var products = await _storageService.GetAllProductsAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    count = products.Count,
                    products = products
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}

