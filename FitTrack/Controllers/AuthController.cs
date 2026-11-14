using FitTrack.DTOs.Auth;
using FitTrack.DTOs.Common;
using FitTrack.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FitTrack.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredUserDto>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationError(validation);
        }

        var user = await authService.RegisterAsync(request, cancellationToken);
        if (user is null)
        {
            return Conflict(new ErrorResponse(409, "Email already exists."));
        }

        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationError(validation);
        }

        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized(new ErrorResponse(401, "Invalid email or password."));
        }

        return Ok(response);
    }

    private BadRequestObjectResult ValidationError(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors
            .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return BadRequest(new ErrorResponse(400, "Validation failed", errors));
    }
}
