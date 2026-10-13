using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.SmsAgent.Services;

public sealed class AgentApi : IDisposable
{
    private readonly HttpClient _http = new(new TokenHandler());
    private string _baseUrl = "";

    /// <summary>Добавляет к каждому запросу ключ устройства (если он уже выдан сервером).</summary>
    private sealed class TokenHandler : DelegatingHandler
    {
        public TokenHandler() : base(new HttpClientHandler()) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = AgentSettings.AgentToken;
            if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("X-Agent-Token"))
                request.Headers.TryAddWithoutValidation("X-Agent-Token", token);
            return base.SendAsync(request, cancellationToken);
        }
    }

    public AgentApi()
    {
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public string BaseUrl => _baseUrl;

    /// <summary>
    /// Запоминает адрес сервера. Намеренно не через HttpClient.BaseAddress: его нельзя менять после
    /// первого запроса (InvalidOperationException), а фоновый сервис вызывает это в каждом цикле.
    /// </summary>
    public void SetBaseUrl(string url)
    {
        url = url.Trim().TrimEnd('/');
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("Некорректный адрес сервера: " + url);
        _baseUrl = url;
    }

    private Uri Url(string path)
    {
        if (_baseUrl.Length == 0)
            throw new InvalidOperationException("Адрес сервера не задан.");
        return new Uri(_baseUrl + "/" + path.TrimStart('/'), UriKind.Absolute);
    }

    /// <summary>Подключение: сервер создаёт (или узнаёт) телефон и выдаёт идентификатор и ключ.</summary>
    public async Task<(bool Ok, string Message, string PhoneNumber, string Token)> ConnectAsync(
        string phoneNumber, string deviceName, string deviceId, CancellationToken ct)
    {
        using var resp = await PostJson("/api/agent/connect", new { phoneNumber, deviceName, deviceId }, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            return (false, $"HTTP {(int)resp.StatusCode}: {body.Trim()}", "", "");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var ok = root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
        var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
        var id = root.TryGetProperty("phoneNumber", out var idEl) ? idEl.GetString() ?? "" : "";
        var token = root.TryGetProperty("token", out var tEl) ? tEl.GetString() ?? "" : "";
        return (ok && id.Length > 0 && token.Length > 0, message, id, token);
    }

    /// <summary>Status: код HTTP-ответа; 0 — сервер не ответил (сеть/адрес).</summary>
    public async Task<(bool Ok, int PendingTasks, int Status)> HeartbeatAsync(
        string phoneNumber, CancellationToken ct)
    {
        try
        {
            using var resp = await PostJson("/api/agent/heartbeat", new { phoneNumber }, ct);
            var status = (int)resp.StatusCode;
            if (!resp.IsSuccessStatusCode)
                return (false, 0, status);
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
            var pending = root.TryGetProperty("pendingTasks", out var pEl) ? pEl.GetInt32() : 0;
            return (ok, pending, status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    public async Task<List<AgentTaskDto>> GetTasksAsync(
        string phoneNumber, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                Url($"/api/agent/{Uri.EscapeDataString(phoneNumber)}/tasks"));
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                AgentLog.WriteVerbose($"Опрос задач: сервер ответил HTTP {(int)resp.StatusCode}");
                return new List<AgentTaskDto>();
            }

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("tasks", out var tasks))
                return new List<AgentTaskDto>();

            return tasks.Deserialize<List<AgentTaskDto>>(_jsonOptions) ?? new List<AgentTaskDto>();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            AgentLog.WriteVerbose("Опрос задач: ошибка — " + ex.Message);
            return new List<AgentTaskDto>();
        }
    }

    /// <summary>Отправляет строки журнала на сервер. true — сервер принял.</summary>
    public async Task<bool> UploadLogAsync(
        string phoneNumber, string deviceName, List<string> lines, CancellationToken ct)
    {
        try
        {
            using var resp = await PostJson("/api/agent/log", new { phoneNumber, deviceName, lines }, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Http: код ответа сервера (0 — сервер не ответил).</summary>
    public async Task<(bool Accepted, string Status, int Http)> PostResultAsync(
        string phoneNumber, long taskId, string result,
        int segments, string? error, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post,
                Url($"/api/agent/{Uri.EscapeDataString(phoneNumber)}/tasks/{taskId}/result"));
            req.Content = new StringContent(
                JsonSerializer.Serialize(new { taskId, result, segments, error }),
                Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return (false, "", (int)resp.StatusCode);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var accepted = root.TryGetProperty("accepted", out var aEl) && aEl.GetBoolean();
            var status = root.TryGetProperty("taskStatus", out var sEl) ? sEl.GetString() ?? "" : "";
            return (accepted, status, (int)resp.StatusCode);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return (false, "", 0);
        }
    }

    private async Task<HttpResponseMessage> PostJson(
        string path, object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _http.PostAsync(Url(path), content, ct);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public void Dispose() => _http.Dispose();
}

public class AgentTaskDto
{
    public long TaskId { get; set; }
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";
    public int? SubscriptionId { get; set; }
    public int Segments { get; set; }
}
