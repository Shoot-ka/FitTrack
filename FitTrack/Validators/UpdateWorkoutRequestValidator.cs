using FitTrack.DTOs.Workouts;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class UpdateWorkoutRequestValidator : AbstractValidator<UpdateWorkoutRequest>
{
    public UpdateWorkoutRequestValidator()
    {
        RuleFor(workout => workout.Date).Must(date =>
        {
            var now = DateTimeOffset.UtcNow;
            return date >= now.AddYears(-1) && date <= now;
        }).WithMessage("Date must be within the past year and cannot be in the future.");
        RuleFor(workout => workout.Title).NotEmpty()
            .Must(title => title?.Trim().Length is >= 2 and <= 80)
            .WithMessage("Title must be between 2 and 80 characters.");
        RuleFor(workout => workout.Notes).MaximumLength(1000);
        RuleFor(workout => workout.DurationMinutes).InclusiveBetween(1, 600);
        RuleFor(workout => workout.Sets)
            .Must(sets => sets?.Count is >= 1 and <= 100)
            .WithMessage("Provide between 1 and 100 sets.")
            .Must(sets => sets is null || sets.Where(set => set is not null)
                .GroupBy(set => (set!.ExerciseId, set.SetNumber)).All(group => group.Count() == 1))
            .WithMessage("Set numbers must be unique within each exercise.");
        RuleForEach(workout => workout.Sets).NotNull().SetValidator(new WorkoutSetRequestValidator()!);
    }
}
