using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicHub.Core.Dtos;

namespace ClinicHub.Admin.Services;

/// <summary>HTTP-клиент API ClinicHub. Токен хранится в Preferences.</summary>
public class ApiClient
{
    private const string TokenKey = "clinichub_access_token";
    private const string RefreshKey = "clinichub_refresh_token";
    private const string BaseUrlKey = "clinichub_base_url";

    private readonly HttpClient _http;
    private string _token = "";

    public ApiClient(HttpClient http)
    {
        _http = http;
        _token = Preferences.Default.Get(TokenKey, "");
        BaseUrl = Preferences.Default.Get(BaseUrlKey, "http://localhost:5112");
    }

    public string BaseUrl { get; set; }

    public string? Token => string.IsNullOrEmpty(_token) ? null : _token;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public static void SaveBaseUrl(string url) => Preferences.Default.Set(BaseUrlKey, url);

    private HttpRequestMessage Build(string method, string path, object? body = null)
    {
        var msg = new HttpRequestMessage(new HttpMethod(method), BaseUrl.TrimEnd('/') + path);
        if (body != null)
            msg.Content = JsonContent.Create(body, options: JsonOptions);
        if (!string.IsNullOrEmpty(_token))
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return msg;
    }

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<T> SendAsync<T>(HttpRequestMessage msg, CancellationToken ct = default)
    {
        using var resp = await _http.SendAsync(msg, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, text);
        if (string.IsNullOrWhiteSpace(text))
            return default!;
        return JsonSerializer.Deserialize<T>(text, JsonOptions)!;
    }

    public Task<AuthResponse> LoginAsync(string login, string password, CancellationToken ct = default)
        => SendAsync<AuthResponse>(Build("POST", "/api/auth/login", new { login, password }), ct);

    public Task<T> GetAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(Build("GET", path), ct);

    public Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(Build("POST", path, body), ct);

    public Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default)
        => SendAsync<T>(Build("PUT", path, body), ct);

    public Task<T> DeleteAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(Build("DELETE", path), ct);

    public Task SetTokenAsync(AuthResponse auth)
    {
        _token = auth.AccessToken;
        Preferences.Default.Set(TokenKey, auth.AccessToken);
        Preferences.Default.Set(RefreshKey, auth.RefreshToken);
        return Task.CompletedTask;
    }

    public void ClearToken()
    {
        _token = "";
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(RefreshKey);
    }
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string body)
        : base($"Сервер вернул {statusCode}: {body}")
    {
        StatusCode = statusCode;
    }
}