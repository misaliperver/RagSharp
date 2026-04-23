using System;
using System.IO;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;

namespace Decisionman.Services.Storage
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioStorageService(string endpoint, string accessKey, string secretKey, string bucketName)
        {
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(false) 
                .Build();
            _bucketName = bucketName;
            
            EnsureBucketExistsAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureBucketExistsAsync()
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            fileStream.Position = 0; 

            var poArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(uniqueFileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(poArgs);
            return uniqueFileName;
        }

        public async Task<Stream> GetFileAsync(string fileIdentifier)
        {
            var memoryStream = new MemoryStream();
            var goArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileIdentifier)
                .WithCallbackStream((stream) => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(goArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task DeleteFileAsync(string fileIdentifier)
        {
            var rmArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileIdentifier);
            await _minioClient.RemoveObjectAsync(rmArgs);
        }
    }
}
