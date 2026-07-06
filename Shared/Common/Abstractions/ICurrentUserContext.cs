namespace V3RII.Application.Common.Abstractions;

public interface ICurrentUserContext
{
    long? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Permissions { get; }
}
