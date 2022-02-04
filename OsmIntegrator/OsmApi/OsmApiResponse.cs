namespace OsmIntegrator.OsmApi
{
  public class OsmApiResponse
  {
    public OsmApiStatusCode Status { get; }

    public object Data { get; }

    private OsmApiResponse(object data)
    {
      Status = OsmApiStatusCode.Ok;
      Data = data;
    }

    private OsmApiResponse(OsmApiStatusCode status)
    {
      Status = status;
    }

    public static OsmApiResponse Success(object data) => new OsmApiResponse(data);

    public static OsmApiResponse Error(OsmApiStatusCode status) => new OsmApiResponse(status);
  }
}