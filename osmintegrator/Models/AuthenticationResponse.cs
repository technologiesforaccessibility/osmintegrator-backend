using osmintegrator.Interfaces;

namespace osmintegrator.Models
{
    public class AuthenticationResponse : BaseResponse, IAuthenticationResponse
    {
        public TokenData TokenData { get; set; }
    }
}

