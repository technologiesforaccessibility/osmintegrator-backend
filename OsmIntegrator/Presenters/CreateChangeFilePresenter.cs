using System.IO;
using System.Net;
using System.Net.Http;
using OsmIntegrator.DomainUseCases;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Presenters
{

    public class CreateChangeFileWebPresenter : PresentResponse<StreamContent>
    {
        public StreamContent Content { get; private set;}

        public void Present(AUseCaseResponse result)
        {   
            var r = (CreateChangeFileResponse)result;            
            Content = new StreamContent(r.XmlStream);            
        }
    }
}