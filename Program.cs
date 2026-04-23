using Microsoft.EntityFrameworkCore;
using Decisionman.Data;
using Decisionman.Services.AI;
using Decisionman.Services.Documents;
using Decisionman.Services.RAG;
using Decisionman.Services.Storage;

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
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddSingleton<DocumentProcessor>();
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
app.UseCors(x => 
    x.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();

app.Run();
