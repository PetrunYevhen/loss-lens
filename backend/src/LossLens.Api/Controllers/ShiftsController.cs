using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/shifts")]
public sealed class ShiftsController(ShiftsService shifts) : ControllerBase
{
    /// <summary>All shifts. Their ids are used by the shiftId filter of the other endpoints.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ShiftDto>> Get(CancellationToken ct) => shifts.GetShiftsAsync(ct);
}
