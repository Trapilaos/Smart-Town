using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDTO>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateofBirth.CalculateAge()));
            CreateMap<Photo, PhotoDTO>();

            CreateMap<WeatherResponse, WeatherDTO>()
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.current.condition.text))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.current.temp_c + " °C"))
                .ForMember(dest => dest.Humidity, opt => opt.MapFrom(src => src.current.humidity + " %"));
        }
        

    }
}