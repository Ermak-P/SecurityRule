using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using FirewallRulesIndex = SecurityRule.Web.Components.Pages.FirewallRules.Index;
using FirewallRulesCreate = SecurityRule.Web.Components.Pages.FirewallRules.Create;
using FirewallRulesEdit = SecurityRule.Web.Components.Pages.FirewallRules.Edit;

namespace SecurityRule.BDD.Tests.ComponentTests;

/// <summary>
/// bUnit component tests for the FirewallRules pages.
/// </summary>
[TestFixture]
public class FirewallRulesComponentTests : BunitTestContext
{
    private Mock<IFirewallRuleRepository> _firewallRuleRepositoryMock = null!;
    private Mock<ISnackbar> _snackbarMock = null!;

    [SetUp]
    public void SetUp()
    {
        _firewallRuleRepositoryMock = new Mock<IFirewallRuleRepository>();
        _snackbarMock = new Mock<ISnackbar>();

        Services.AddSingleton(_firewallRuleRepositoryMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(Mock.Of<IServerRepository>());
        Services.AddSingleton(Mock.Of<IAppServiceRepository>());
        Services.AddSingleton(Mock.Of<ICertificateRepository>());
        AddMudBlazorTestServices();
    }

    [Test]
    public void Index_WhenNoRules_ShouldRenderTable()
    {
        // Arrange
        _firewallRuleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<FirewallRulesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var table = cut.FindAll("table");
            table.Should().NotBeEmpty();
        });
    }

    [Test]
    public void Index_WhenRulesExist_ShouldRenderIPs()
    {
        // Arrange
        var rules = new List<FirewallRule>
        {
            new() { Id = 1, SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Allow HTTP" },
            new() { Id = 2, SourceIp = "192.168.1.1", DestinationIp = "192.168.1.2", ExpiresAt = DateTime.Now.AddDays(10), Description = "Allow SSH" }
        };
        _firewallRuleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var cut = Render<FirewallRulesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("10.0.0.1");
            markup.Should().Contain("10.0.0.2");
            markup.Should().Contain("192.168.1.1");
        });
    }

    [Test]
    public void Index_WhenExpiredRule_ShouldShowExpiredStatus()
    {
        // Arrange
        var rules = new List<FirewallRule>
        {
            new() { Id = 1, SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddDays(-5), Description = "Expired" }
        };
        _firewallRuleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var cut = Render<FirewallRulesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Истёк"));
    }

    [Test]
    public void Index_WhenRuleExpiresSoon_ShouldShowWarningSoonStatus()
    {
        // Arrange
        var rules = new List<FirewallRule>
        {
            new() { Id = 1, SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddDays(15), Description = "Expiring soon" }
        };
        _firewallRuleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var cut = Render<FirewallRulesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Скоро истекает"));
    }

    [Test]
    public void Index_WhenRuleIsActive_ShouldShowActiveStatus()
    {
        // Arrange
        var rules = new List<FirewallRule>
        {
            new() { Id = 1, SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddDays(90), Description = "Active" }
        };
        _firewallRuleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var cut = Render<FirewallRulesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Активен"));
    }

    [Test]
    public void Create_ShouldRenderForm()
    {
        // Act
        var cut = Render<FirewallRulesCreate>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Исходящий IP");
        markup.Should().Contain("Входящий IP");
        markup.Should().Contain("Описание");
    }

    [Test]
    public void Edit_WhenRuleExists_ShouldPopulateForm()
    {
        // Arrange
        var rule = new FirewallRule
        {
            Id = 3,
            SourceIp = "172.16.0.1",
            DestinationIp = "172.16.0.2",
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Edit me"
        };
        _firewallRuleRepositoryMock
            .Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(rule);

        // Act
        var cut = Render<FirewallRulesEdit>(p => p.Add(e => e.Id, 3));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("172.16.0.1");
            markup.Should().Contain("172.16.0.2");
        });
    }
}
