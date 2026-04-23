using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Decisionman.Data;
using Decisionman.Services.AI;
using Decisionman.Services.Documents;
using Decisionman.Services.RAG;
using Decisionman.Services.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// Configure Services
builder.Services.AddSingleton<DocumentProcessor>();

builder.Services.AddHttpClient<GeminiService>();
// No need for a custom factory since GeminiService correctly injects AppDbContext now

// Configure Storage (Can be switched to MinIO via configuration in future)
var storageMode = builder.Configuration["StorageMode"] ?? "Local";
if (storageMode == "MinIO")
{
    builder.Services.AddTransient<IStorageService>(provider => 
        new MinioStorageService(
            builder.Configuration["Minio:Endpoint"] ?? "",
            builder.Configuration["Minio:AccessKey"] ?? "",
            builder.Configuration["Minio:SecretKey"] ?? "",
            builder.Configuration["Minio:BucketName"] ?? "rag-docs"
        ));
}
else
{
    builder.Services.AddScoped<IStorageService>(provider => new LocalStorageService("wwwroot/uploads"));
}

builder.Services.AddScoped<RagService>();

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global CORS rule for local testing from index.html if needed (though it's served from same domain)
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();

app.Run();
