using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IHostEnvironment>(new WasmHostEnvironment(builder.HostEnvironment));

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddSpeechSynthesis();
builder.Services.AddSpeechRecognition();
builder.ConfigureContainer<ServiceRegistry>(
    new LamarServiceProviderFactory(), registry =>
        registry.IncludeRegistry<UIClientServiceRegistry>());


var app = builder.Build();
await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
await ApplyBrowserCultureAsync(app.Services);
await app.RunAsync();

static async Task ApplyBrowserCultureAsync(IServiceProvider services)
{
    try
    {
        var js = services.GetRequiredService<IJSRuntime>();
        var language = await js.InvokeAsync<string>("churchBulletinBrowserCulture");
        ClientDateFormatter.ApplyBrowserCulture(language);
    }
    catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
    {
        // The page still loads when the browser language cannot be read.
    }
}