using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class DevelopmentAuthenticationHandlerTests
{
    [Test]
    public async Task AuthenticateAsync_ShouldReturnConfiguredDevelopmentUser()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DevelopmentUser"] = "dev.user"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationHandler.SchemeName, _ => { });

        var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthenticationService>();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };

        var result = await auth.AuthenticateAsync(context, DevelopmentAuthenticationHandler.SchemeName);

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity!.Name.Should().Be("dev.user");
    }
}
