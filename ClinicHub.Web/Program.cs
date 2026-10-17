using System.Text.Encodings.Web;
using System.Text.Unicode;
using ClinicHub.Web.Booking;
using ClinicHub.Web.Components;
using ClinicHub.Web.Site;

var builder = WebApplication.CreateBuilder(args);

// Контент сайта (клиника, услуги, врачи) — отдельный файл, правится без пересборки.
builder.Configuration.AddJsonFile("site-content.json", optional: false, reloadOnChange: true);
builder.Services.Configure<SiteContent>(builder.Configuration.GetSection("Site"));

// Кириллицу в HTML отдаём как есть, а не &#x...; — страницы короче и читаемее. Опасные символы (<, >, &, ") по-прежнему экранируются.
builder.Services.AddWebEncoders(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddSingleton(TimeProvider.System);
// Пока МИС не выбрана — демо-слоты. Потом здесь будет адаптер МИС (PLAN, этап 2).
builder.Services.AddSingleton<IBookingSource, DemoBookingSource>();

// Публичная часть — статический SSR без интерактивности: быстро и индексируется (ADR-0003).
builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapSeoEndpoints();
app.MapRazorComponents<App>();

app.Run();
