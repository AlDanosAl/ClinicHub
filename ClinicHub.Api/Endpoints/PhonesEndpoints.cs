using ClinicHub.Core.Dtos;
using ClinicHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Endpoints;

public static class PhonesEndpoints
{
    public static IEndpointRouteBuilder MapPhonesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/phones")
            .WithTags("Телефоны и SIM")
            .RequireAuthorization();

        group.MapGet("/", GetList);
        group.MapPost("/", Create);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/{id:int}/activate", SetActive(true));
        group.MapPost("/{id:int}/deactivate", SetActive(false));
        group.MapPost("/{id:int}/sim", AddSim);
        group.MapGet("/sim/{simId:int}/balance-log", GetBalanceLog);
        group.MapPost("/sim/{simId:int}/balance", SetBalance);

        return app;
    }

    private static async Task<IResult> GetList(Data.ClinicHubDbContext db)
    {
        var onlineCutoff = DateTime.UtcNow.AddMinutes(-2);
        var phones = await db.Phones
            .Include(p => p.SimCards)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var result = phones.Select(p => ToDto(p, onlineCutoff));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(int id, Data.ClinicHubDbContext db)
    {
        var phone = await db.Phones
            .Include(p => p.SimCards)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (phone is null)
            return Results.NotFound();

        return Results.Ok(ToDto(phone, DateTime.UtcNow.AddMinutes(-2)));
    }

    private static async Task<IResult> Create(
        PhoneInputDto dto,
        Data.ClinicHubDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return Results.BadRequest("Укажите номер телефона.");

        var normalized = dto.PhoneNumber.Trim();
        if (await db.Phones.AnyAsync(p => p.PhoneNumber == normalized, ct))
            return Results.BadRequest("Телефон с таким номером уже зарегистрирован.");

        var phone = new Phone
        {
            PhoneNumber = normalized,
            IsActive = dto.IsActive,
        };

        db.Phones.Add(phone);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/phones/{phone.Id}",
            new { phone.Id, phone.PhoneNumber, phone.IsActive });
    }

    private static Func<int, Data.ClinicHubDbContext, CancellationToken, Task<IResult>> SetActive(bool active)
        => async (id, db, ct) =>
        {
            var phone = await db.Phones.FindAsync(id);
            if (phone is null)
                return Results.NotFound();

            phone.IsActive = active;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { phone.Id, phone.IsActive });
        };

    private static async Task<IResult> AddSim(
        int id,
        SimCardInputDto dto,
        Data.ClinicHubDbContext db,
        CancellationToken ct)
    {
        var phone = await db.Phones.FindAsync(id);
        if (phone is null)
            return Results.NotFound();

        var sim = new SimCard
        {
            PhoneId = id,
            Number = dto.Number,
            Operator = dto.Operator,
            LimitPerMinute = dto.LimitPerMinute,
            LimitPerHour = dto.LimitPerHour,
            LimitPerDay = dto.LimitPerDay,
            Balance = dto.Balance,
            SubscriptionId = dto.SubscriptionId,
            IsActive = dto.IsActive,
        };

        db.SimCards.Add(sim);

        if (dto.Balance != 0)
        {
            db.BalanceLogs.Add(new BalanceLog
            {
                SimCard = sim,
                Change = dto.Balance,
                Reason = "Пополнение",
                BalanceAfter = dto.Balance,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/phones/{id}/sim/{sim.Id}", ToSimDto(sim));
    }

    private static async Task<IResult> SetBalance(
        int simId,
        SetBalanceDto dto,
        Data.ClinicHubDbContext db,
        CancellationToken ct)
    {
        var sim = await db.SimCards.FindAsync(simId);
        if (sim is null)
            return Results.NotFound();

        var change = dto.NewBalance - sim.Balance;
        sim.Balance = dto.NewBalance;

        db.BalanceLogs.Add(new BalanceLog
        {
            SimCardId = sim.Id,
            Change = change,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? "Корректировка" : dto.Reason,
            BalanceAfter = sim.Balance,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSimDto(sim));
    }

    private static async Task<IResult> GetBalanceLog(
        int simId,
        Data.ClinicHubDbContext db,
        int? take)
    {
        var logs = await db.BalanceLogs
            .Where(l => l.SimCardId == simId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(take ?? 100)
            .Select(l => new BalanceLogDto
            {
                Id = l.Id,
                SimCardId = l.SimCardId,
                Change = l.Change,
                Reason = l.Reason,
                CreatedAt = l.CreatedAt,
                BalanceAfter = l.BalanceAfter,
            })
            .ToListAsync();

        return Results.Ok(logs);
    }

    private static PhoneDto ToDto(Phone p, DateTime onlineCutoff) => new()
    {
        Id = p.Id,
        PhoneNumber = p.PhoneNumber,
        IsActive = p.IsActive,
        LastSeenAt = p.LastSeenAt,
        IsOnline = p.IsActive && p.LastSeenAt != null && p.LastSeenAt.Value >= onlineCutoff,
        SimCards = p.SimCards.Select(ToSimDto).ToList(),
    };

    private static SimCardDto ToSimDto(SimCard s) => new()
    {
        Id = s.Id,
        PhoneId = s.PhoneId,
        Number = s.Number,
        Operator = s.Operator,
        LimitPerMinute = s.LimitPerMinute,
        LimitPerHour = s.LimitPerHour,
        LimitPerDay = s.LimitPerDay,
        Balance = s.Balance,
        SubscriptionId = s.SubscriptionId,
        IsActive = s.IsActive,
    };
}