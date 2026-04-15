using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class CertificateRepositoryTests
{
    private AppDbContext _context = null!;
    private CertificateRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new CertificateRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_ShouldAddCertificate()
    {
        var cert = new Certificate
        {
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Test cert"
        };

        await _repository.AddAsync(cert);

        var result = await _context.Certificates.ToListAsync();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Description, Is.EqualTo("Test cert"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllCertificates()
    {
        _context.Certificates.AddRange(
            new Certificate { IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" },
            new Certificate { IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(2), Description = "Cert2" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectCertificate()
    {
        var cert = new Certificate { IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(cert.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.EqualTo("Cert1"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateCertificate()
    {
        var cert = new Certificate { IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        cert.Description = "UpdatedCert";
        await _repository.UpdateAsync(cert);

        var result = await _context.Certificates.FindAsync(cert.Id);
        Assert.That(result!.Description, Is.EqualTo("UpdatedCert"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveCertificate()
    {
        var cert = new Certificate { IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(cert.Id);

        var result = await _context.Certificates.ToListAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task IsExpired_WhenExpiresAtIsInPast_ShouldBeTrue()
    {
        var cert = new Certificate
        {
            IssuedAt = DateTime.Now.AddYears(-2),
            ExpiresAt = DateTime.Now.AddDays(-1),
            Description = "Expired cert"
        };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(cert.Id);

        Assert.That(result!.ExpiresAt, Is.LessThan(DateTime.Now));
    }
}
