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
    public Task<IReadOnlyList<FpyTrendPointDto>> GetTrend(
        [BindRequired] DateOnly from, [BindRequired] DateOnly to, Guid? lineId, CancellationToken ct) =>
        fpy.GetTrendAsync(users.GetCurrent(), from, to, lineId, ct);

    /// <summary>First pass yield per station for one day (per type when several lines are combined).</summary>
    [HttpGet("stations")]
    public Task<IReadOnlyList<StationFpyDto>> GetStations(
        [BindRequired] DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct) =>
        fpy.GetStationsAsync(users.GetCurrent(), date, lineId, shiftId, ct);
}
