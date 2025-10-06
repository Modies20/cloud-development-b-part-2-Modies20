using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailFunctions.Services;
using System.Net;

namespace ABCRetailFunctions
{
    public class FileStorageFunction
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<FileStorageFunction> _logger;

        public FileStorageFunction(IAzureStorageService storageService, ILogger<FileStorageFunction> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadFile function");

            try
            {
                // Get filename from query string
                var fileName = req.Query["fileName"];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = $"document_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
                }

                // Read file content from request body
                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.Length == 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file data provided");
                    return badResponse;
                }

                var success = await _storageService.UploadFileAsync(memoryStream, fileName);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "File uploaded successfully to Azure File Storage" : "Failed to upload file",
                    fileName = fileName,
                    size = memoryStream.Length,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllFiles")]
        public async Task<HttpResponseData> GetAllFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "files")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GetAllFiles function");

            try
            {
                var files = await _storageService.GetAllFilesAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    count = files.Count,
                    files = files,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DownloadFile")]
        public async Task<HttpResponseData> DownloadFile(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "files/download/{fileName}")] HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation($"Processing DownloadFile function for {fileName}");

            try
            {
                var fileStream = await _storageService.DownloadFileAsync(fileName);

                if (fileStream == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"File '{fileName}' not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/octet-stream");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                
                await fileStream.CopyToAsync(response.Body);
                fileStream.Close();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileName}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteFile")]
        public async Task<HttpResponseData> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "files/{fileName}")] HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation($"Processing DeleteFile function for {fileName}");

            try
            {
                var success = await _storageService.DeleteFileAsync(fileName);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? $"File '{fileName}' deleted successfully" : $"File '{fileName}' not found",
                    fileName = fileName,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("UploadContract")]
        public async Task<HttpResponseData> UploadContract(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/contracts")] HttpRequestData req)
        {
            _logger.LogInformation("Processing UploadContract function");

            try
            {
                var contractId = req.Query["contractId"] ?? Guid.NewGuid().ToString();
                var fileName = $"contract_{contractId}_{DateTime.UtcNow:yyyyMMdd}.pdf";

                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.Length == 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No contract data provided");
                    return badResponse;
                }

                var success = await _storageService.UploadFileAsync(memoryStream, fileName);

                var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    success = success,
                    message = success ? "Contract uploaded successfully" : "Failed to upload contract",
                    contractId = contractId,
                    fileName = fileName,
                    size = memoryStream.Length,
                    timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}

