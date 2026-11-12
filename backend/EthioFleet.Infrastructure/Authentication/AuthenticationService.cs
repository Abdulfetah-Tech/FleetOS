using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EthioFleet.Application.Authentication;
using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Organizations;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EthioFleet.Infrastructure.Authentication;

public sealed class AuthenticationService(
    AppDbContext dbContext,
    UserManager<AppIdentityUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<JwtOptions> jwtOptions,
    IEmailSender emailSender,
    IHttpContextAccessor httpContextAccessor,
    IClock clock) : IAuthenticationService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            return RegistrationResult.Failure("Email, username, password, and organization name are required.");
        }

        EnsureEmailDeliveryIsConfigured();
        var email = request.Email.Trim();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var existingUser = await dbContext.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return RegistrationResult.Success();
        }

        await IdentitySeeder.EnsureRoleAsync(roleManager, "CompanyAdministrator");
        var organization = new Organization(request.OrganizationName, CreateOrganizationCode(), email, string.Empty, string.Empty);
        var user = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName.Trim(),
            Email = email,
            OrganizationId = organization.Id,
            EmailConfirmed = false,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RegistrationResult.Failure(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "CompanyAdministrator");
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RegistrationResult.Failure("Could not assign the company administrator role.");
        }

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendAsync(new EmailMessage(
            email,
            "Confirm your EthioFleet account",
            $"Use this token to confirm your email address:{Environment.NewLine}{confirmationToken}"), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return RegistrationResult.Success();
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            return false;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return false;
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded;
    }

    public async Task ResendConfirmationAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        EnsureEmailDeliveryIsConfigured();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        await SendConfirmationEmailAsync(user, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        EnsureEmailDeliveryIsConfigured();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.EmailConfirmed)
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailSender.SendAsync(new EmailMessage(
            user.Email!,
            "Reset your EthioFleet password",
            $"Use this token to reset your password:{Environment.NewLine}{token}"), cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return false;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.EmailConfirmed)
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var activeTokens = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = clock.UtcNow;
            token.RevokedByIp = GetClientIp();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult.Failure("Email and password are required.");
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.OrganizationId == Guid.Empty || !user.EmailConfirmed || user.LockoutEnd > clock.UtcNow)
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthResult.Failure("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult.Failure("Refresh token is required.");
        }

        var stored = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == HashToken(request.RefreshToken), cancellationToken);
        if (stored is null)
        {
            return AuthResult.Failure("Invalid refresh token.");
        }

        if (stored.IsRevoked)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
            return AuthResult.Failure("Refresh token reuse detected. This session has been revoked.");
        }

        if (stored.ExpiresAt <= clock.UtcNow)
        {
            return AuthResult.Failure("Refresh token has expired.");
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || user.OrganizationId != stored.OrganizationId || !user.EmailConfirmed || user.LockoutEnd > clock.UtcNow)
        {
            stored.RevokedAt = clock.UtcNow;
            stored.RevokedByIp = GetClientIp();
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthResult.Failure("The account is unavailable.");
        }

        stored.RevokedAt = clock.UtcNow;
        stored.RevokedByIp = GetClientIp();
        return await CreateSessionAsync(user, cancellationToken, stored);
    }

    public async Task<bool> RevokeAsync(RevokeTokenRequest request, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return false;
        }

        var stored = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == HashToken(request.RefreshToken)
                && token.UserId == currentUserId, cancellationToken);
        if (stored is null || stored.IsRevoked)
        {
            return false;
        }

        stored.RevokedAt = clock.UtcNow;
        stored.RevokedByIp = GetClientIp();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResult> CreateSessionAsync(
        AppIdentityUser user,
        CancellationToken cancellationToken,
        RefreshToken? rotatedFrom = null,
        bool transactionAlreadyOpen = false)
    {
        var roles = await userManager.GetRolesAsync(user);
        var now = clock.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var stored = new RefreshToken
        {
            UserId = user.Id,
            OrganizationId = user.OrganizationId,
            FamilyId = rotatedFrom?.FamilyId ?? Guid.NewGuid(),
            TokenHash = HashToken(refresh),
            CreatedAt = now,
            ExpiresAt = refreshExpires,
            CreatedByIp = GetClientIp()
        };

        await using var transaction = transactionAlreadyOpen
            ? null
            : await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.RefreshTokens.Add(stored);
        if (rotatedFrom is not null)
        {
            rotatedFrom.RevokedAt = now;
            rotatedFrom.RevokedByIp = GetClientIp();
            rotatedFrom.ReplacedByTokenId = stored.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("organization_id", user.OrganizationId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (roles.Contains("CompanyAdministrator", StringComparer.Ordinal)
            && !roles.Contains("CompanyOwner", StringComparer.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, "CompanyOwner"));
        }
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            notBefore: now.UtcDateTime, expires: accessExpires.UtcDateTime, signingCredentials: credentials);

        var userInfo = new AuthenticatedUser(user.Id, user.OrganizationId, user.Email ?? string.Empty,
            user.UserName ?? string.Empty, roles.ToArray());
        return AuthResult.Success(new AuthenticationResponse(new JwtSecurityTokenHandler().WriteToken(jwt),
            accessExpires, refresh, refreshExpires), userInfo);
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null && token.ExpiresAt > clock.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = clock.UtcNow;
            token.RevokedByIp = GetClientIp();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendConfirmationEmailAsync(AppIdentityUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendAsync(new EmailMessage(
            user.Email!,
            "Confirm your EthioFleet account",
            $"Use this token to confirm your email address:{Environment.NewLine}{token}"), cancellationToken);
    }

    private void EnsureEmailDeliveryIsConfigured()
    {
        if (!emailSender.IsConfigured)
        {
            throw new EmailDeliveryNotConfiguredException();
        }
    }

    private string GetClientIp() => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string CreateOrganizationCode() => $"ORG-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
}
