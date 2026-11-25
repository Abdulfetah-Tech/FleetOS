using System.Threading.Tasks;
namespace EthioFleet.Infrastructure.Services;
/// <summary>
/// Abstract contract for sending e‑mail.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail,
                   string toName,
                   string subject,
                   string htmlBody,
                   string? plainTextBody = null);
}
