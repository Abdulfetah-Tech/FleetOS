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
    [ProducesResponseType(typeof(RegistrationAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(request, cancellationToken);
        return result.Succeeded
            ? Accepted(new RegistrationAcceptedResponse("If this address can be used, follow the instructions sent to it."))
            : BadRequest(new { error = result.Error });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : Unauthorized(new { error = result.Error });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : Unauthorized(new { error = result.Error });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var confirmed = await authenticationService.ConfirmEmailAsync(request, cancellationToken);
        return confirmed ? NoContent() : BadRequest(new { error = "Email confirmation token is invalid or expired." });
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(EmailRequestAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResendConfirmation(EmailRequest request, CancellationToken cancellationToken)
    {
        await authenticationService.ResendConfirmationAsync(request, cancellationToken);
        return Accepted(new EmailRequestAcceptedResponse("If the account requires confirmation, an email will be sent."));
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("password-reset/request")]
    [ProducesResponseType(typeof(EmailRequestAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RequestPasswordReset(EmailRequest request, CancellationToken cancellationToken)
    {
        await authenticationService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(new EmailRequestAcceptedResponse("If the account exists, password reset instructions will be sent."));
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var reset = await authenticationService.ResetPasswordAsync(request, cancellationToken);
        return reset ? NoContent() : BadRequest(new { error = "Password reset token is invalid or expired." });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
