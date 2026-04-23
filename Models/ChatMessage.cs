using System;

namespace Decisionman.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Role { get; set; } = string.Empty; // "user", "gemini", "event"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Topic? Topic { get; set; }
}
