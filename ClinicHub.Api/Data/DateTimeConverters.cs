using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicHub.Api.Data;

/// <summary>
/// Момент времени в БД (PostgreSQL <c>timestamp with time zone</c>). Храним UTC (CLAUDE.md).
/// Npgsql отказывается записывать в такую колонку DateTime с Kind ≠ Utc, а к нам приходят и Unspecified
/// (из JSON без «Z»), и Local. Поэтому при записи:
///   Utc — как есть; Local — переводим в UTC; Unspecified — считаем, что это уже UTC (так было и с SQL Server).
/// При чтении помечаем значение как Utc.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => ToUtc(v),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
{
    private static DateTime ToUtc(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc),
    };
}

/// <summary>
/// Календарная дата без времени (колонка <c>date</c>): дата рождения, день срабатывания авто-правила.
/// Часовой пояс здесь не нужен: переводить в UTC нельзя, иначе полночь по Москве превратится во вчерашний день.
/// </summary>
public sealed class DateOnlyDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => DateTime.SpecifyKind(v.Date, DateTimeKind.Unspecified),
    v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));
