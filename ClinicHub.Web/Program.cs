using System.Text.Encodings.Web;
using System.Text.Unicode;
using ClinicHub.Web.Booking;
using ClinicHub.Web.Components;
using ClinicHub.Web.Data;
using ClinicHub.Web.Site;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);

// На компьютере клиники сайт работает службой Windows «ClinicHub Web» (deploy/web/install.ps1).
// Предупреждения и ошибки идут в журнал событий «Приложение», источник ClinicHub.Web (его регистрирует установщик).
builder.Services.AddWindowsService(o => o.ServiceName = "ClinicHub Web");
#pragma warning disable CA1416 // Журнал событий есть только в Windows; на других ОС эти настройки просто не читаются.
builder.Services.Configure<EventLogSettings>(o => o.SourceName = "ClinicHub.Web");
#pragma warning restore CA1416

// Журнал в файлы: у службы по умолчанию logs\ рядом с сайтом, при разработке — только если задан Logging:File:Directory.
var logDirectory = builder.Configuration["Logging:File:Directory"]
    ?? (WindowsServiceHelpers.IsWindowsService() ? "logs" : null);
if (logDirectory is not null)
{
    var provider = new FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, logDirectory), TimeProvider.System);
    builder.Logging.AddProvider(provider);
}

// Ключи шифрования (антифорджери, сохранённое подключение к базе) — в config\keys рядом с сайтом,
// под защитой DPAPI учётной записи сайта. Без явной папки служба держала бы их в своём профиле Windows.
var configDirectory = DbConnectionStore.ConfigDirectory(builder.Configuration, builder.Environment);
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("ClinicHub.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(configDirectory, "keys")));
if (OperatingSystem.IsWindows())
    dataProtection.ProtectKeysWithDpapi();

// БД сайта: страницы из блоков и настройки, схема site в общей базе ClinicHub (ADR-0007).
// Подключение — из user-secrets / переменной окружения ConnectionStrings__ClinicHub (разработка)
// или со страницы /nastroika-bd (компьютер клиники). Пока его нет, сайт показывает только эту страницу.
builder.Services.AddSingleton<DbConnectionStore>();
builder.Services.AddSingleton<IDbContextFactory<SiteDbContext>, SiteDbContextFactory>();
builder.Services.AddHostedService<DbConnectionMonitor>();
builder.Services.AddScoped<SiteStore>();

// site-content.json: каталог услуг и врачей, пока его нет в ClinicHub.Api (Issue #13), и начальное наполнение пустой БД.
// Каталог перечитывается на лету; остальное после первого запуска правится уже в БД.
builder.Configuration.AddJsonFile("site-content.json", optional: false, reloadOnChange: true);
builder.Services.Configure<SiteContent>(builder.Configuration.GetSection("Site"));
builder.Services.AddSingleton<ISiteCatalog, ContentFileCatalog>();

// Кириллицу в HTML отдаём как есть, а не &#x...; — страницы короче и читаемее. Опасные символы (<, >, &, ") по-прежнему экранируются.
builder.Services.AddWebEncoders(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddSingleton(TimeProvider.System);
// Пока МИС не выбрана — демо-слоты. Потом здесь будет адаптер МИС (PLAN, этап 2).
builder.Services.AddSingleton<IBookingSource, DemoBookingSource>();

// Публичная часть — статический SSR без интерактивности: быстро и индексируется (ADR-0003).
builder.Services.AddRazorComponents();

var app = builder.Build();

await app.Services.GetRequiredService<DbConnectionStore>().InitializeAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Пока база не готова, все страницы ведут на настройку подключения. Статические файлы (стили) отдаются как обычно.
app.Use(async (context, next) =>
{
    var store = context.RequestServices.GetRequiredService<DbConnectionStore>();
    var path = context.Request.Path;
    var endpoint = context.GetEndpoint();
    var isPage = endpoint is null || endpoint.Metadata.GetMetadata<ComponentTypeMetadata>() is not null
        || path.Equals("/robots.txt") || path.Equals("/sitemap.xml");
    if (store.State == DbState.Ready || !isPage || path.StartsWithSegments(DbConnectionStore.SetupPath))
    {
        await next();
        return;
    }
    context.Response.Redirect(DbConnectionStore.SetupPath);
});

app.MapStaticAssets();
app.MapSeoEndpoints();
app.MapRazorComponents<App>();

await app.RunAsync();
