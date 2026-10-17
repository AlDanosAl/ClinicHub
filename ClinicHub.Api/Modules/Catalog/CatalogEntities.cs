namespace ClinicHub.Api.Modules.Catalog;

// Каталог услуг, врачей и цен (ADR-0007 §2, ADR-0008 §2). Схема БД — catalog.
// Мастер-данные: сейчас ведутся в админке (Source = Manual), после выбора МИС приходят из неё (Source = Mis)
// и синхронизируются по ExternalId. Редакторские поля (описание, видимость, порядок) синхронизация не трогает.

/// <summary>Откуда пришла запись каталога.</summary>
public enum CatalogSource
{
    Manual,
    Mis,
}

public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsVisibleOnSite { get; set; } = true;

    public List<Service> Services { get; set; } = [];
}

public class Service
{
    /// <summary>Латинский идентификатор (slug), идёт в адреса сайта и записи: «consultation».</summary>
    public string Id { get; set; } = "";

    public int? CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Описание для сайта (редакторское поле).</summary>
    public string Description { get; set; } = "";

    /// <summary>Цена «от», ₽. null — цена не указана («по запросу»).</summary>
    public decimal? PriceFrom { get; set; }

    public int DurationMinutes { get; set; } = 45;
    public bool IsVisibleOnSite { get; set; } = true;
    public int SortOrder { get; set; }

    public CatalogSource Source { get; set; } = CatalogSource.Manual;

    /// <summary>Идентификатор в МИС (для синхронизации).</summary>
    public string? ExternalId { get; set; }
    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DoctorService> Doctors { get; set; } = [];
}

public class Doctor
{
    /// <summary>Латинский идентификатор (slug): «lastochkina-shankina».</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Специальность и стаж одной строкой, как на сайте.</summary>
    public string Role { get; set; } = "";

    public string Description { get; set; } = "";
    public bool IsVisibleOnSite { get; set; } = true;
    public int SortOrder { get; set; }

    public CatalogSource Source { get; set; } = CatalogSource.Manual;
    public string? ExternalId { get; set; }
    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DoctorService> Services { get; set; } = [];
}

/// <summary>Какие услуги ведёт врач.</summary>
public class DoctorService
{
    public string DoctorId { get; set; } = "";
    public Doctor? Doctor { get; set; }

    public string ServiceId { get; set; } = "";
    public Service? Service { get; set; }
}

/// <summary>История цен: нужна для разборов «почему на сайте была другая цена».</summary>
public class PriceHistory
{
    public long Id { get; set; }
    public string ServiceId { get; set; } = "";
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Кто или что поменяло: «user:admin», «mis-sync», «seed».</summary>
    public string ChangedBy { get; set; } = "";
}
