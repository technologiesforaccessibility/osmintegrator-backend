namespace OsmIntegrator.Interfaces {
    public interface IPresentResponse<TResult> 
    {
        public TResult Content { get;}

        public void Present(AUseCaseResponse result);        
    }
}