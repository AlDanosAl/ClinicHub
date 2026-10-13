namespace ClinicHub.Core.Dtos;

public record LoginRequest(string Login, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);

public record UserInfo(int Id, string Login, string Role);