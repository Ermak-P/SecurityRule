using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class ServerFormServiceTests
{
    private AppDbContext _context = null!;
    private ServerFormService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _context.OperatingSystemOptions.AddRange(
            new OperatingSystemOption { Name = "Windows Server 2022" },
            new OperatingSystemOption { Name = "Ubuntu 22.04" });
        _context.AppServices.AddRange(
            new AppService { Name = "SvcA", UserName = @"domain\svc-a" },
            new AppService { Name = "SvcB", UserName = @"domain\svc-b" });
        _context.Tags.Add(new Tag { Name = "production" });
        await _context.SaveChangesAsync();

        _service = new ServerFormService(
            new ServerRepository(_context),
            new AppServiceRepository(_context),
            new OperatingSystemRepository(_context),
            new TagRepository(_context));
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetCreateModelAsync_WithCloneFrom_CopiesSourceData()
    {
        var source = new Server
        {
            Name = "SourceSrv",
            IpAddress = "10.10.10.1",
            OperatingSystem = "Ubuntu 22.04",
            Description = "source"
        };
        source.Services.Add(_context.AppServices.First());
        source.Tags.Add(_context.Tags.First());
        _context.Servers.Add(source);
        await _context.SaveChangesAsync();

        var model = await _service.GetCreateModelAsync(source.Id);

        model.Server.Name.Should().Be("SourceSrv");
        model.Server.IpAddress.Should().Be("10.10.10.1");
        model.SelectedServiceIds.Should().ContainSingle();
        model.SelectedTags.Should().Contain("production");
    }

    [Test]
    public async Task UpdateAsync_WhenServerExists_UpdatesServicesAndTags()
    {
        var existing = new Server
        {
            Name = "OldName",
            IpAddress = "10.0.0.1",
            OperatingSystem = "Windows Server 2022"
        };
        _context.Servers.Add(existing);
        await _context.SaveChangesAsync();

        var model = await _service.GetEditModelAsync(existing.Id);
        model.Should().NotBeNull();
        var detached = model!.Server;
        detached.Name = "NewName";
        var serviceId = _context.AppServices.OrderBy(s => s.Id).Last().Id;
        var result = await _service.UpdateAsync(detached, [serviceId], ["production", "new-tag"]);

        result.Should().BeTrue();
        var persisted = await _context.Servers.Include(s => s.Services).Include(s => s.Tags).SingleAsync();
        persisted.Name.Should().Be("NewName");
        persisted.Services.Select(s => s.Id).Should().BeEquivalentTo([serviceId]);
        persisted.Tags.Select(t => t.Name).Should().Contain(["production", "new-tag"]);
    }
}
