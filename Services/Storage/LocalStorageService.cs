using System;
using System.IO;
using System.Threading.Tasks;

namespace Decisionman.Services.Storage
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _basePath;

        public LocalStorageService(string basePath = "wwwroot/uploads")
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_basePath, uniqueFileName);

            using var fileStreamLocal = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamLocal);
            
            return uniqueFileName; // Return identifier
        }

        public Task<Stream> GetFileAsync(string fileIdentifier)
        {
            var filePath = Path.Combine(_basePath, fileIdentifier);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {fileIdentifier}");

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteFileAsync(string fileIdentifier)
        {
            var filePath = Path.Combine(_basePath, fileIdentifier);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
