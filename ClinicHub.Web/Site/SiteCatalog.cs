using Microsoft.Extensions.Options;

namespace ClinicHub.Web.Site;

/// <summary>
/// Каталог для сайта и записи: услуги с ценами и врачи (ADR-0007 §2). Блоки страниц хранят только id,
/// а названия и цены берутся отсюда, поэтому цена правится в одном месте.
/// </summary>
public interface ISiteCatalog
{
    IReadOnlyList<ServiceItem> Services { get; }
    IReadOnlyList<DoctorItem> Doctors { get; }
}

/// <summary>
/// Временный каталог из site-content.json (разделы Services и Doctors), перечитывается на лету.
/// Каталог принадлежит серверу (модуль Catalog в ClinicHub.Api, ADR-0008 §2, Issue #13).
/// Когда там появится API, здесь будет его клиент, а страницы менять не придётся.
/// </summary>
public sealed class ContentFileCatalog(IOptionsMonitor<SiteContent> content) : ISiteCatalog
{
    public IReadOnlyList<ServiceItem> Services => content.CurrentValue.Services;
    public IReadOnlyList<DoctorItem> Doctors => content.CurrentValue.Doctors;
}

public static class SiteCatalogExtensions
{
    /// <summary>Врач ведёт услугу. Пустой список услуг у врача — ведёт все.</summary>
    public static bool Treats(this DoctorItem doctor, string serviceId) =>
        doctor.ServiceIds.Count == 0 || doctor.ServiceIds.Contains(serviceId);

    /// <summary>Выбранные элементы в заданном порядке; пустой выбор — все. Неизвестные id пропускаем (удалили из каталога).</summary>
    public static IEnumerable<T> Pick<T>(this IReadOnlyList<T> all, IReadOnlyList<string> ids, Func<T, string> id) =>
        ids.Count == 0 ? all : ids.Select(x => all.FirstOrDefault(item => id(item) == x)).OfType<T>();
}
