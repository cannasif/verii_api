using V3RII.Application.Common;
using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IMailOutboxService
{
    Task<PagedResult<MailOutboxItemDto>> GetAllAsync(int page, int pageSize, string? status, string? search, CancellationToken cancellationToken = default);
    Task<MailOutboxSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<MailOutboxItemDto> RetryAsync(long id, CancellationToken cancellationToken = default);
}
