using System.Threading.Tasks;
using MimeKit;

namespace OsmIntegrator.Interfaces
{
    public interface IEmailService
    {
        void Send(string to, string subject, string message);
        void Send(MimeMessage message);
        Task SendEmailAsync(string to, string subject, string message);
        Task SendEmailAsync(MimeMessage message);

        string BuildSubject(string subject);

        string BuildServerName(bool useHtml);
    }
}
