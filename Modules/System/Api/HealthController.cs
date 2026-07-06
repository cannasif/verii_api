using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V3RII.Application.Common;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(V3RiiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new
        {
            status = canConnect ? "ready" : "degraded",
            database = canConnect
        }));
    }
}
