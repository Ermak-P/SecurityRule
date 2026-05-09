using Bunit;
using FluentAssertions;
using MudBlazor.Services;
using SecurityRule.Web.Components.Layout;

namespace SecurityRule.Tests;

[TestFixture]
public class NavMenuComponentTests
{
    private TestContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _context = new TestContext();
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
}
