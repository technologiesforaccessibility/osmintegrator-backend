using AutoMapper;
namespace OsmIntegrator.AutoMapper
{
    public class TagProfile : Profile
    {
        public TagProfile()
        {
            AllowNullCollections = true;
            CreateMap<OsmIntegrator.Database.Models.JsonFields.Tag, OsmIntegrator.ApiModels.Tag>().ReverseMap();
        }
    }
}