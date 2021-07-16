using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class NewNoteProfile : Profile
    {
        public NewNoteProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbNote, NewNote>().ReverseMap();
        }
    }
}