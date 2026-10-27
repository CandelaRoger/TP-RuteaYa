using AutoMapper;
using RuteaYa.MVC.Models;
using RuteaYa.DTOs;

namespace RuteaYa.MVC.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegistroViewModel, RegistroDto>();
        }

    }
}
