using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicHub.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ClinicHub.Api.Services;

public class TokenService
{
    private readonly TokenOptions _options;

    public TokenService(TokenOptions options)
    {
        _options = options;
    }

    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        return CreateToken(claims, TimeSpan.FromMinutes(_options.AccessTokenMinutes));
    }

    public string CreateRefreshToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("token_type", "refresh"),
        };
        return CreateToken(claims, TimeSpan.FromDays(_options.RefreshTokenDays));
    }

    public ClaimsPrincipal? ValidateToken(string token, bool requireRefreshType = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, GetParameters(), out var validatedToken);
            var jwt = validatedToken as JwtSecurityToken;
            var isRefresh = jwt?.Claims.Any(c => c is { Type: "token_type", Value: "refresh" }) == true;

            if (requireRefreshType && !isRefresh)
                return null;
            if (!requireRefreshType && isRefresh)
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private TokenValidationParameters GetParameters() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private string CreateToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}