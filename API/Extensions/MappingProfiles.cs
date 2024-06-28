using AutoMapper;
using API.Entities;
using API.DTOs;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<WasteBin, WasteBinDTO>();
        CreateMap<WasteBinDTO, WasteBin>();
    }
}
