using EthioFleet.Application.Authentication;

namespace EthioFleet.Infrastructure.Services;

public sealed class UnconfiguredEmailSender : IEmailSender
{
    public bool IsConfigured => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new EmailDeliveryNotConfiguredException();
}
