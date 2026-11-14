using AutoMapper;
using FitTrack.DTOs.Catalog;
using FitTrack.Model;

namespace FitTrack.Mapping;

public sealed class ExerciseProfile : Profile
{
    public ExerciseProfile()
    {
        CreateMap<Exercise, ExerciseDto>()
            .ForCtorParam(nameof(ExerciseDto.MuscleGroups), options => options.MapFrom(
                exercise => exercise.ExerciseMuscleGroups
                    .Select(link => link.MuscleGroup.Name)
                    .OrderBy(name => name)
                    .ToList()));
    }
}
