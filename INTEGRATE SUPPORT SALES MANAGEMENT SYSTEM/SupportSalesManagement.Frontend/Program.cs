using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SupportSalesManagement.Frontend;
using SupportSalesManagement.Frontend.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using ApexCharts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.HostEnvironment.IsDevelopment() 
    ? "http://localhost:5300/" 
    : builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddScoped(sp =>
{
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new SignalRService(localStorage, apiBaseUrl);
});
builder.Services.AddApexCharts();

var app = builder.Build();

await app.RunAsync();
