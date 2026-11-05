using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using System.Security.Claims;
using EthioFleet.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(new { error = result.Error });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : Unauthorized(new { error = result.Error });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : Unauthorized(new { error = result.Error });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var revoked = await authenticationService.RevokeAsync(request, parsedUserId, cancellationToken);
        return revoked ? NoContent() : NotFound();
    }
}
