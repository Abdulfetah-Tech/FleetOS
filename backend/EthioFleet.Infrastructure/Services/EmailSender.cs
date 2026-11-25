using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EthioFleet.Infrastructure.Configuration;
namespace EthioFleet.Infrastructure.Services;
/// <summary>
/// Sends e‑mail via SMTP using MailKit (async, has proper logging).
/// </summary>
public sealed class EmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailSender> _logger;
    public EmailSender(IOptions<SmtpSettings> settings,
                       ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }
    public async Task SendAsync(string toEmail,
                                string toName,
                                string subject,
                                string htmlBody,
                                string? plainTextBody = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        var body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainTextBody ?? "Please view this e‑mail in an HTML‑compatible client."
        };
        message.Body = body.ToMessageBody();
        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_settings.Server,
                                      _settings.Port,
                                      socketOptions);
            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username,
                                              _settings.Password);
            }
            await client.SendAsync(message);
            _logger.LogInformation("Email sent to {Recipient}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", toEmail);
            throw; // let the caller decide whether to fail the whole request
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
