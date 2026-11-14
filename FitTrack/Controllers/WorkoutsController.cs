using System.Security.Claims;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Workouts;
using FitTrack.Enums;
using FitTrack.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

[ApiController]
[Authorize]
[Route("api/workouts")]
public sealed class WorkoutsController(
    IWorkoutService workouts,
    IValidator<CreateWorkoutRequest> createValidator,
    IValidator<UpdateWorkoutRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Member))]
    public async Task<ActionResult<PagedResult<WorkoutDto>>> List(
        CancellationToken ct, DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = 20)
    {
        var error = RequestChecks.PaginationError(page, pageSize);
        if (error is not null)
        {
            return BadRequest(error);
        }

        if (from > to)
        {
            return BadRequest(new ErrorResponse(400, "The from date must be on or before the to date."));
        }

        return Ok(await workouts.ListAsync(UserId, from, to, page, pageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutDto>> Get(int id, CancellationToken ct) =>
        Ok(await workouts.GetAsync(id, UserId, User.IsInRole(nameof(UserRole.Trainer)), ct));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Member))]
    public async Task<ActionResult<WorkoutDto>> Create(CreateWorkoutRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(RequestChecks.ValidationError(validation));
        }

        var workout = await workouts.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = workout.Id }, workout);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Member))]
    public async Task<ActionResult<WorkoutDto>> Update(int id, UpdateWorkoutRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(RequestChecks.ValidationError(validation));
        }

        return Ok(await workouts.UpdateAsync(id, UserId, request, ct));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Member))]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await workouts.DeleteAsync(id, UserId, ct);
        return NoContent();
    }

    private int UserId => int.Parse(User.FindFirstValue("sub")!);
}
