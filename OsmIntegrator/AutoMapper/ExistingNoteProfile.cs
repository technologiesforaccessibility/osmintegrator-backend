using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class ExistingNoteProfile : Profile
    {
        public ExistingNoteProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbNote, ExistingNote>().ReverseMap();
        }
    }
}