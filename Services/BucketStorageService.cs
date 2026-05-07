using Google.Cloud.Storage.V1;
using restaurant.Interfaces;

namespace restaurant.Services;

public class BucketStorageService : IBucketStorageService
{
    private readonly ILogger<BucketStorageService> _logger;
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;

    public BucketStorageService(ILogger<BucketStorageService> logger, IConfiguration config)
    {
        _logger = logger;
        _bucketName = config.GetValue<string>("Storage:Google:BucketName")!;
        _storageClient = StorageClient.Create();
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentNullException(nameof(file), "File is empty or null");

        string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        if (!permittedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            throw new ArgumentException("File extension not accepted. Allowed: jpg, jpeg, png, gif");

        try
        {
            if (string.IsNullOrWhiteSpace(fileNameForStorage))
                fileNameForStorage = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            await _storageClient.UploadObjectAsync(
                _bucketName, fileNameForStorage, file.ContentType, memoryStream);

            _logger.LogInformation("Uploaded {FileName} to bucket {Bucket}",
                fileNameForStorage, _bucketName);

            return $"gs://{_bucketName}/{fileNameForStorage}";
        }
        catch (Google.GoogleApiException gae)
        {
            _logger.LogError(gae, "Google API error during upload");
            throw new ApplicationException($"Google API Error: {gae.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during upload");
            throw new ApplicationException($"Upload error: {e.Message}");
        }
    }

    public Task DeleteFileAsync(string fileName)
    {
        throw new NotImplementedException();
    }
}
