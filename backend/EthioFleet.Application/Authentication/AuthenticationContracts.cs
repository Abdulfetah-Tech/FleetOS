namespace EthioFleet.Application.Authentication;

public sealed record RegisterRequest(string Email, string UserName, string Password, string OrganizationName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RevokeTokenRequest(string RefreshToken);
public sealed record AuthenticationResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
public sealed record AuthenticatedUser(Guid Id, Guid OrganizationId, string Email, string UserName, IReadOnlyCollection<string> Roles);
public sealed record AuthResult(bool Succeeded, AuthenticationResponse? Tokens, AuthenticatedUser? User, string? Error)
{
    public static AuthResult Failure(string error) => new(false, null, null, error);
    public static AuthResult Success(AuthenticationResponse tokens, AuthenticatedUser user) => new(true, tokens, user, null);
}

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(RevokeTokenRequest request, Guid currentUserId, CancellationToken cancellationToken);
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
