using LossLens.Api.Auth;
using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/defects")]
public sealed class DefectsController(DefectsService defects, IUserContextProvider users) : ControllerBase
{
    /// <summary>Pareto of defect causes and DPMO for a date range.</summary>
    [HttpGet("pareto")]
    public async Task<ParetoResponseDto> GetPareto(
        [BindRequired] DateOnly from,
        [BindRequired] DateOnly to,
        Guid? lineId,
        Guid? stationId,
        Guid? shiftId,
        CancellationToken ct) =>
        await defects.GetParetoAsync(await users.GetCurrentAsync(ct), from, to, lineId, stationId, shiftId, ct);
}
