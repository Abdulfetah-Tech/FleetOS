namespace EthioFleet.Infrastructure.Configuration;
/// <summary>
/// SMTP configuration – values are read from appsettings.json.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Server   { get; set; } = "";
    public int    Port     { get; set; } = 587;   // 587 = STARTTLS, 465 = SSL
    public string SenderName  { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public string Username    { get; set; } = "";
    public string Password    { get; set; } = "";
}
