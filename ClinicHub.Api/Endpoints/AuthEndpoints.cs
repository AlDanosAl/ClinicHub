using System.Security.Claims;
using ClinicHub.Core.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Аутентификация");

        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/refresh", Refresh).AllowAnonymous();

        return app;
    }

    private static async Task<IResult> Login(
        ClinicHub.Core.Dtos.LoginRequest req,
        Data.ClinicHubDbContext db,
        Services.TokenService tokens)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Login == req.Login);
        if (user is null || !user.IsActive ||
            !Services.PasswordHasher.Verify(req.Password, user.Salt, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken(user);
        return Results.Ok(new ClinicHub.Core.Dtos.AuthResponse(access, refresh, 3600));
    }

    private static async Task<IResult> Refresh(
        ClinicHub.Core.Dtos.RefreshRequest req,
        Data.ClinicHubDbContext db,
        Services.TokenService tokens)
    {
        var principal = tokens.ValidateToken(req.RefreshToken, requireRefreshType: true);
        if (principal is null)
            return Results.Unauthorized();

        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var id))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(new ClinicHub.Core.Dtos.AuthResponse(
            tokens.CreateAccessToken(user),
            tokens.CreateRefreshToken(user),
            3600));
    }
}