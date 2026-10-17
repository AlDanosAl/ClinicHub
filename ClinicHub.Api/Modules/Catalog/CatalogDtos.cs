namespace ClinicHub.Api.Modules.Catalog;

// ── Чтение для сайта (без авторизации, только видимое на сайте) ─────────────
public record PublicCategoryDto(int Id, string Name, int SortOrder);

public record PublicServiceDto(
    string Id, int? CategoryId, string? CategoryName, string Name, string Description,
    decimal? PriceFrom, int DurationMinutes, int SortOrder);

public record PublicDoctorDto(string Id, string Name, string Role, string Description, int SortOrder, IReadOnlyList<string> ServiceIds);

// ── Админка ─────────────────────────────────────────────────────────────────
public record CategoryDto(int Id, string Name, int SortOrder, bool IsVisibleOnSite);

public record CategoryInputDto(string Name, int SortOrder = 0, bool IsVisibleOnSite = true);

public record ServiceDto(
    string Id, int? CategoryId, string Name, string Description, decimal? PriceFrom, int DurationMinutes,
    bool IsVisibleOnSite, int SortOrder, CatalogSource Source, string? ExternalId, DateTime? SyncedAt,
    DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>Создание и правка услуги. Id задаётся только при создании, в PUT берётся из адреса.</summary>
public record ServiceInputDto(
    string? Id, int? CategoryId, string Name, string? Description, decimal? PriceFrom,
    int DurationMinutes = 45, bool IsVisibleOnSite = true, int SortOrder = 0, string? ExternalId = null);

public record DoctorDto(
    string Id, string Name, string Role, string Description, bool IsVisibleOnSite, int SortOrder,
    CatalogSource Source, string? ExternalId, DateTime? SyncedAt, IReadOnlyList<string> ServiceIds);

public record DoctorInputDto(
    string? Id, string Name, string? Role, string? Description, IReadOnlyList<string>? ServiceIds,
    bool IsVisibleOnSite = true, int SortOrder = 0, string? ExternalId = null);

public record PriceHistoryDto(long Id, string ServiceId, decimal? OldPrice, decimal? NewPrice, DateTime ChangedAt, string ChangedBy);
