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
                // Null-safe: users without photos map to a null PhotoUrl instead of throwing.
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    src.Photos == null ? null : src.Photos.Where(x => x.IsMain).Select(x => x.Url).FirstOrDefault()))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateofBirth.CalculateAge()));
            CreateMap<Photo, PhotoDTO>();

            CreateMap<RegisterDTO, AppUser>();
        }
    }
}
