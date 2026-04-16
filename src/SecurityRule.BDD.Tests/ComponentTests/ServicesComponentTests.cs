using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using ServicesIndex = SecurityRule.Web.Components.Pages.Services.Index;
using ServicesCreate = SecurityRule.Web.Components.Pages.Services.Create;
using ServicesEdit = SecurityRule.Web.Components.Pages.Services.Edit;
using ServicesDetails = SecurityRule.Web.Components.Pages.Services.Details;

namespace SecurityRule.BDD.Tests.ComponentTests;

/// <summary>
/// bUnit component tests for the Services pages.
/// </summary>
[TestFixture]
public class ServicesComponentTests : BunitTestContext
{
    private Mock<IAppServiceRepository> _appServiceRepositoryMock = null!;
    private Mock<IServerRepository> _serverRepositoryMock = null!;
    private Mock<ISnackbar> _snackbarMock = null!;

    [SetUp]
    public void SetUp()
    {
        _appServiceRepositoryMock = new Mock<IAppServiceRepository>();
        _serverRepositoryMock = new Mock<IServerRepository>();
        _snackbarMock = new Mock<ISnackbar>();

        Services.AddSingleton(_appServiceRepositoryMock.Object);
        Services.AddSingleton(_serverRepositoryMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(Mock.Of<ICertificateRepository>());
        Services.AddSingleton(Mock.Of<IFirewallRuleRepository>());
        AddMudBlazorTestServices();
    }

    [Test]
    public void Index_WhenNoServices_ShouldRenderTable()
    {
        // Arrange
        _appServiceRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<ServicesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var table = cut.FindAll("table");
            table.Should().NotBeEmpty();
        });
    }

    [Test]
    public void Index_WhenServicesExist_ShouldRenderServiceNames()
    {
        // Arrange
        var services = new List<AppService>
        {
            new() { Id = 1, Name = "AuthService", AdAccountName = "domain\\auth", Servers = [], Certificates = [] },
            new() { Id = 2, Name = "PaymentService", AdAccountName = "domain\\payment", Servers = [], Certificates = [] }
        };
        _appServiceRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(services);

        // Act
        var cut = Render<ServicesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("AuthService");
            markup.Should().Contain("PaymentService");
        });
    }

    [Test]
    public void Index_ShouldDisplayAdAccountName()
    {
        // Arrange
        var services = new List<AppService>
        {
            new() { Id = 1, Name = "MyService", AdAccountName = "domain\\mysvc", Servers = [], Certificates = [] }
        };
        _appServiceRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(services);

        // Act
        var cut = Render<ServicesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("domain\\mysvc"));
    }

    [Test]
    public void Create_ShouldRenderFormFields()
    {
        // Arrange
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<ServicesCreate>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Название");
        markup.Should().Contain("AD");
    }

    [Test]
    public void Create_ShouldListAvailableServers()
    {
        // Arrange
        var servers = new List<Server>
        {
            new() { Id = 1, Name = "Server-A", IpAddress = "10.0.0.1", OperatingSystem = "Linux" },
            new() { Id = 2, Name = "Server-B", IpAddress = "10.0.0.2", OperatingSystem = "Windows" }
        };
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(servers);

        // Act
        var cut = Render<ServicesCreate>();

        // Assert – The MudSelect renders MudSelectItem elements in a popover/dropdown,
        // so we verify that the Servers select input element is present in the page,
        // and that the component loaded the server list (verified via the repository call).
        cut.WaitForAssertion(() =>
        {
            // Verify the select for "Серверы" is rendered
            var markup = cut.Markup;
            markup.Should().Contain("Серверы");
        });
        _serverRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Test]
    public void Details_WhenServiceExists_ShouldShowServiceInfo()
    {
        // Arrange
        var service = new AppService
        {
            Id = 5,
            Name = "Details-Service",
            AdAccountName = "domain\\details",
            Servers = [],
            Certificates = []
        };
        _appServiceRepositoryMock
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(service);

        // Act
        var cut = Render<ServicesDetails>(p => p.Add(d => d.Id, 5));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("Details-Service");
            markup.Should().Contain("domain\\details");
        });
    }

    [Test]
    public void Details_WhenServiceHasNoServers_ShouldShowInfoAlert()
    {
        // Arrange
        var service = new AppService
        {
            Id = 6,
            Name = "Unlinked-Service",
            AdAccountName = "domain\\unlinked",
            Servers = [],
            Certificates = []
        };
        _appServiceRepositoryMock
            .Setup(r => r.GetByIdAsync(6))
            .ReturnsAsync(service);

        // Act
        var cut = Render<ServicesDetails>(p => p.Add(d => d.Id, 6));

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("не привязан ни к одному серверу"));
    }

    [Test]
    public void Edit_WhenServiceExists_ShouldPopulateForm()
    {
        // Arrange
        var service = new AppService
        {
            Id = 10,
            Name = "Edit-Service",
            AdAccountName = "domain\\edit",
            Servers = [],
            Certificates = []
        };
        _appServiceRepositoryMock
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(service);
        _serverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<ServicesEdit>(p => p.Add(e => e.Id, 10));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("Edit-Service");
            markup.Should().Contain("domain\\edit");
        });
    }
}
