using System.Globalization;

namespace ClinicHub.Web.Booking;

/// <summary>Параметры адреса /zapis: шаг записи хранится в адресе, поэтому «Назад» в браузере работает.</summary>
public static class BookingQuery
{
    public const string AnyDoctor = "any";

    public static DateOnly? ParseDate(string? s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public static TimeOnly? ParseTime(string? s) =>
        TimeOnly.TryParseExact(s, "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;

    public static string FormatDate(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string FormatTime(TimeOnly t) => t.ToString("HHmm", CultureInfo.InvariantCulture);
}

/// <summary>Куда прислать код подтверждения и напоминание.</summary>
public enum ReminderChannel
{
    Sms,
    Telegram,
    Max,
}

/// <summary>Данные последнего шага записи.</summary>
public sealed class BookingForm
{
    public string? Phone { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.Sms;

    /// <summary>Согласие на обработку ПДн — обязательно.</summary>
    public bool PersonalDataConsent { get; set; }

    /// <summary>Согласие на рекламные рассылки — отдельно и необязательно (модуль Consents).</summary>
    public bool MarketingConsent { get; set; }
}
