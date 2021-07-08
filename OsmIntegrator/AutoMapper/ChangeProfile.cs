using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Controllers;
using OsmIntegrator.Database.Models;
using OsmIntegrator.DomainUseCases;

namespace OsmIntegrator.AutoMapper
{
    public class ChangeProfile : Profile
    {
        public ChangeProfile()
        {
            AllowNullCollections = true;
            CreateMap<CreateChangeFileInputDto, CreateChangeFileRequestInput>().ReverseMap();
        }
    }
}