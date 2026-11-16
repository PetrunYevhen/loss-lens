using LossLens.Api.Auth;
using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/plan-vs-actual")]
public sealed class PlanVsActualController(PlanService plan, IUserContextProvider users) : ControllerBase
{
    /// <summary>Planned and good units per hour. Hours that have not started have no actual value.</summary>
    [HttpGet]
    public Task<IReadOnlyList<PlanVsActualPointDto>> Get(
        [BindRequired] DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct) =>
        plan.GetAsync(users.GetCurrent(), date, lineId, shiftId, ct);
}
