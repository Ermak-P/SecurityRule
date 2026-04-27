using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // Arrange
        var cert = new Certificate
        {
            SerialNumber = "SN-001",
            Thumbprint = "ABCDEF1234567890",
            RequestNumber = "REQ-001",
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Test cert"
        };

        // Act
        await _repository.AddAsync(cert);

        // Assert
        var result = await _context.Certificates.ToListAsync();
        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Test cert");
        result[0].SerialNumber.Should().Be("SN-001");
        result[0].Thumbprint.Should().Be("ABCDEF1234567890");
        result[0].RequestNumber.Should().Be("REQ-001");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllCertificates()
    {
        // Arrange
        _context.Certificates.AddRange(
            new Certificate { SerialNumber = "SN-001", Thumbprint = "AA", RequestNumber = "REQ-1", IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" },
            new Certificate { SerialNumber = "SN-002", Thumbprint = "BB", RequestNumber = "REQ-2", IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(2), Description = "Cert2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectCertificate()
    {
        // Arrange
        var cert = new Certificate { SerialNumber = "SN-001", Thumbprint = "AA", RequestNumber = "REQ-1", IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(cert.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("Cert1");
        result.SerialNumber.Should().Be("SN-001");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange – empty database

        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateCertificate()
    {
        // Arrange
        var cert = new Certificate { SerialNumber = "SN-001", Thumbprint = "AA", RequestNumber = "REQ-1", IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        // Act
        cert.Description = "UpdatedCert";
        cert.SerialNumber = "SN-UPDATED";
        cert.Thumbprint = "NEWHASH";
        cert.RequestNumber = "REQ-999";
        await _repository.UpdateAsync(cert);

        // Assert
        var result = await _context.Certificates.FindAsync(cert.Id);
        result!.Description.Should().Be("UpdatedCert");
        result.SerialNumber.Should().Be("SN-UPDATED");
        result.Thumbprint.Should().Be("NEWHASH");
        result.RequestNumber.Should().Be("REQ-999");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveCertificate()
    {
        // Arrange
        var cert = new Certificate { SerialNumber = "SN-001", Thumbprint = "AA", RequestNumber = "REQ-1", IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddYears(1), Description = "Cert1" };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(cert.Id);

        // Assert
        var result = await _context.Certificates.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task IsExpired_WhenExpiresAtIsInPast_ShouldBeTrue()
    {
        // Arrange
        var cert = new Certificate
        {
            SerialNumber = "SN-EXP",
            Thumbprint = "EXPTHUMB",
            RequestNumber = "REQ-EXP",
            IssuedAt = DateTime.Now.AddYears(-2),
            ExpiresAt = DateTime.Now.AddDays(-1),
            Description = "Expired cert"
        };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(cert.Id);

        // Assert
        result!.ExpiresAt.Should().BeBefore(DateTime.Now);
    }

    [Test]
    public async Task AddAsync_ShouldStoreAllNewFields()
    {
        // Arrange
        var cert = new Certificate
        {
            SerialNumber = "123456789",
            Thumbprint = "A1B2C3D4E5F6",
            Description = "SSL Certificate",
            RequestNumber = "JIRA-42",
            IssuedAt = new DateTime(2024, 1, 1),
            ExpiresAt = new DateTime(2026, 1, 1)
        };

        // Act
        await _repository.AddAsync(cert);

        // Assert
        var result = await _context.Certificates.FindAsync(cert.Id);
        result.Should().NotBeNull();
        result!.SerialNumber.Should().Be("123456789");
        result.Thumbprint.Should().Be("A1B2C3D4E5F6");
        result.Description.Should().Be("SSL Certificate");
        result.RequestNumber.Should().Be("JIRA-42");
        result.IssuedAt.Should().Be(new DateTime(2024, 1, 1));
        result.ExpiresAt.Should().Be(new DateTime(2026, 1, 1));
    }
}
