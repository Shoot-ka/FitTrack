using AutoMapper;
using FitTrack.DTOs.Catalog;
using FitTrack.Model;

namespace FitTrack.Mapping;

public sealed class MuscleGroupProfile : Profile
{
    public MuscleGroupProfile()
    {
        CreateMap<MuscleGroup, MuscleGroupDto>();
    }
}
