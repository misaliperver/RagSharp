using Pgvector;

namespace Decisionman.Models
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Vector? Embedding { get; set; } 
        
        public Document? Document { get; set; }
    }
}
