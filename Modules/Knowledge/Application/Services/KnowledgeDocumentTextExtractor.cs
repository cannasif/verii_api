using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using V3RII.Application.Interfaces;

namespace V3RII.Infrastructure.Services;

public sealed class KnowledgeDocumentTextExtractor : IKnowledgeDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md",
        ".markdown",
        ".pdf",
        ".docx"
    };

    public bool Supports(string fileName) => SupportedExtensions.Contains(Path.GetExtension(fileName));

    public async Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" or ".markdown" => await ExtractPlainTextAsync(stream, cancellationToken),
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            _ => throw new InvalidOperationException("Desteklenmeyen doküman formatı.")
        };
    }

    private static async Task<string> ExtractPlainTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(x => x.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.AppendLine(text);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
