using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EthioFleet.Application.Authentication;
using EthioFleet.Infrastructure.Configuration;

namespace EthioFleet.Infrastructure.Services;

/// <summary>
/// Sends e-mail via SMTP using MailKit (async, has proper logging).
/// Implements EthioFleet.Application.Authentication.IEmailSender
/// </summary>
public sealed class EmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Server) &&
        !string.IsNullOrWhiteSpace(_settings.SenderEmail) &&
        !string.IsNullOrWhiteSpace(_settings.Username);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        mimeMessage.To.Add(new MailboxAddress(message.Recipient, message.Recipient));
        mimeMessage.Subject = message.Subject;
        var body = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.TextBody // Use text body for both for simplicity
        };
        mimeMessage.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_settings.Server, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            client.Send(mimeMessage);
            _logger.LogInformation("Email sent to {Recipient}", message.Recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.Recipient);
            throw; // let the caller decide whether to fail the whole request
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}