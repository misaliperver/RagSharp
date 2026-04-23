using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Decisionman.Data;
using Decisionman.Models;
using Decisionman.Services.AI;
using Decisionman.Services.Documents;
using Decisionman.Services.Storage;

namespace Decisionman.Services.RAG;

public class RagService
{
    private readonly AppDbContext _dbContext;
    private readonly GeminiService _geminiService;
    private readonly DocumentProcessor _docProcessor;
    private readonly IStorageService _storageService;

    public RagService(
        AppDbContext dbContext,
        GeminiService geminiService,
        DocumentProcessor docProcessor,
        IStorageService storageService)
    {
        _dbContext = dbContext;
        _geminiService = geminiService;
        _docProcessor = docProcessor;
        _storageService = storageService;
    }

    public async Task<Topic> CreateTopicAsync(string name)
    {
        var topic = new Topic { Name = name };
        _dbContext.Topics.Add(topic);
        await _dbContext.SaveChangesAsync();
        return topic;
    }

    public async Task<List<Topic>> GetTopicsAsync()
    {
        return await _dbContext.Topics
            .OrderByDescending(t => t.ChatMessages.Any() ? t.ChatMessages.Max(m => m.CreatedAt) : t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Document>> GetDocumentsByTopicAsync(int topicId)
    {
        return await _dbContext.Documents.Where(d => d.TopicId == topicId).ToListAsync();
    }

    public async Task<Document> IngestDocumentAsync(int topicId, Stream fileStream, string fileName, string contentType)
    {
        var topic = await _dbContext.Topics.FindAsync(topicId);
        if (topic == null) throw new Exception("Topic not found");

        var storagePath = await _storageService.UploadFileAsync(fileStream, fileName, contentType);

        var document = new Document
        {
            TopicId = topicId,
            FileName = fileName,
            StoragePath = storagePath
        };
        
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        // Parser
        fileStream.Position = 0; 
        var chunks = await _docProcessor.ProcessAndChunkAsync(fileStream, fileName);

        foreach (var chunk in chunks)
        {
            var embedding = await _geminiService.GenerateEmbeddingAsync(chunk);
            
            var docChunk = new DocumentChunk
            {
                DocumentId = document.Id,
                Content = chunk,
                Embedding = embedding
            };
            _dbContext.DocumentChunks.Add(docChunk);
        }

        await _dbContext.SaveChangesAsync();
        // Log event to ChatHistory
        var chatEvent = new ChatMessage
        {
            TopicId = topicId,
            Role = "event",
            Content = $"Doküman eklendi: {fileName}",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(chatEvent);
        await _dbContext.SaveChangesAsync();

        return document;
    }
    
    public async Task<List<ChatMessage>> GetChatHistoryAsync(int topicId)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.TopicId == topicId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<string> AskQuestionAsync(int topicId, string question, List<int>? documentIds = null)
    {
        // Orijinal soruyu veritabanina kaydet
        var userMsg = new ChatMessage
        {
            TopicId = topicId,
            Role = "user",
            Content = question,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(userMsg);
        await _dbContext.SaveChangesAsync();

        // Sorunun vektorunu olusturuyoruz
        var questionEmbedding = await _geminiService.GenerateEmbeddingAsync(question);

        var query = _dbContext.DocumentChunks.Where(c => c.Document!.TopicId == topicId);
        
        if (documentIds != null && documentIds.Any())
        {
            query = query.Where(c => documentIds.Contains(c.DocumentId));
        }

        // Vector DB aramasi
        var topChunks = await query
            .OrderBy(c => c.Embedding!.L2Distance(questionEmbedding)) 
            .Take(5) 
            .Select(c => c.Content)
            .ToListAsync();

        if (!topChunks.Any())
        {
            return "Bu konu altında herhangi bir belge bulunamadığı için cevap veremiyorum.";
        }

        // Gemini API ile cevap uretme
        var answer = await _geminiService.GenerateAnswerAsync(question, topChunks);

        // Gelen cevabi veritabanina kaydet
        var botMsg = new ChatMessage
        {
            TopicId = topicId,
            Role = "gemini",
            Content = answer,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(botMsg);
        await _dbContext.SaveChangesAsync();

        return answer;
    }
}
