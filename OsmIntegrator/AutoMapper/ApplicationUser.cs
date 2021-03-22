using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class ApplicationUserProfile : Profile
    {
        public ApplicationUserProfile()
        {
            AllowNullCollections = true;
            CreateMap<ApplicationUser, User>().ReverseMap();
        }
    }
}