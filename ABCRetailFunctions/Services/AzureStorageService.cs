using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;

namespace ABCRetailFunctions.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;
        private readonly TableClient _orderTableClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly QueueClient _queueClient;
        private readonly ShareClient _shareClient;

        public AzureStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? configuration["AzureStorage:ConnectionString"];

            // Initialize Table clients
            var tableServiceClient = new TableServiceClient(connectionString);
            _customerTableClient = tableServiceClient.GetTableClient(configuration["AzureStorage:TableName"]);
            _productTableClient = tableServiceClient.GetTableClient(configuration["AzureStorage:ProductTableName"]);
            _orderTableClient = tableServiceClient.GetTableClient(configuration["AzureStorage:OrderTableName"]);

            // Initialize Blob client
            var blobServiceClient = new BlobServiceClient(connectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(configuration["AzureStorage:BlobContainerName"]);

            // Initialize Queue client
            _queueClient = new QueueClient(connectionString, configuration["AzureStorage:QueueName"]);

            // Initialize File Share client
            var shareServiceClient = new ShareServiceClient(connectionString);
            _shareClient = shareServiceClient.GetShareClient(configuration["AzureStorage:FileShareName"]);

            // Ensure containers/tables/queues exist
            InitializeStorageAsync().Wait();
        }

        private async Task InitializeStorageAsync()
        {
            try
            {
                await _customerTableClient.CreateIfNotExistsAsync();
                await _productTableClient.CreateIfNotExistsAsync();
                await _orderTableClient.CreateIfNotExistsAsync();
                await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                await _queueClient.CreateIfNotExistsAsync();
                await _shareClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - app should still work
                Console.WriteLine($"Error initializing storage: {ex.Message}");
            }
        }

        #region Table Storage Operations

        public async Task<bool> AddOrderAsync(Order order)
        {
            try
            {
                order.RowKey = Guid.NewGuid().ToString();
                order.TotalAmount = order.UnitPrice * order.Quantity;
                await _orderTableClient.AddEntityAsync(order);

                // Queue notification for new order
                await SendMessageAsync($"[ORDER_CREATED] #{order.RowKey} for '{order.CustomerName}' - {order.Quantity} x '{order.ProductName}' @ {order.UnitPrice:F2} = {order.TotalAmount:F2}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding order: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddCustomerProfileAsync(CustomerProfile customer)
        {
            try
            {
                customer.RowKey = Guid.NewGuid().ToString();
                await _customerTableClient.AddEntityAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding customer: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                product.RowKey = Guid.NewGuid().ToString();
                await _productTableClient.AddEntityAsync(product);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
                return false;
            }
        }

        public async Task<List<CustomerProfile>> GetAllCustomerProfilesAsync()
        {
            try
            {
                var customers = new List<CustomerProfile>();
                await foreach (var customer in _customerTableClient.QueryAsync<CustomerProfile>())
                {
                    customers.Add(customer);
                }
                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customers: {ex.Message}");
                return new List<CustomerProfile>(0);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = new List<Product>();
                await foreach (var product in _productTableClient.QueryAsync<Product>())
                {
                    products.Add(product);
                }
                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products: {ex.Message}");
                return new List<Product>(0);
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                var orders = new List<Order>();
                await foreach (var order in _orderTableClient.QueryAsync<Order>())
                {
                    orders.Add(order);
                }
                return orders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting orders: {ex.Message}");
                return new List<Order>(0);
            }
        }

        public async Task<CustomerProfile?> GetCustomerProfileAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customer: {ex.Message}");
                return null;
            }
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting product: {ex.Message}");
                return null;
            }
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting order: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateCustomerProfileAsync(CustomerProfile customer)
        {
            try
            {
                await _customerTableClient.UpdateEntityAsync(customer, customer.ETag);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating customer: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                await _productTableClient.UpdateEntityAsync(product, product.ETag);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            try
            {
                await _orderTableClient.UpdateEntityAsync(order, order.ETag);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCustomerProfileAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting customer: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Blob Storage Operations

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);
                await blobClient.UploadAsync(imageStream, overwrite: true);
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType });

                // Send queue message about image upload
                await SendMessageAsync($"Image uploaded: {fileName}");

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<BlobItem>> GetAllImagesAsync()
        {
            try
            {
                var blobs = new List<BlobItem>();
                await foreach (var blobItem in _blobContainerClient.GetBlobsAsync())
                {
                    blobs.Add(blobItem);
                }
                return blobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting images: {ex.Message}");
                return new List<BlobItem>();
            }
        }

        public async Task<Stream> DownloadImageAsync(string fileName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);
                var response = await blobClient.DownloadStreamingAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
                return Stream.Null;
            }
        }

        public async Task<bool> DeleteImageAsync(string fileName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);
                await blobClient.DeleteIfExistsAsync();

                // Send queue message about image deletion
                await SendMessageAsync($"Image deleted: {fileName}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
                return false;
            }
        }

        public Task<string> GetImageUrlAsync(string fileName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);
                return Task.FromResult(blobClient.Uri.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image URL: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }

        #endregion

        #region Queue Storage Operations

        public async Task<bool> SendMessageAsync(string message)
        {
            try
            {
                await _queueClient.SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> ReceiveMessageAsync()
        {
            try
            {
                var response = await _queueClient.ReceiveMessageAsync();
                if (response.Value != null)
                {
                    await _queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                    return response.Value.MessageText;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetQueueMessagesAsync(int maxMessages = 10)
        {
            try
            {
                var messages = new List<string>();
                var response = await _queueClient.PeekMessagesAsync(maxMessages);

                foreach (var message in response.Value)
                {
                    messages.Add(message.MessageText);
                }

                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting queue messages: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt)
        {
            try
            {
                await _queueClient.DeleteMessageAsync(messageId, popReceipt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Storage Operations

        public async Task<bool> UploadFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                var directoryClient = _shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadAsync(fileStream);

                // Send queue message about file upload
                await SendMessageAsync($"Contract file uploaded: {fileName}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAllFilesAsync()
        {
            try
            {
                var files = new List<string>();
                var directoryClient = _shareClient.GetRootDirectoryClient();

                await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        files.Add(item.Name);
                    }
                }

                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting files: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            try
            {
                var directoryClient = _shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);
                var response = await fileClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return Stream.Null;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var directoryClient = _shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.DeleteIfExistsAsync();

                // Send queue message about file deletion
                await SendMessageAsync($"Contract file deleted: {fileName}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
