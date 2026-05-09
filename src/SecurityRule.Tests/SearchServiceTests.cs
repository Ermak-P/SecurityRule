using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class SearchServiceTests
{
    private AppDbContext _context = null!;
    private SearchService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new SearchService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    // ── Граничные случаи ──────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsNull()
    {
        var result = await _service.SearchAsync(null!);
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsEmpty()
    {
        var result = await _service.SearchAsync(string.Empty);
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsWhitespace()
    {
        var result = await _service.SearchAsync("   ");
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsSingleCharacter()
    {
        _context.Servers.Add(new Server { Name = "Alpha", IpAddress = "1.2.3.4", OperatingSystem = "Linux" });
        await _context.SaveChangesAsync();

        var result = await _service.SearchAsync("A");

        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_ReturnsEmpty_WhenNothingMatches()
    {
        _context.Servers.Add(new Server { Name = "Server-A", IpAddress = "10.0.0.1", OperatingSystem = "Linux" });
        await _context.SaveChangesAsync();

        var result = await _service.SearchAsync("ZZZNOMATCH");

        result.Should().BeEmpty();
    }

    // ── Поиск по серверам ─────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_FindsServer_ByName()
    {
        _context.Servers.Add(new Server { Name = "Web-Server-01", IpAddress = "10.0.0.1", OperatingSystem = "Linux" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Web-Server")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервер" && r.FieldName == "Название" && r.FieldValue == "Web-Server-01");
    }

    [Test]
    public async Task SearchAsync_FindsServer_ByIpAddress()
    {
        _context.Servers.Add(new Server { Name = "DB-Server", IpAddress = "192.168.99.5", OperatingSystem = "Linux" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("192.168.99")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервер" && r.FieldName == "IP адрес" && r.FieldValue == "192.168.99.5");
    }

    [Test]
    public async Task SearchAsync_FindsServer_ByOperatingSystem()
    {
        _context.Servers.Add(new Server { Name = "Win-Box", IpAddress = "10.1.1.1", OperatingSystem = "Windows Server 2022" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Windows Server")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервер" && r.FieldName == "Операционная система" && r.FieldValue == "Windows Server 2022");
    }

    [Test]
    public async Task SearchAsync_FindsServer_ByDescription()
    {
        _context.Servers.Add(new Server { Name = "Backup-Srv", IpAddress = "10.2.2.2", OperatingSystem = "Linux", Description = "Резервный сервер" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Резервный")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервер" && r.FieldName == "Описание" && r.FieldValue == "Резервный сервер");
    }

    [Test]
    public async Task SearchAsync_ServerResult_NavigateUrl_PointsToServerDetails()
    {
        var server = new Server { Name = "Nav-Server", IpAddress = "10.3.3.3", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Nav-Server")).ToList();

        result.Should().ContainSingle(r => r.NavigateUrl == $"/servers/{server.Id}");
    }

    [Test]
    public async Task SearchAsync_DoesNotReturnServer_WhenDescriptionIsNull_AndDoesNotMatch()
    {
        _context.Servers.Add(new Server { Name = "NoDesc", IpAddress = "10.4.4.4", OperatingSystem = "Linux", Description = null });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("SomeDesc")).ToList();

        result.Should().BeEmpty();
    }

    // ── Поиск по сервисам ─────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_FindsService_ByName()
    {
        _context.AppServices.Add(new AppService { Name = "AuthService", UserName = "domain\\auth" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("AuthSer")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервис" && r.FieldName == "Название" && r.FieldValue == "AuthService");
    }

    [Test]
    public async Task SearchAsync_FindsService_ByUserName()
    {
        _context.AppServices.Add(new AppService { Name = "PayService", UserName = "domain\\johndoe" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("johndoe")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сервис" && r.FieldName == "AD учётная запись" && r.FieldValue == "domain\\johndoe");
    }

    [Test]
    public async Task SearchAsync_ServiceResult_NavigateUrl_PointsToServiceDetails()
    {
        var svc = new AppService { Name = "NavSvc", UserName = "domain\\nav" };
        _context.AppServices.Add(svc);
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("NavSvc")).ToList();

        result.Should().ContainSingle(r => r.NavigateUrl == $"/services/{svc.Id}");
    }

    // ── Поиск по пользователям ────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_FindsUser_ByName()
    {
        _context.Users.Add(new User { Name = "domain\\ermakov", Description = "Иванов Иван" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("ermakov")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Пользователь" && r.FieldName == "Название" && r.FieldValue == "domain\\ermakov");
    }

    [Test]
    public async Task SearchAsync_FindsUser_ByDescription()
    {
        _context.Users.Add(new User { Name = "domain\\ivanov", Description = "Технический директор" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("директор")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Пользователь" && r.FieldName == "Описание" && r.FieldValue == "Технический директор");
    }

    [Test]
    public async Task SearchAsync_UserResult_NavigateUrl_PointsToUserDetails()
    {
        var user = new User { Name = "domain\\navuser", Description = "" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("navuser")).ToList();

        result.Should().ContainSingle(r => r.NavigateUrl == $"/users/{user.Id}");
    }

    // ── Поиск по группам ──────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_FindsGroup_ByName()
    {
        _context.Groups.Add(new Group { Name = "Admins", Description = "Группа администраторов" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Admin")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Группа" && r.FieldName == "Название" && r.FieldValue == "Admins");
    }

    [Test]
    public async Task SearchAsync_FindsGroup_ByDescription()
    {
        _context.Groups.Add(new Group { Name = "DevTeam", Description = "Команда разработки" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("разработки")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Группа" && r.FieldName == "Описание" && r.FieldValue == "Команда разработки");
    }

    [Test]
    public async Task SearchAsync_GroupResult_NavigateUrl_PointsToGroupDetails()
    {
        var group = new Group { Name = "NavGroup", Description = "" };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("NavGroup")).ToList();

        result.Should().ContainSingle(r => r.NavigateUrl == $"/groups/{group.Id}");
    }

    // ── Поиск по сертификатам ─────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_FindsCertificate_ByDescription()
    {
        _context.Certificates.Add(new Certificate
        {
            SerialNumber = "SN-001",
            Thumbprint = "AABBCC",
            RequestNumber = "REQ-1",
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "SSL сертификат"
        });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("SSL")).ToList();

        result.Should().ContainSingle(r => r.EntityType == "Сертификат" && r.FieldName == "Описание" && r.FieldValue == "SSL сертификат");
    }

    [Test]
    public async Task SearchAsync_CertificateResult_NavigateUrl_PointsToCertificateEdit()
    {
        var cert = new Certificate
        {
            SerialNumber = "SN-NAV",
            Thumbprint = "NAVTHUMB",
            RequestNumber = "REQ-NAV",
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "NavCert"
        };
        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("NavCert")).ToList();

        result.Should().ContainSingle(r => r.NavigateUrl == $"/certificates/edit/{cert.Id}");
    }

    // ── Поиск по нескольким полям одновременно ────────────────────────────────

    [Test]
    public async Task SearchAsync_CanReturnMultipleResultsForOneEntity_WhenQueryMatchesSeveralFields()
    {
        _context.Servers.Add(new Server { Name = "Alpha", IpAddress = "10.0.0.1", OperatingSystem = "AlphaOS", Description = "Alpha node" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("Alpha")).ToList();

        // Matches Name, OperatingSystem, and Description → 3 results
        result.Should().HaveCount(3);
        result.Select(r => r.FieldName).Should().BeEquivalentTo(["Название", "Операционная система", "Описание"]);
    }

    [Test]
    public async Task SearchAsync_ReturnsResultsAcrossMultipleEntityTypes()
    {
        _context.Servers.Add(new Server { Name = "MyServer", IpAddress = "10.0.0.1", OperatingSystem = "Linux" });
        _context.AppServices.Add(new AppService { Name = "MyService", UserName = "domain\\svc" });
        _context.Groups.Add(new Group { Name = "MyGroup", Description = "" });
        await _context.SaveChangesAsync();

        var result = (await _service.SearchAsync("My")).ToList();

        result.Should().HaveCount(3);
        result.Select(r => r.EntityType).Should().BeEquivalentTo(["Сервер", "Сервис", "Группа"]);
    }
}
