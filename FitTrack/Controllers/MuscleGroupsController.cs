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
[Route("api/muscle-groups")]
public sealed class MuscleGroupsController(
    ICatalogService catalog,
    IValidator<CreateMuscleGroupRequest> validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MuscleGroupDto>>> List(
        CancellationToken ct, int page = 1, int pageSize = 20)
    {
        var error = CatalogRequestChecks.PaginationError(page, pageSize);
        if (error is not null)
        {
            return BadRequest(error);
        }

        return Ok(await catalog.GetMuscleGroupsAsync(page, pageSize, ct));
    }

    [Authorize(Roles = nameof(UserRole.Trainer))]
    [HttpPost]
    public async Task<ActionResult<MuscleGroupDto>> Create(CreateMuscleGroupRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CatalogRequestChecks.ValidationError(validation));
        }

        return StatusCode(StatusCodes.Status201Created, await catalog.AddMuscleGroupAsync(request, ct));
    }
}
