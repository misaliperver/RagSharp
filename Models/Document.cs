using System;
using System.Collections.Generic;

namespace Decisionman.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Topic? Topic { get; set; }
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
