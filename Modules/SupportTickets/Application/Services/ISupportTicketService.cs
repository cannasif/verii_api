using V3RII.Application.Common;
using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface ISupportTicketService
{
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResult<SupportTicketDto>> GetAllAsync(int page, int pageSize, string? status, string? product, string? search, CancellationToken cancellationToken = default);
    Task<SupportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<SupportTicketDto> UpdateStatusAsync(long id, UpdateSupportTicketStatusRequestDto request, CancellationToken cancellationToken = default);
}
