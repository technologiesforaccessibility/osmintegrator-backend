namespace OsmIntegrator.Interfaces {
    public interface IPresentResponse<TResult> 
    {
        TResult Content { get;}

        void Present(AUseCaseResponse result);        
    }
}