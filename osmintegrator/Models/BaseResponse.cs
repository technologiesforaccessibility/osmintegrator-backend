using osmintegrator.Interfaces;

namespace osmintegrator.Models
{
    public class BaseResponse : IResponse
    {
        public bool IsSuccess { get; set; }
        public object Result { get; set; }
        public string ErrorMsg { get; set; }
    }
}
