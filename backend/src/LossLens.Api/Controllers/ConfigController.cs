using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
public sealed class ConfigController(ConfigService config) : ControllerBase
{
    /// <summary>Current RAG thresholds. The same for every role.</summary>
    [HttpGet("thresholds")]
    public ThresholdsDto GetThresholds() => config.GetThresholdIds();
}
