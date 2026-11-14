using AutoMapper;
using AutoMapper.QueryableExtensions;
using FitTrack.Data;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Trainer;
using FitTrack.DTOs.Workouts;
using FitTrack.Enums;
using FitTrack.Exceptions;
using FitTrack.Model;
using FitTrack.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed class TrainerService(AppDbContext db, IMapper mapper, IWorkoutService workouts) : ITrainerService
{
    public async Task<ClientDto> AddClientAsync(int trainerId, AddClientRequest request, CancellationToken ct)
    {
        var email = request.Email!.Trim().ToLowerInvariant();
        var clientId = await db.Users.Where(user => user.Email == email && user.Role == UserRole.Member)
            .Select(user => (int?)user.Id)
            .SingleOrDefaultAsync(ct);

        if (clientId is null)
        {
            throw new ApiException(new ErrorResponse(400, "Validation failed",
                new Dictionary<string, string[]> { ["email"] = ["Email must belong to a registered Member."] }));
        }

        if (await HasClientAsync(trainerId, clientId.Value, ct))
        {
            throw AlreadyLinked();
        }

        db.TrainerClients.Add(new TrainerClient
        {
            TrainerId = trainerId,
            ClientId = clientId.Value,
            Since = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw AlreadyLinked();
        }

        return await db.TrainerClients
            .Where(link => link.TrainerId == trainerId && link.ClientId == clientId.Value)
            .ProjectTo<ClientDto>(mapper.ConfigurationProvider)
            .SingleAsync(ct);
    }

    public async Task<PagedResult<ClientDto>> ListClientsAsync(
        int trainerId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.TrainerClients.AsNoTracking().Where(link => link.TrainerId == trainerId);
        var total = await query.CountAsync(ct);
        var clients = await query.OrderBy(link => link.ClientId).Skip((page - 1) * pageSize)
            .Take(pageSize).ProjectTo<ClientDto>(mapper.ConfigurationProvider).ToListAsync(ct);

        return new PagedResult<ClientDto>(clients, total, page, pageSize);
    }

    public async Task<PagedResult<WorkoutDto>> ListClientWorkoutsAsync(
        int trainerId, int clientId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        if (!await HasClientAsync(trainerId, clientId, ct))
        {
            throw new ApiException(new ErrorResponse(403, "This Member is not your client."));
        }

        return await workouts.ListAsync(clientId, from, to, page, pageSize, ct);
    }

    private Task<bool> HasClientAsync(int trainerId, int clientId, CancellationToken ct) =>
        db.TrainerClients.AnyAsync(link => link.TrainerId == trainerId && link.ClientId == clientId, ct);

    private static ApiException AlreadyLinked() =>
        new(new ErrorResponse(409, "This Member is already your client."));
}
