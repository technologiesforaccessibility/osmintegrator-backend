namespace OsmIntegrator.Interfaces {
    public interface PresentResponse<TResult> 
    {
        public TResult Content { get;}

        public void Present(AUseCaseResponse result);        
    }
}