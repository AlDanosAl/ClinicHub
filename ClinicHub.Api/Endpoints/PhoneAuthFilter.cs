using System.Security.Cryptography;
using System.Text;
using ClinicHub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Api.Endpoints;

/// <summary>
/// Проверка устройства SmsAgent для запросов, где путь содержит phoneNumber.
/// Для альфа-версии идентификация — по самому номеру телефона, без отдельного ApiKey
/// (сознательно принятое упрощение: см. комментарий в AgentEndpoints.Register). Это значит,
/// что доступ к задачам/результатам даёт сам факт, что номер уже добавлен и активен в
/// админке — секрета, который нужно держать в тайне на устройстве, больше нет.
/// </summary>
public class PhoneAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var phoneNumber = context.HttpContext.Request.RouteValues["phoneNumber"]?.ToString();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Results.Unauthorized();

        var db = context.HttpContext.RequestServices.GetRequiredService<Data.ClinicHubDbContext>();
        var phone = await db.Phones.FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
        if (phone is null || !phone.IsActive || !AgentAuth.IsAuthorized(phone, context.HttpContext))
            return Results.Unauthorized();

        context.HttpContext.Items["PhoneId"] = phone.Id;
        context.HttpContext.Items["Phone"] = phone;
        return await next(context);
    }
}

public static class HttpContextPhoneExtensions
{
    public static int GetPhoneId(this HttpContext http)
        => (int)http.Items["PhoneId"]!;
}

/// <summary>
/// Проверка ключа устройства. Телефон получает ключ при подключении (кнопка «Подключиться» в
/// приложении) и присылает его в заголовке X-Agent-Token с каждым запросом. Телефоны без ключа
/// (созданные вручную до этой версии) принимаются по одному идентификатору — как раньше.
/// </summary>
public static class AgentAuth
{
    public const string HeaderName = "X-Agent-Token";

    public static bool IsAuthorized(Phone phone, HttpContext http)
    {
        if (string.IsNullOrEmpty(phone.AgentToken))
            return true;

        var given = http.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(given))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(given),
            Encoding.UTF8.GetBytes(phone.AgentToken));
    }
}
