using System.Threading.Tasks;

namespace OsmIntegrator.Interfaces
{
    public interface IEmailService
    {
        void Send(string to, string subject, string message);
        Task SendEmailAsync(string to, string subject, string message);
    }
}
