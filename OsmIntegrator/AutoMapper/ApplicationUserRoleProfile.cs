using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class ApplicationUserRoleProfile : Profile
    {
        public ApplicationUserRoleProfile()
        {
            AllowNullCollections = true;
            CreateMap<ApplicationUserRole, string>().ConstructUsing(x => x.Role.Name);
        }
    }
}