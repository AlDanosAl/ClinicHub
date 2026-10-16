using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace ClinicHub.Web.Site;

/// <summary>robots.txt и sitemap.xml (PLAN, задача 4.2).</summary>
public static class SeoEndpoints
{
    /// <summary>Публичные страницы для поисковиков. Новые страницы добавлять сюда.</summary>
    private static readonly string[] PublicPaths = ["/", "/zapis", "/politika", "/pravila"];

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", (HttpRequest req, IOptionsSnapshot<SiteContent> content) =>
        {
            var baseUrl = SiteUrls.BaseUrl(content.Value.Clinic, req);
            var sb = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Disallow: /zapis?")
                .AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
            return Results.Text(sb.ToString(), "text/plain", Encoding.UTF8);
        });

        app.MapGet("/sitemap.xml", (HttpRequest req, IOptionsSnapshot<SiteContent> content) =>
        {
            var baseUrl = SiteUrls.BaseUrl(content.Value.Clinic, req);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset",
                    PublicPaths.Select(p => new XElement(ns + "url", new XElement(ns + "loc", baseUrl + p)))));
            return Results.Text(doc.Declaration + "\n" + doc.Root, "application/xml", Encoding.UTF8);
        });

        return app;
    }
}
