namespace EthioFleet.Application.Authentication;

/// <summary>Creates a company organization and its first administrator.</summary>
public sealed record RegisterRequest(string Email, string UserName, string Password, string OrganizationName);

/// <summary>Authenticates an account using its email address and password.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Rotates an account's refresh token.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>Revokes an account's refresh token.</summary>
public sealed record RevokeTokenRequest(string RefreshToken);

/// <summary>Requests a confirmation email or password reset email for an account.</summary>
public sealed record EmailRequest(string Email);

/// <summary>Confirms an account email using the one-time token sent to that address.</summary>
public sealed record ConfirmEmailRequest(string Email, string Token);

/// <summary>Changes an account password using the one-time reset token sent by email.</summary>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>Confirms that registration was accepted and an email may be sent.</summary>
public sealed record RegistrationAcceptedResponse(string Message);

/// <summary>Returns a generic acceptance message for account email requests.</summary>
public sealed record EmailRequestAcceptedResponse(string Message);

/// <summary>Contains access and refresh credentials with their expiry times.</summary>
public sealed record AuthenticationResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

/// <summary>Describes the authenticated account and its organization roles.</summary>
public sealed record AuthenticatedUser(Guid Id, Guid OrganizationId, string Email, string UserName, IReadOnlyCollection<string> Roles);
public sealed record RegistrationResult(bool Succeeded, string? Error)
{
    public static RegistrationResult Failure(string error) => new(false, error);
    public static RegistrationResult Success() => new(true, null);
}

public sealed record EmailMessage(string Recipient, string Subject, string TextBody);

/// <summary>Sends account lifecycle messages through a configured email provider.</summary>
public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed class EmailDeliveryNotConfiguredException()
    : Exception("Email delivery is not configured. Configure an email provider before using account email workflows.");

/// <summary>Represents an authentication operation result.</summary>
public sealed record AuthResult(bool Succeeded, AuthenticationResponse? Tokens, AuthenticatedUser? User, string? Error)
{
    public static AuthResult Failure(string error) => new(false, null, null, error);
    public static AuthResult Success(AuthenticationResponse tokens, AuthenticatedUser user) => new(true, tokens, user, null);
}

public interface IAuthenticationService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(RevokeTokenRequest request, Guid currentUserId, CancellationToken cancellationToken);
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken);
    Task ResendConfirmationAsync(EmailRequest request, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;
}
