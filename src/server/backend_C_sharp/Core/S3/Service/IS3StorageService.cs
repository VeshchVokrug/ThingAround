namespace Core.S3.Service;

public interface IS3StorageService
{
    (string UploadUrl, string PublicUrl) GenerateUploadUrls(string objectKey, int expirationMinutes = 15);
    Task<bool> FileExistsAsync(string objectKey);
    Task DeleteFileAsync(string objectKey);
    string GetKeyFromUrl(string url);
}