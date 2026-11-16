using LossLens.Api.Auth;
using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/kpi")]
public sealed class KpiController(KpiService kpi, IUserContextProvider users) : ControllerBase
{
    /// <summary>The four KPI cards for one day, with status and change against the previous day.</summary>
    [HttpGet("summary")]
    public Task<KpiSummaryDto> GetSummary(
        [BindRequired] DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct) =>
        kpi.GetSummaryAsync(users.GetCurrent(), date, lineId, shiftId, ct);
}
