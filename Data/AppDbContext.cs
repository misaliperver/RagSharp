using Microsoft.EntityFrameworkCore;
using Decisionman.Models;

namespace Decisionman.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Topic> Topics { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // PostgreSQL vector extension (pgvector)
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<Topic>()
                .HasMany(t => t.Documents)
                .WithOne(d => d.Topic)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Topic>()
                .HasMany(t => t.ChatMessages)
                .WithOne(m => m.Topic)
                .HasForeignKey(m => m.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SystemSetting>()
                .HasKey(s => s.Key);
        }
    }
}
