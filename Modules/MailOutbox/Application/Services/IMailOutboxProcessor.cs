namespace V3RII.Application.Interfaces;

public interface IMailOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
