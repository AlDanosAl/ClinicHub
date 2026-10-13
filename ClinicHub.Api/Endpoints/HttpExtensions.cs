using System.Security.Claims;
using ClinicHub.Api.Services;

namespace ClinicHub.Api.Endpoints;

public static class HttpExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}