using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using ServersIndex = SecurityRule.Web.Components.Pages.Servers.Index;
using ServersCreate = SecurityRule.Web.Components.Pages.Servers.Create;
using ServersEdit = SecurityRule.Web.Components.Pages.Servers.Edit;
using ServersDetails = SecurityRule.Web.Components.Pages.Servers.Details;

namespace SecurityRule.BDD.Tests.ComponentTests;

/// <summary>
/// bUnit component tests for the Servers pages.
/// These tests verify the UI rendering of the Blazor components
/// using mocked repository dependencies.
/// </summary>
[TestFixture]
public class ServersComponentTests : BunitTestContext
{
    private Mock<IServerRepository> _serverRepositoryMock = null!;
    private Mock<ISnackbar> _snackbarMock = null!;

    [SetUp]
    public void SetUp()
    {
        _serverRepositoryMock = new Mock<IServerRepository>();
        _snackbarMock = new Mock<ISnackbar>();

        Services.AddSingleton(_serverRepositoryMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(Mock.Of<IAppServiceRepository>());
        Services.AddSingleton(Mock.Of<ICertificateRepository>());
        Services.AddSingleton(Mock.Of<IFirewallRuleRepository>());
        AddMudBlazorTestServices();
    }

    [Test]
    public void Index_WhenNoServers_ShouldRenderTable()
    {
        // Arrange
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<ServersIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var table = cut.FindAll("table");
            table.Should().NotBeEmpty();
        });
    }

    [Test]
    public void Index_WhenServersExist_ShouldRenderServerNames()
    {
        // Arrange
        var servers = new List<Server>
        {
            new() { Id = 1, Name = "Web-Server", IpAddress = "10.0.0.1", OperatingSystem = "Linux" },
            new() { Id = 2, Name = "DB-Server", IpAddress = "10.0.0.2", OperatingSystem = "Windows" }
        };
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(servers);

        // Act
        var cut = Render<ServersIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("Web-Server");
            markup.Should().Contain("DB-Server");
        });
    }

    [Test]
    public void Index_ShouldDisplayServerIpAndOs()
    {
        // Arrange
        var servers = new List<Server>
        {
            new() { Id = 1, Name = "App-Server", IpAddress = "192.168.1.1", OperatingSystem = "Ubuntu" }
        };
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(servers);

        // Act
        var cut = Render<ServersIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("192.168.1.1");
            markup.Should().Contain("Ubuntu");
        });
    }

    [Test]
    public void Create_ShouldRenderFormFields()
    {
        // Act
        var cut = Render<ServersCreate>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Название");
        markup.Should().Contain("IP");
        markup.Should().Contain("Операционная система");
    }

    [Test]
    public void Details_WhenServerExists_ShouldShowServerInfo()
    {
        // Arrange
        var server = new Server
        {
            Id = 1,
            Name = "Test-Server",
            IpAddress = "10.10.10.10",
            OperatingSystem = "CentOS",
            Services = []
        };
        _serverRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        // Act
        var cut = Render<ServersDetails>(p => p.Add(d => d.Id, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("Test-Server");
            markup.Should().Contain("10.10.10.10");
            markup.Should().Contain("CentOS");
        });
    }

    [Test]
    public void Details_WhenServerHasNoServices_ShouldShowInfoAlert()
    {
        // Arrange
        var server = new Server
        {
            Id = 1,
            Name = "Empty-Server",
            IpAddress = "10.0.0.1",
            OperatingSystem = "Linux",
            Services = []
        };
        _serverRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        // Act
        var cut = Render<ServersDetails>(p => p.Add(d => d.Id, 1));

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("нет сервисов"));
    }

    [Test]
    public void Edit_WhenServerExists_ShouldPopulateForm()
    {
        // Arrange
        var server = new Server
        {
            Id = 5,
            Name = "Edit-Server",
            IpAddress = "10.0.5.5",
            OperatingSystem = "Debian"
        };
        _serverRepositoryMock
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(server);

        // Act
        var cut = Render<ServersEdit>(p => p.Add(e => e.Id, 5));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("Edit-Server");
            markup.Should().Contain("10.0.5.5");
            markup.Should().Contain("Debian");
        });
    }
}
