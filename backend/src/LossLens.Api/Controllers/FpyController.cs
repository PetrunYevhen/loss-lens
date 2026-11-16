using LossLens.Api.Auth;
using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/fpy")]
public sealed class FpyController(FpyService fpy, IUserContextProvider users) : ControllerBase
{
    /// <summary>Daily rolled throughput yield for a date range, up to today.</summary>
    [HttpGet("trend")]
    public async Task<IReadOnlyList<FpyTrendPointDto>> GetTrend(
        [BindRequired] DateOnly from, [BindRequired] DateOnly to, Guid? lineId, CancellationToken ct) =>
        await fpy.GetTrendAsync(await users.GetCurrentAsync(ct), from, to, lineId, ct);

    /// <summary>First pass yield per station for one day (per type when several lines are combined).</summary>
    [HttpGet("stations")]
    public async Task<IReadOnlyList<StationFpyDto>> GetStations(
        [BindRequired] DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct) =>
        await fpy.GetStationsAsync(await users.GetCurrentAsync(ct), date, lineId, shiftId, ct);
}
