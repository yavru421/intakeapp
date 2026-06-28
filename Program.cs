using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using IntakeApp;
using IntakeApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IntakeApp.Services.PdfService>();
builder.Services.AddScoped<IntakeSyncService>();

var host = builder.Build();

// Load PDF fonts client-side for WebAssembly runtime
try
{
    var httpClient = host.Services.GetRequiredService<HttpClient>();
    var fontBytes = await httpClient.GetByteArrayAsync("Roboto-Regular.ttf");
    WasmFontResolver.FontBytes = fontBytes;
    PdfSharpCore.Fonts.GlobalFontSettings.FontResolver = new WasmFontResolver();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load font or set FontResolver: {ex.Message}");
}

var syncService = host.Services.GetRequiredService<IntakeSyncService>();
await syncService.InitializeAsync();

await host.RunAsync();
