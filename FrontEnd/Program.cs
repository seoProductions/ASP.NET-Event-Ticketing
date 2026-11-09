using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using FrontEnd.Authentication;
using FrontEnd;
using Frontend.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Support for JWT
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    provider =>
        provider.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddTransient<JwtAuthHandler>();

builder.Services.AddHttpClient("API", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5158/");        // back-end url
    })
    .AddHttpMessageHandler<JwtAuthHandler>();


await builder.Build().RunAsync();
