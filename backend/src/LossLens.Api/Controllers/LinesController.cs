using LossLens.Api.Auth;
using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/lines")]
public sealed class LinesController(LinesService lines, IUserContextProvider users) : ControllerBase
{
    /// <summary>Lines the caller may see, with their stations in line order.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<LineDto>> Get(CancellationToken ct) =>
        await lines.GetLinesAsync(await users.GetCurrentAsync(ct), ct);
}
