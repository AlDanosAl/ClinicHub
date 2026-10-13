using ClinicHub.Core.Dtos;
using ClinicHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Дашборд")
            .RequireAuthorization();

        group.MapGet("/summary", Summary);

        return app;
    }

    private static async Task<IResult> Summary(Data.ClinicHubDbContext db)
    {
        var todayStartUtc = new DateTimeOffset(DateTime.Today).UtcDateTime;
        var now = DateTime.UtcNow;
        var onlineCutoff = now.AddMinutes(-2);

        var result = new DashboardSummaryDto
        {
            RequestsToday = await db.SmsRequests.CountAsync(r => r.CreatedAt >= todayStartUtc),
            RequestsWaiting = await db.SmsRequests.CountAsync(r =>
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Approved ||
                r.Status == RequestStatus.Scheduled ||
                r.Status == RequestStatus.Sending),
            SmsSentToday = await db.SmsLogs.CountAsync(l =>
                l.Status == SmsStatus.Sent && l.SentAt >= todayStartUtc),
            SmsFailedToday = await db.SmsLogs.CountAsync(l =>
                l.Status == SmsStatus.Failed && l.SentAt >= todayStartUtc),
            SmsInQueue = await db.SmsQueue.CountAsync(q =>
                q.Status == QueueItemStatus.Pending || q.Status == QueueItemStatus.Assigned),
            PhonesOnline = await db.Phones.CountAsync(p =>
                p.IsActive && p.LastSeenAt != null && p.LastSeenAt.Value >= onlineCutoff),
            PhonesTotal = await db.Phones.CountAsync(p => p.IsActive),
        };

        result.SimBalances = await db.SimCards
            .Include(s => s.Phone)
            .Where(s => s.Phone != null && s.Phone.IsActive)
            .Select(s => new SimBalanceDto
            {
                SimCardId = s.Id,
                Number = s.Number,
                Operator = s.Operator,
                Balance = s.Balance,
                IsActive = s.IsActive,
            })
            .OrderBy(x => x.SimCardId)
            .ToListAsync();

        var segmentsBySim = await db.SmsLogs
            .Where(l => l.Status == SmsStatus.Sent && l.SentAt >= todayStartUtc && l.SimCardId != null)
            .GroupBy(l => l.SimCardId!.Value)
            .Select(g => new { Sim = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.Sim, x => x.Cnt);

        foreach (var sim in result.SimBalances)
            sim.TodaySegments = segmentsBySim.GetValueOrDefault(sim.SimCardId);

        result.Phones = await db.Phones
            .OrderBy(p => p.Id)
            .Select(p => new PhoneStatusDto
            {
                Id = p.Id,
                PhoneNumber = p.PhoneNumber,
                IsActive = p.IsActive,
                LastSeenAt = p.LastSeenAt,
                IsOnline = p.IsActive && p.LastSeenAt != null && p.LastSeenAt.Value >= onlineCutoff,
            })
            .ToListAsync();

        return Results.Ok(result);
    }
}