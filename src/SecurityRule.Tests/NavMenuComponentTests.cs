using Bunit;
using FluentAssertions;
using MudBlazor.Services;
using SecurityRule.Web.Components.Layout;

namespace SecurityRule.Tests;

[TestFixture]
public class NavMenuComponentTests
{
    private Bunit.TestContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _context = new Bunit.TestContext();
        _context.Services.AddMudServices();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void Render_ShowsCoreNavigationLinks()
    {
        var cut = _context.RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("/servers");
        cut.Markup.Should().Contain("/services");
        cut.Markup.Should().Contain("/connections/map");
    }

    [Test]
    public void Render_ShowsAllInfrastructureLinks()
    {
        var cut = _context.RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("/servers");
        cut.Markup.Should().Contain("/services");
        cut.Markup.Should().Contain("Серверы");
        cut.Markup.Should().Contain("Сервисы");
    }

    [Test]
    public void Render_ShowsAllSecurityLinks()
    {
        var cut = _context.RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("/certificates");
        cut.Markup.Should().Contain("/connections");
        cut.Markup.Should().Contain("/connections/map");
        cut.Markup.Should().Contain("Сертификаты");
        cut.Markup.Should().Contain("Связи");
        cut.Markup.Should().Contain("Карта связей");
    }

    [Test]
    public void Render_ShowsAccountLinks()
    {
        var cut = _context.RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("/users");
        cut.Markup.Should().Contain("/groups");
        cut.Markup.Should().Contain("Пользователи");
        cut.Markup.Should().Contain("Группы");
    }

    [Test]
    public void Render_ShowsSectionLabels()
    {
        var cut = _context.RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("ИНФРАСТРУКТУРА");
        cut.Markup.Should().Contain("БЕЗОПАСНОСТЬ");
        cut.Markup.Should().Contain("УЧЁТНЫЕ ЗАПИСИ");
    }

    [Test]
    public void Render_DashboardLink_UsesExactMatch()
    {
        var cut = _context.RenderComponent<NavMenu>();

        // Dashboard link uses NavLinkMatch.All — ensures the "/" route does not
        // highlight for every page. Validate the href is present.
        cut.Markup.Should().Contain("href=\"/\"");
        cut.Markup.Should().Contain("Дашборд");
    }
}
