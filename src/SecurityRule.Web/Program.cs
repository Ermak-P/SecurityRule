using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContextFactory<FakeAdDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FakeAdConnection")));

builder.Services.AddServerSideBlazor(options => 
{
    options.DetailedErrors = true;
});

builder.Services.AddApplicationServices();

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
