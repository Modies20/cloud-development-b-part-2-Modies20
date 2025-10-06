using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using System.ComponentModel.DataAnnotations;


namespace ABCRetailFunctions.Services
{
    public interface IAzureStorageService
    {
        // Table Storage Operations
        Task<bool> AddCustomerProfileAsync(CustomerProfile customer);
        Task<bool> AddProductAsync(Product product);
        Task<bool> AddOrderAsync(Order order);
        Task<List<CustomerProfile>> GetAllCustomerProfilesAsync();
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Order>> GetAllOrdersAsync();
        Task<CustomerProfile?> GetCustomerProfileAsync(string partitionKey, string rowKey);
        Task<Product?> GetProductAsync(string partitionKey, string rowKey);
        Task<Order?> GetOrderAsync(string partitionKey, string rowKey);
        Task<bool> UpdateCustomerProfileAsync(CustomerProfile customer);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> UpdateOrderAsync(Order order);
        Task<bool> DeleteCustomerProfileAsync(string partitionKey, string rowKey);
        Task<bool> DeleteProductAsync(string partitionKey, string rowKey);
        Task<bool> DeleteOrderAsync(string partitionKey, string rowKey);

        // Blob Storage Operations
        Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType);
        Task<List<BlobItem>> GetAllImagesAsync();
        Task<Stream> DownloadImageAsync(string fileName);
        Task<bool> DeleteImageAsync(string fileName);
        Task<string> GetImageUrlAsync(string fileName);

        // Queue Storage Operations
        Task<bool> SendMessageAsync(string message);
        Task<string?> ReceiveMessageAsync();
        Task<List<string>> GetQueueMessagesAsync(int maxMessages = 10);
        Task<bool> DeleteMessageAsync(string messageId, string popReceipt);

        // File Storage Operations
        Task<bool> UploadFileAsync(Stream fileStream, string fileName);
        Task<List<string>> GetAllFilesAsync();
        Task<Stream> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
    }

    // Table Entity Models
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Phone]
        public string Phone { get; set; } = string.Empty;
        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }

    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public double Price { get; set; }
        [Required, StringLength(100)]
        public string Category { get; set; } = string.Empty;
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        [Url]
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }

    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        // Foreign-like references
        [Required]
        public string CustomerRowKey { get; set; } = string.Empty; // Customer.RowKey
        [Required]
        public string ProductRowKey { get; set; } = string.Empty;  // Product.RowKey

        // Order fields
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        [Range(0, double.MaxValue)]
        public double UnitPrice { get; set; }
        public double TotalAmount { get; set; }
        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        [Required, StringLength(30)]
        public string Status { get; set; } = "New"; // New, Processing, Completed

        // Denormalized for convenience (optional)
        [Required, StringLength(120)]
        public string CustomerName { get; set; } = string.Empty;
        [Required, StringLength(120)]
        public string ProductName { get; set; } = string.Empty;
    }
}
