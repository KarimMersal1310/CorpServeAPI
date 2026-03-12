using CorpServe.Services.Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CorpServe.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadAsync(IFormFile file, string folderPath)
        {
            var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var normalizedFolderPath = folderPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullFolderPath = Path.Combine(webRootPath, normalizedFolderPath);

            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullFilePath = Path.Combine(fullFolderPath, uniqueFileName);

            await using var fileStream = new FileStream(fullFilePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            var relativePath = Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");
            return relativePath.StartsWith("/") ? relativePath : $"/{relativePath}";
        }
    }
}
