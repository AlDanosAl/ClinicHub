using System.Net.Http.Json;
using ClinicHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Services;

/// <summary>Уведомление внешнего сайта клиники о завершении рассылки (POST на WebhookCallbackUrl).</summary>
public class WebhookNotifier
{
    private readonly ClinicHubDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<WebhookNotifier> _logger;

    public WebhookNotifier(ClinicHubDbContext db, IHttpClientFactory http, ILogger<WebhookNotifier> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task NotifyIfTerminalAsync(int requestId, string status, CancellationToken ct)
    {
        if (status is not ("Sent" or "Partial" or "Failed"))
            return;

        var callbackUrl = await _db.AppSettings
            .Where(s => s.Key == "WebhookCallbackUrl")
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(callbackUrl))
            return;

        var request = await _db.SmsRequests
            .Where(r => r.Id == requestId)
            .Select(r => new
            {
                r.Id,
                r.ExternalId,
                r.Title,
                Status = r.Status.ToString(),
                Total = r.Recipients.Count,
                Sent = r.Recipients.Count(x => x.Status == Core.Entities.RecipientStatus.Sent),
                Failed = r.Recipients.Count(x => x.Status == Core.Entities.RecipientStatus.Failed),
            })
            .FirstOrDefaultAsync(ct);

        if (request is null)
            return;

        try
        {
            using var client = _http.CreateClient();
            using var resp = await client.PostAsJsonAsync(callbackUrl, request, ct);
            _logger.LogInformation("Вебхук #{Id}: {StatusCode}", requestId, resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить вебхук для заявки #{Id}", requestId);
        }
    }
}