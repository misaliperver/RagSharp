using System.IO;
using System.Threading.Tasks;

namespace Decisionman.Services.Storage
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileAsync(string fileIdentifier);
        Task DeleteFileAsync(string fileIdentifier);
    }
}
