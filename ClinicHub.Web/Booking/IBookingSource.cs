namespace ClinicHub.Web.Booking;

/// <summary>
/// Источник свободного времени для онлайн-записи (ARCHITECTURE §3.6).
/// Сейчас работает демо-реализация. После выбора МИС (PLAN, этап 2) здесь будет адаптер МИС,
/// а страницы записи менять не придётся.
/// </summary>
public interface IBookingSource
{
    /// <summary>true, если слоты выдуманы и запись никуда не уходит. Страница показывает предупреждение.</summary>
    bool IsDemo { get; }

    /// <summary>
    /// Свободные слоты на день (дата по Москве).
    /// <paramref name="doctorId"/> = null означает «любой свободный врач».
    /// </summary>
    Task<IReadOnlyList<BookingSlot>> GetSlotsAsync(
        string serviceId, string? doctorId, DateOnly date, CancellationToken ct = default);
}

/// <summary>Свободное окно у конкретного врача. Время храним в UTC (CLAUDE.md).</summary>
public sealed record BookingSlot(DateTimeOffset StartUtc, string DoctorId);
