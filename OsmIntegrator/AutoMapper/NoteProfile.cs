using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class NoteProfile : Profile
    {
        public NoteProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbNote, Note>().ReverseMap();
        }
    }
}