namespace Decisionman.Services.Storage;

static public class StorageExtension
{
    static public IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var storageMode = configuration["StorageMode"] ?? "Local";

        if (storageMode == "MinIO")
        {
            services.AddTransient<IStorageService>(provider =>
                new MinioStorageService(
                    configuration["Minio:Endpoint"] ?? "",
                    configuration["Minio:AccessKey"] ?? "",
                    configuration["Minio:SecretKey"] ?? "",
                    configuration["Minio:BucketName"] ?? "rag-docs"
                ));
        }
        else
        {
            services.AddScoped<IStorageService>(provider => new LocalStorageService("wwwroot/uploads"));
        }
        return services;
    }
}
