using osmintegrator.Models;

namespace osmintegrator.Interfaces
{
    public interface IAuthenticationResponse : IResponse
    {
        TokenData TokenData { get; set; }
    }
}
