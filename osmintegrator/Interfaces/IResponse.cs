namespace osmintegrator.Interfaces
{
    public interface IResponse
    {
        bool IsSuccess { get; }
        dynamic Result { get; }
        string ErrorMsg { get; }
    }
}
