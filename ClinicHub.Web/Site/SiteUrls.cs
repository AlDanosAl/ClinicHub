namespace ClinicHub.Web.Site;

public static class SiteUrls
{
    /// <summary>
    /// Корень сайта без слеша на конце. Берём из настроек (Site:Clinic:BaseUrl),
    /// а если там пусто — из текущего запроса (удобно при разработке).
    /// </summary>
    public static string? BaseUrl(ClinicInfo clinic, HttpRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(clinic.BaseUrl))
            return clinic.BaseUrl.TrimEnd('/');
        return request is null ? null : $"{request.Scheme}://{request.Host}";
    }
}
