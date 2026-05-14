using FluentAssertions;
using SecurityRule.Domain.Models;
using SecurityRule.Domain.Validation;

namespace SecurityRule.Tests;

/// <summary>
/// Tests for <see cref="DomainInvariants"/> — covering valid paths and all invalid edge-cases.
/// </summary>
[TestFixture]
public class DomainInvariantsTests
{
    // ── ValidateServer — happy paths ──────────────────────────────────────────

    [Test]
    [TestCase("192.168.1.1")]
    [TestCase("10.0.0.1")]
    [TestCase("0.0.0.0")]
    [TestCase("255.255.255.255")]
    [TestCase("::1")]
    [TestCase("2001:db8::1")]
    public void ValidateServer_ValidIp_DoesNotThrow(string ip)
    {
        var server = new Server { Name = "Srv", IpAddress = ip, OperatingSystem = "Linux" };

        var act = () => DomainInvariants.ValidateServer(server);

        act.Should().NotThrow();
    }

    // ── ValidateServer — negative cases ───────────────────────────────────────

    [Test]
    public void ValidateServer_NullServer_ThrowsArgumentNullException()
    {
        var act = () => DomainInvariants.ValidateServer(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void ValidateServer_EmptyOrWhitespaceName_ThrowsDomainValidationException(string? name)
    {
        var server = new Server { Name = name!, IpAddress = "10.0.0.1", OperatingSystem = "Linux" };

        var act = () => DomainInvariants.ValidateServer(server);

        act.Should().Throw<DomainValidationException>();
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void ValidateServer_EmptyOrWhitespaceIpAddress_ThrowsDomainValidationException(string? ip)
    {
        var server = new Server { Name = "Srv", IpAddress = ip!, OperatingSystem = "Linux" };

        var act = () => DomainInvariants.ValidateServer(server);

        act.Should().Throw<DomainValidationException>();
    }

    [Test]
    [TestCase("not-an-ip")]
    [TestCase("256.256.256.256")]
    [TestCase("abc")]
    public void ValidateServer_InvalidIpFormat_ThrowsDomainValidationException(string ip)
    {
        var server = new Server { Name = "Srv", IpAddress = ip, OperatingSystem = "Linux" };

        var act = () => DomainInvariants.ValidateServer(server);

        act.Should().Throw<DomainValidationException>()
           .WithMessage("*IP*");
    }

    // ── ValidateServiceConnection — happy paths ───────────────────────────────

    [Test]
    [TestCase("TCP")]
    [TestCase("UDP")]
    [TestCase("ICMP")]
    [TestCase("ANY")]
    [TestCase("tcp")]
    [TestCase("udp")]
    [TestCase("")]
    [TestCase(null)]
    public void ValidateServiceConnection_ValidProtocol_DoesNotThrow(string? protocol)
    {
        var conn = new ServiceConnection
        {
            DestinationServiceId = 1,
            Protocol = protocol!
        };

        var act = () => DomainInvariants.ValidateServiceConnection(conn);

        act.Should().NotThrow();
    }

    // ── ValidateServiceConnection — negative cases ────────────────────────────

    [Test]
    public void ValidateServiceConnection_NullConnection_ThrowsArgumentNullException()
    {
        var act = () => DomainInvariants.ValidateServiceConnection(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-100)]
    public void ValidateServiceConnection_InvalidDestinationServiceId_ThrowsDomainValidationException(int id)
    {
        var conn = new ServiceConnection { DestinationServiceId = id };

        var act = () => DomainInvariants.ValidateServiceConnection(conn);

        act.Should().Throw<DomainValidationException>();
    }

    [Test]
    [TestCase("FTP")]
    [TestCase("HTTP")]
    [TestCase("GRE")]
    [TestCase("INVALID")]
    public void ValidateServiceConnection_UnknownProtocol_ThrowsDomainValidationException(string protocol)
    {
        var conn = new ServiceConnection
        {
            DestinationServiceId = 1,
            Protocol = protocol
        };

        var act = () => DomainInvariants.ValidateServiceConnection(conn);

        act.Should().Throw<DomainValidationException>()
           .WithMessage("*протокол*");
    }
}
