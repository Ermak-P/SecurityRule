using FluentAssertions;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class FakePartnerServiceTests
{
    [Test]
    public async Task GetPartnersAsync_Returns_Empty_By_Default()
    {
        var service = new FakePartnerService();

        var result = await service.GetPartnersAsync();

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetPartnersAsync_Returns_Set_Partners()
    {
        var service = new FakePartnerService();
        service.SetPartners([
            new PartnerInfo { Code = "A", Name = "Alpha" },
            new PartnerInfo { Code = "B", Name = "Beta" }
        ]);

        var result = (await service.GetPartnersAsync()).ToList();

        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().BeEquivalentTo(["Alpha", "Beta"]);
    }

    [Test]
    public async Task Reset_Clears_Partners()
    {
        var service = new FakePartnerService();
        service.SetPartners([new PartnerInfo { Code = "A", Name = "Alpha" }]);

        service.Reset();

        var result = await service.GetPartnersAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SetPartners_Replaces_Previous_List()
    {
        var service = new FakePartnerService();
        service.SetPartners([new PartnerInfo { Code = "A", Name = "Alpha" }]);
        service.SetPartners([new PartnerInfo { Code = "B", Name = "Beta" }]);

        var result = (await service.GetPartnersAsync()).ToList();

        result.Should().ContainSingle().Which.Name.Should().Be("Beta");
    }
}
