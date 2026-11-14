using FitTrack.DTOs.Auth;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
    }
}
