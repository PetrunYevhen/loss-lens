using LossLens.Application.Dtos;
using LossLens.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LossLens.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Signs in and returns an access token. Wrong credentials give 401.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<LoginResultDto> Login(LoginRequest request, CancellationToken ct) =>
        auth.LoginAsync(request.Username, request.Password, ct);
}

public sealed record LoginRequest(string Username, string Password);
