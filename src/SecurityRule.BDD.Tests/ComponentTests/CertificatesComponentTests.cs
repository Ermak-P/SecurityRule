using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using CertificatesIndex = SecurityRule.Web.Components.Pages.Certificates.Index;
using CertificatesCreate = SecurityRule.Web.Components.Pages.Certificates.Create;
using CertificatesEdit = SecurityRule.Web.Components.Pages.Certificates.Edit;

namespace SecurityRule.BDD.Tests.ComponentTests;

/// <summary>
/// bUnit component tests for the Certificates pages.
/// </summary>
[TestFixture]
public class CertificatesComponentTests : BunitTestContext
{
    private Mock<ICertificateRepository> _certificateRepositoryMock = null!;
    private Mock<ISnackbar> _snackbarMock = null!;

    [SetUp]
    public void SetUp()
    {
        _certificateRepositoryMock = new Mock<ICertificateRepository>();
        _snackbarMock = new Mock<ISnackbar>();

        Services.AddSingleton(_certificateRepositoryMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(Mock.Of<IServerRepository>());
        Services.AddSingleton(Mock.Of<IAppServiceRepository>());
        Services.AddSingleton(Mock.Of<IFirewallRuleRepository>());
        AddMudBlazorTestServices();
    }

    [Test]
    public void Index_WhenNoCertificates_ShouldRenderTable()
    {
        // Arrange
        _certificateRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var cut = Render<CertificatesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var table = cut.FindAll("table");
            table.Should().NotBeEmpty();
        });
    }

    [Test]
    public void Index_WhenCertificatesExist_ShouldRenderDescriptions()
    {
        // Arrange
        var certs = new List<Certificate>
        {
            new() { Id = 1, Description = "API Cert", IssuedAt = DateTime.Now.AddYears(-1), ExpiresAt = DateTime.Now.AddYears(1), Services = [] },
            new() { Id = 2, Description = "Web Cert", IssuedAt = DateTime.Now.AddYears(-2), ExpiresAt = DateTime.Now.AddMonths(6), Services = [] }
        };
        _certificateRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(certs);

        // Act
        var cut = Render<CertificatesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().Contain("API Cert");
            markup.Should().Contain("Web Cert");
        });
    }

    [Test]
    public void Index_WhenExpiredCertificate_ShouldShowExpiredStatus()
    {
        // Arrange
        var certs = new List<Certificate>
        {
            new() { Id = 1, Description = "Expired Cert", IssuedAt = DateTime.Now.AddYears(-2), ExpiresAt = DateTime.Now.AddDays(-5), Services = [] }
        };
        _certificateRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(certs);

        // Act
        var cut = Render<CertificatesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Истёк"));
    }

    [Test]
    public void Index_WhenCertificateExpiresSoon_ShouldShowWarningSoonStatus()
    {
        // Arrange
        var certs = new List<Certificate>
        {
            new() { Id = 1, Description = "Soon Cert", IssuedAt = DateTime.Now.AddYears(-1), ExpiresAt = DateTime.Now.AddDays(15), Services = [] }
        };
        _certificateRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(certs);

        // Act
        var cut = Render<CertificatesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Скоро истекает"));
    }

    [Test]
    public void Index_WhenCertificateIsActive_ShouldShowActiveStatus()
    {
        // Arrange
        var certs = new List<Certificate>
        {
            new() { Id = 1, Description = "Active Cert", IssuedAt = DateTime.Now.AddYears(-1), ExpiresAt = DateTime.Now.AddMonths(6), Services = [] }
        };
        _certificateRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(certs);

        // Act
        var cut = Render<CertificatesIndex>();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().ContainEquivalentOf("Активен"));
    }

    [Test]
    public void Create_ShouldRenderForm()
    {
        // Act
        var cut = Render<CertificatesCreate>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Описание");
        markup.Should().Contain("выдачи");
        markup.Should().Contain("истечения");
    }

    [Test]
    public void Edit_WhenCertificateExists_ShouldPopulateForm()
    {
        // Arrange
        var cert = new Certificate
        {
            Id = 7,
            Description = "Edit Cert",
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(1)
        };
        _certificateRepositoryMock
            .Setup(r => r.GetByIdAsync(7))
            .ReturnsAsync(cert);

        // Act
        var cut = Render<CertificatesEdit>(p => p.Add(e => e.Id, 7));

        // Assert
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Edit Cert"));
    }
}
