using System.Globalization;

namespace ClinicHub.Web.Booking;

/// <summary>Время клиники: храним UTC, показываем по Москве (CLAUDE.md).</summary>
public static class ClinicTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    public static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow()).DateTime);

    /// <summary>Момент по московскому времени → UTC.</summary>
    public static DateTimeOffset FromLocal(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>«пн, 19 октября, 10:45»</summary>
    public static string FormatSlot(DateTimeOffset utc)
    {
        var l = ToLocal(utc);
        return l.ToString("ddd, d MMMM, HH:mm", Ru);
    }

    public static string FormatTime(DateTimeOffset utc) => ToLocal(utc).ToString("HH:mm", Ru);
}
