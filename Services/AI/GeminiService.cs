using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Pgvector; 

using Decisionman.Data;
using Microsoft.EntityFrameworkCore;

namespace Decisionman.Services.AI;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;

    public GeminiService(HttpClient httpClient, AppDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
    }
    
    private async Task<string> GetApiKeyAsync()
    {
        var setting = await _dbContext.SystemSettings.FindAsync("GeminiApiKey");
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
            throw new Exception("Gemini API Key is not configured in Settings.");
        return setting.Value;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        var apiKey = await GetApiKeyAsync();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-2:embedContent?key={apiKey}";
        
        var requestBody = new
        {
            model = "models/gemini-embedding-2",
            content = new
            {
                parts = new[] { new { text = text } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        
        int maxRetries = 5;
        int delayMs = 2000;

        for (int i = 0;; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                var values = doc.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray().Select(x => x.GetSingle()).ToArray();
                return new Vector(values);
            }
            else if (response.StatusCode == (System.Net.HttpStatusCode)429 && i < maxRetries)
            {
                await Task.Delay(delayMs);
                delayMs *= 2;
                continue;
            }
            
            response.EnsureSuccessStatusCode();
        }
        throw new System.Exception("Unreachable");
    }

    public async Task<string> GenerateAnswerAsync(string prompt, List<string> contextChunks)
    {
        var apiKey = await GetApiKeyAsync();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var combinedContext = string.Join("\n---\n", contextChunks);
        var systemInstruction = "Sana sağlanan belgelerdeki (context) bilgilere dayanarak kullanıcının sorusuna cevap ver. Eğer belgelerde cevap yoksa, 'Bu belge setinde sorulan bilgiye rastlanmadı.' şeklinde cevap ver.";
        
        var finalPrompt = $"Bağlam (Context):\n{combinedContext}\n\nSoru: {prompt}";

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[]
            {
                new {
                    role = "user",
                    parts = new[] { new { text = finalPrompt } }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";
    }
}
