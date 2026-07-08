namespace V3RII.Application.Interfaces;

public interface IKnowledgeDocumentTextExtractor
{
    bool Supports(string fileName);
    Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
