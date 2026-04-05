using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<string> UploadAsync(IFormFile file, string folderPath);
    Task DeleteAsync(string fileUrl);
}
