namespace osmintegrator.Interfaces
{
    public interface IEmailService
    {
        void Send(string to, string subject, string message);
    }
}
