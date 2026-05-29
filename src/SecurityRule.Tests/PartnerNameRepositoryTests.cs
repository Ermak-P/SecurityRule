using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class PartnerNameRepositoryTests
{
    private AppDbContext _context = null!;
    private PartnerNameRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new PartnerNameRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetAllAsync_Returns_Empty_When_No_PartnerNames_Exist()
    {
        var result = await _repository.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_Returns_All_PartnerNames_Ordered_By_Name()
    {
        _context.PartnerNames.AddRange(
            new PartnerName { Name = "Zebra Corp" },
            new PartnerName { Name = "Alpha Ltd" },
            new PartnerName { Name = "Middle Inc" });
        await _context.SaveChangesAsync();

        var result = (await _repository.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result.Select(p => p.Name).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task GetOrCreateAsync_Creates_New_PartnerName_When_Not_Exists()
    {
        var partner = await _repository.GetOrCreateAsync("New Partner");

        partner.Should().NotBeNull();
        partner.Name.Should().Be("New Partner");
        partner.Id.Should().BeGreaterThan(0);

        var inDb = await _context.PartnerNames.SingleAsync(p => p.Name == "New Partner");
        inDb.Id.Should().Be(partner.Id);
    }

    [Test]
    public async Task GetOrCreateAsync_Returns_Existing_PartnerName_When_Already_Exists()
    {
        var existing = new PartnerName { Name = "Existing Partner" };
        _context.PartnerNames.Add(existing);
        await _context.SaveChangesAsync();

        var result = await _repository.GetOrCreateAsync("Existing Partner");

        result.Id.Should().Be(existing.Id);
        var count = await _context.PartnerNames.CountAsync(p => p.Name == "Existing Partner");
        count.Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateAsync_Called_Twice_Does_Not_Duplicate_PartnerName()
    {
        await _repository.GetOrCreateAsync("DupPartner");
        await _repository.GetOrCreateAsync("DupPartner");

        var count = await _context.PartnerNames.CountAsync(p => p.Name == "DupPartner");
        count.Should().Be(1);
    }
}
