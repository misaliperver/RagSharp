using Microsoft.AspNetCore.Mvc;
using Decisionman.Services.RAG;

namespace Decisionman.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RAGController : ControllerBase
{
    private readonly RagService _ragService;

    public RAGController(RagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
    {
        var topic = await _ragService.CreateTopicAsync(request.Name);
        return Ok(new { topic.Id, topic.Name, topic.CreatedAt });
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics()
    {
        var topics = await _ragService.GetTopicsAsync();
        var result = topics.Select(t => new { t.Id, t.Name, t.CreatedAt }).ToList();
        return Ok(result);
    }

    [HttpGet("topics/{topicId}/documents")]
    public async Task<IActionResult> GetDocuments(int topicId)
    {
        var docs = await _ragService.GetDocumentsByTopicAsync(topicId);
        var result = docs.Select(d => new { d.Id, d.FileName, d.UploadedAt }).ToList();
        return Ok(result);
    }

    [HttpPost("topics/{topicId}/documents")]
    public async Task<IActionResult> UploadDocument(int topicId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        try
        {
            var doc = await _ragService.IngestDocumentAsync(topicId, stream, file.FileName, file.ContentType);
            return Ok(new { doc.Id, doc.TopicId, doc.FileName, doc.StoragePath, doc.UploadedAt });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("topics/{topicId}/chat")]
    public async Task<IActionResult> GetChatHistory(int topicId)
    {
        var history = await _ragService.GetChatHistoryAsync(topicId);
        var result = history.Select(h => new { h.Id, h.Role, h.Content, h.CreatedAt });
        return Ok(result);
    }

    [HttpPost("topics/{topicId}/chat")]
    public async Task<IActionResult> Chat(int topicId, [FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        try
        {
            var answer = await _ragService.AskQuestionAsync(topicId, request.Question, request.DocumentIds);
            return Ok(new { answer });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

public class CreateTopicRequest
{
    public string Name { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Question { get; set; } = string.Empty;
    public System.Collections.Generic.List<int>? DocumentIds { get; set; }
}
