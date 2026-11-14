using FitTrack.DTOs.Workouts;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class WorkoutSetRequestValidator : AbstractValidator<WorkoutSetRequest>
{
    public WorkoutSetRequestValidator()
    {
        RuleFor(set => set.ExerciseId).GreaterThan(0);
        RuleFor(set => set.SetNumber).InclusiveBetween(1, 20);
        RuleFor(set => set.Reps).InclusiveBetween(1, 100);
        RuleFor(set => set.WeightKg).InclusiveBetween(0m, 500m);
    }
}
