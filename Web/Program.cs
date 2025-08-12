using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System;
using System.Net.Http.Json;
using Web;
using Web.Common;
using Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

var environment = builder.HostEnvironment.Environment;
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var config = await httpClient.GetFromJsonAsync<AppSettings>($"appsettings.{environment}.json");

if (config?.ApiHosts == null || !config.ApiHosts.Any())
{
    throw new InvalidOperationException("ApiHosts cannot be null or empty, please update your appSettings.json configuration.");
}

var defaultApiHost = config.ApiHosts.First().Url;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(defaultApiHost) });

builder.Services.AddScoped<IAuthValidator, AuthValidator>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ILinkService, LinkService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IApiHostService, ApiHostService>();

builder.Services.AddSingleton(config);

builder.Services.AddSingleton<AccountState>();

await builder.Build().RunAsync();
