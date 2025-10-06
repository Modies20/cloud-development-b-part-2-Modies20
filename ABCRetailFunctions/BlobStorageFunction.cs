using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailFunctions.Services;
using System.Net;

namespace ABCRetailFunctions
{
    public class BlobStorageFunction
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<BlobStorageFunction> _logger;

        public BlobStorageFunction(IAzureStorageService storageService, ILogger<BlobStorageFunction> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blobs/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadBlob function");

            try
            {
                // Check if request has multipart/form-data content
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues) ||
                    !contentTypeValues.Any(ct => ct.Contains("multipart/form-data")))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Request must be multipart/form-data");
                    return badResponse;
                }

                // Read the file from the request body
                var fileName = req.Query["fileName"] ?? $"upload_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                var contentType = req.Query["contentType"] ?? "application/octet-stream";

                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.Length == 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file data provided");
                    return badResponse;
                }

                var imageUrl = await _storageService.UploadImageAsync(memoryStream, fileName, contentType);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "File uploaded successfully to Blob Storage",
                    fileName = fileName,
                    imageUrl = imageUrl,
                    size = memoryStream.Length
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllBlobs")]
        public async Task<HttpResponseData> GetAllBlobs(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blobs")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GetAllBlobs function");

            try
            {
                var blobs = await _storageService.GetAllImagesAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    count = blobs.Count,
                    blobs = blobs.Select(b => new
                    {
                        name = b.Name,
                        contentType = b.Properties.ContentType,
                        size = b.Properties.ContentLength,
                        createdOn = b.Properties.CreatedOn,
                        lastModified = b.Properties.LastModified
                    })
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blobs");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlob(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blobs/{fileName}")] HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation($"Processing DeleteBlob function for {fileName}");

            try
            {
                var success = await _storageService.DeleteImageAsync(fileName);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? $"Blob '{fileName}' deleted successfully" : $"Blob '{fileName}' not found",
                    fileName = fileName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob {fileName}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}

