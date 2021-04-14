using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class TagProfile : Profile
    {
        public TagProfile()
        {
            AllowNullCollections = true;
            //CreateMap<DbTag, Tag>().ReverseMap();
            CreateMap<Tag, DbTag>().ReverseMap();
        }
    }
}