using MudBlazor.Services;
using tric.lib;
using tric.web.Components;
using tric.web.Services;

var builder = WebApplication.CreateBuilder(args);

// Texts are English, but dates (dd.MM.yyyy) and amounts (1.234,56€) are always shown in German format.
var culture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 8 * 1024 * 1024);

builder.Services.AddMudServices();
builder.Services.AddScoped<IndexedDbService>();

// Scoped (one per circuit): Api holds the auth token and user id of a single browser session.
builder.Services.AddScoped(sp => new Api(sp.GetRequiredService<ILogger<Api>>()));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<DataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
