using System.Threading.Tasks;
using MimeKit;

namespace OsmIntegrator.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, MimeEntity message);
    }
}
