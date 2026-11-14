using System.Security.Claims;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Trainer;
using FitTrack.DTOs.Workouts;
using FitTrack.Enums;
using FitTrack.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Trainer))]
[Route("api/trainer/clients")]
public sealed class TrainerClientsController(
    ITrainerService trainerService,
    IValidator<AddClientRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClientDto>> AddClient(AddClientRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(RequestChecks.ValidationError(validation));
        }

        var client = await trainerService.AddClientAsync(TrainerId, request, ct);
        return StatusCode(StatusCodes.Status201Created, client);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClientDto>>> ListClients(
        CancellationToken ct, int page = 1, int pageSize = 20)
    {
        var error = RequestChecks.PaginationError(page, pageSize);
        if (error is not null)
        {
            return BadRequest(error);
        }

        return Ok(await trainerService.ListClientsAsync(TrainerId, page, pageSize, ct));
    }

    [HttpGet("{clientId:int}/workouts")]
    public async Task<ActionResult<PagedResult<WorkoutDto>>> ListWorkouts(
        int clientId, CancellationToken ct, DateOnly? from = null, DateOnly? to = null,
        int page = 1, int pageSize = 20)
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

        return Ok(await trainerService.ListClientWorkoutsAsync(
            TrainerId, clientId, from, to, page, pageSize, ct));
    }

    private int TrainerId => int.Parse(User.FindFirstValue("sub")!);
}
