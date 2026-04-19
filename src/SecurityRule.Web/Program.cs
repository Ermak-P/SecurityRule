using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;
using SecurityRule.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddServerSideBlazor(options => 
{
    options.DetailedErrors = true;
});

builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IAppServiceRepository, AppServiceRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IFirewallRuleRepository, FirewallRuleRepository>();
builder.Services.AddScoped<IOperatingSystemRepository, OperatingSystemRepository>();
builder.Services.AddScoped<IAdAccountRepository, AdAccountRepository>();
builder.Services.AddScoped<ThemeState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<SecurityRule.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Makes the generated Program class accessible from the E2E test project
// so WebApplicationFactory<Program> can be used if needed in the future.
public partial class Program { }
