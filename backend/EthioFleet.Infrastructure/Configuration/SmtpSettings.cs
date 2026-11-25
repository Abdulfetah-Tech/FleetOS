namespace EthioFleet.Infrastructure.Configuration;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Server { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string SenderName { get; init; } = "EthioFleet";
    public string SenderEmail { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}