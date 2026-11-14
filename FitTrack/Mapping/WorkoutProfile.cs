using AutoMapper;
using FitTrack.DTOs.Workouts;
using FitTrack.Model;

namespace FitTrack.Mapping;

public sealed class WorkoutProfile : Profile
{
    public WorkoutProfile()
    {
        CreateMap<Workout, WorkoutDto>()
            .ForMember(dto => dto.TotalVolume,
                options => options.MapFrom(workout => workout.Sets.Sum(set => set.Reps * set.WeightKg)))
            .ForMember(dto => dto.Sets, options => options.MapFrom(workout => GroupSets(workout.Sets)));
    }

    private static List<WorkoutExerciseDto> GroupSets(IEnumerable<WorkoutSet> sets) =>
        sets.GroupBy(set => new { set.ExerciseId, set.Exercise.Name })
            .OrderBy(group => group.Key.ExerciseId)
            .Select(group => new WorkoutExerciseDto(group.Key.ExerciseId, group.Key.Name,
                group.OrderBy(set => set.SetNumber)
                    .Select(set => new WorkoutSetDto(set.SetNumber, set.Reps, set.WeightKg))
                    .ToList()))
            .ToList();
}
