using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Repositories;
using SecurityRule.Infrastructure.Services;
using SecurityRule.Web.Services;

namespace SecurityRule.Web;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that register all
/// application services shared between the production app (Program.cs) and the
/// E2E test server (TestWebServer.cs).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all domain repositories, infrastructure services, and web-layer
    /// services. DbContext registrations are intentionally excluded so that the
    /// caller can choose the appropriate storage provider (SQL Server vs. InMemory).
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        bool useActiveDirectory = false)
    {
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IAppServiceRepository, AppServiceRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<IServiceConnectionRepository, ServiceConnectionRepository>();
        services.AddScoped<IOperatingSystemRepository, OperatingSystemRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IPartnerNameRepository, PartnerNameRepository>();
        services.AddScoped<IAgeOptionRepository, AgeOptionRepository>();
        services.AddScoped<ISearchService, SearchService>();
        if (useActiveDirectory && OperatingSystem.IsWindows())
            services.AddScoped<IAdService, ActiveDirectoryService>();
        else
            services.AddSingleton<IAdService, FakeAdService>();
        services.AddScoped<ThemeState>();
        services.AddScoped<GraphMapElementsBuilder>();
        services.AddScoped<ServerFormService>();

        return services;
    }
}
