using FitTrack.DTOs.Catalog;
using FitTrack.DTOs.Common;
using FitTrack.Enums;
using FitTrack.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

[ApiController]
[Authorize]
[Route("api/exercises")]
public sealed class ExercisesController(
    ICatalogService catalog,
    IValidator<CreateExerciseRequest> createValidator,
    IValidator<UpdateExerciseRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExerciseDto>>> List(
        CancellationToken ct, int? muscleGroupId = null, Equipment? equipment = null,
        string? search = null, int page = 1, int pageSize = 20)
    {
        var error = RequestChecks.PaginationError(page, pageSize);
        if (error is not null)
        {
            return BadRequest(error);
        }

        if (equipment.HasValue && !Enum.IsDefined(equipment.Value))
        {
            return BadRequest(new ErrorResponse(400, "Invalid equipment."));
        }

        return Ok(await catalog.GetExercisesAsync(muscleGroupId, equipment, search, page, pageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> Get(int id, CancellationToken ct) =>
        Ok(await catalog.GetExerciseAsync(id, ct));

    [Authorize(Roles = nameof(UserRole.Trainer))]
    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> Create(CreateExerciseRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(RequestChecks.ValidationError(validation));
        }

        var exercise = await catalog.AddExerciseAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = exercise.Id }, exercise);
    }

    [Authorize(Roles = nameof(UserRole.Trainer))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> Update(int id, UpdateExerciseRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(RequestChecks.ValidationError(validation));
        }

        return Ok(await catalog.UpdateExerciseAsync(id, request, ct));
    }

    [Authorize(Roles = nameof(UserRole.Trainer))]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await catalog.DeleteExerciseAsync(id, ct);
        return NoContent();
    }
}
