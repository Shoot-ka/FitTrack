using AutoMapper;
using FitTrack.DTOs.Trainer;
using FitTrack.Model;

namespace FitTrack.Mapping;

public sealed class ClientProfile : Profile
{
    public ClientProfile()
    {
        CreateMap<TrainerClient, ClientDto>()
            .ForMember(dto => dto.FullName, options => options.MapFrom(link => link.Client.FullName))
            .ForMember(dto => dto.Email, options => options.MapFrom(link => link.Client.Email))
            .ForMember(dto => dto.LastWorkoutDate, options => options.MapFrom(
                link => link.Client.Workouts.Max(workout => (DateTime?)workout.Date)));
    }
}
