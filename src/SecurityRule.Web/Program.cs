using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Services;
using SecurityRule.Web;
using SecurityRule.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddDbContextFactory<FakeAdDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FakeAdConnection")));

builder.Services.AddServerSideBlazor(options => 
{
    options.DetailedErrors = true;
});

var useActiveDirectoryAuthentication =
    builder.Configuration.GetValue<bool>("Authentication:UseActiveDirectory", true);

if (useActiveDirectoryAuthentication)
{
    builder.Services
        .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
else
{
    builder.Services
        .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName, _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddApplicationServices(useActiveDirectoryAuthentication);

// Register IPartnerService: use the real HTTP implementation when a base URL is configured.
var partnerServiceBaseUrl = builder.Configuration["PartnerService:BaseUrl"];
if (!string.IsNullOrWhiteSpace(partnerServiceBaseUrl))
{
    builder.Services.AddHttpClient<IPartnerService, PartnerService>(client =>
    {
        client.BaseAddress = new Uri(partnerServiceBaseUrl.TrimEnd('/') + "/");
    });
}
else
{
    builder.Services.AddSingleton<IPartnerService, FakePartnerService>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var fakeAdFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FakeAdDbContext>>();
    using var fakeAdDb = fakeAdFactory.CreateDbContext();
    fakeAdDb.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<SecurityRule.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Makes the generated Program class accessible from the E2E test project
// so WebApplicationFactory<Program> can be used if needed in the future.
public partial class Program { }
