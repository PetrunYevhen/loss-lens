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
    public Task<IReadOnlyList<LineDto>> Get(CancellationToken ct) =>
        lines.GetLinesAsync(users.GetCurrent(), ct);
}
