using System.Text;
using ExcelDataReader;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Decisionman.Services.Documents;

public class DocumentProcessor
{
    public DocumentProcessor()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<List<string>> ProcessAndChunkAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string fullText = string.Empty;

        if (extension == ".pdf")
        {
            fullText = ExtractTextFromPdf(fileStream);
        }
        else if (extension == ".xlsx" || extension == ".xls")
        {
            fullText = ExtractTextFromExcel(fileStream);
        }
        else
        {
            using var reader = new StreamReader(fileStream);
            fullText = await reader.ReadToEndAsync();
        }

        return ChunkText(fullText, 4000, 400);
    }

    private string ExtractTextFromPdf(Stream fileStream)
    {
        fileStream.Position = 0;
        using var pdfDocument = PdfDocument.Open(fileStream);
        var sb = new StringBuilder();
        
        foreach (var page in pdfDocument.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private string ExtractTextFromExcel(Stream fileStream)
    {
        fileStream.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var result = reader.AsDataSet();
        var sb = new StringBuilder();

        foreach (System.Data.DataTable table in result.Tables)
        {
            sb.AppendLine($"Sheet: {table.TableName}");
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var rowContents = new List<string>();
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    rowContents.Add(table.Rows[i][j]?.ToString() ?? "");
                }
                sb.AppendLine(string.Join(" | ", rowContents));
            }
        }

        return sb.ToString();
    }

    private List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int i = 0;
        while (i < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, len));
            i += chunkSize - overlap;
        }

        return chunks;
    }
}
