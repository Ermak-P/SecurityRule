using FluentAssertions;
using Reqnroll;
using SecurityRule.BDD.Tests.Support;
using SecurityRule.Domain.Models;

namespace SecurityRule.BDD.Tests.StepDefinitions;

[Binding]
public class CertificateStepDefinitions
{
    private readonly ScenarioState _state;
    private Certificate? _foundCertificate;

    public CertificateStepDefinitions(ScenarioState state)
    {
        _state = state;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the certificate database is empty")]
    public void GivenTheCertificateDatabaseIsEmpty() { }

    [Given("a certificate {string} issued {int} year(s) ago and expiring in {int} year(s) exists")]
    public async Task GivenACertificateExistsYears(string description, int issuedYearsAgo, int expiresInYears)
    {
        var cert = new Certificate
        {
            Description = description,
            IssuedAt = DateTime.Now.AddYears(-issuedYearsAgo),
            ExpiresAt = DateTime.Now.AddYears(expiresInYears)
        };
        await _state.CertificateRepository.AddAsync(cert);
        _state.LastCertificateId = cert.Id;
    }

    [Given("a certificate {string} issued today and expiring in {int} year(s) exists")]
    public async Task GivenACertificateIssuedTodayExistsYears(string description, int expiresInYears)
    {
        var cert = new Certificate
        {
            Description = description,
            IssuedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddYears(expiresInYears)
        };
        await _state.CertificateRepository.AddAsync(cert);
        _state.LastCertificateId = cert.Id;
    }

    [Given("the following certificates exist:")]
    public async Task GivenTheFollowingCertificatesExist(DataTable dataTable)
    {
        foreach (var row in dataTable.Rows)
        {
            var cert = new Certificate
            {
                Description = row["Description"],
                IssuedAt = DateTime.Now.AddDays(-int.Parse(row["IssuedDaysAgo"])),
                ExpiresAt = DateTime.Now.AddDays(int.Parse(row["ExpiresInDays"]))
            };
            await _state.CertificateRepository.AddAsync(cert);
        }
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When("I add a certificate with description {string}, issued {int} year(s) ago and expiring in {int} year(s)")]
    public async Task WhenIAddACertificateYears(string description, int issuedYearsAgo, int expiresInYears)
    {
        var cert = new Certificate
        {
            Description = description,
            IssuedAt = DateTime.Now.AddYears(-issuedYearsAgo),
            ExpiresAt = DateTime.Now.AddYears(expiresInYears)
        };
        await _state.CertificateRepository.AddAsync(cert);
        _state.LastCertificateId = cert.Id;
    }

    [When("I add a certificate with description {string}, issued {int} year(s) ago and expiry {int} day(s) ago")]
    public async Task WhenIAddAnExpiredCertificate(string description, int issuedYearsAgo, int expiredDaysAgo)
    {
        var cert = new Certificate
        {
            Description = description,
            IssuedAt = DateTime.Now.AddYears(-issuedYearsAgo),
            ExpiresAt = DateTime.Now.AddDays(-expiredDaysAgo)
        };
        await _state.CertificateRepository.AddAsync(cert);
        _state.LastCertificateId = cert.Id;
    }

    [When("I add a certificate with description {string}, issued {int} year(s) ago and expiring in {int} days")]
    public async Task WhenIAddACertificateExpiringInDays(string description, int issuedYearsAgo, int expiresInDays)
    {
        var cert = new Certificate
        {
            Description = description,
            IssuedAt = DateTime.Now.AddYears(-issuedYearsAgo),
            ExpiresAt = DateTime.Now.AddDays(expiresInDays)
        };
        await _state.CertificateRepository.AddAsync(cert);
        _state.LastCertificateId = cert.Id;
    }

    [When("I request all certificates")]
    public Task WhenIRequestAllCertificates() => Task.CompletedTask;

    [When("I search for the certificate by its ID")]
    public async Task WhenISearchForTheCertificateByItsId()
    {
        _foundCertificate = await _state.CertificateRepository.GetByIdAsync(_state.LastCertificateId);
    }

    [When("I search for the certificate with ID {int}")]
    public async Task WhenISearchForTheCertificateWithId(int id)
    {
        _foundCertificate = await _state.CertificateRepository.GetByIdAsync(id);
    }

    [When("I update the certificate description to {string}")]
    public async Task WhenIUpdateTheCertificateDescriptionTo(string newDescription)
    {
        var cert = await _state.CertificateRepository.GetByIdAsync(_state.LastCertificateId);
        cert.Should().NotBeNull();
        cert!.Description = newDescription;
        await _state.CertificateRepository.UpdateAsync(cert);
        _foundCertificate = await _state.CertificateRepository.GetByIdAsync(_state.LastCertificateId);
    }

    [When("I delete the certificate")]
    public async Task WhenIDeleteTheCertificate()
    {
        await _state.CertificateRepository.DeleteAsync(_state.LastCertificateId);
    }

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then("the certificate list should contain {int} certificate(s)")]
    public async Task ThenTheCertificateListShouldContain(int count)
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        certs.Should().HaveCount(count);
    }

    [Then("the certificate {string} should exist in the list")]
    public async Task ThenTheCertificateShouldExistInTheList(string description)
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        certs.Should().Contain(c => c.Description == description);
    }

    [Then("the certificate should be found")]
    public void ThenTheCertificateShouldBeFound()
    {
        _foundCertificate.Should().NotBeNull();
    }

    [Then("no certificate should be found")]
    public void ThenNoCertificateShouldBeFound()
    {
        _foundCertificate.Should().BeNull();
    }

    [Then("the certificate description should be {string}")]
    public void ThenTheCertificateDescriptionShouldBe(string description)
    {
        _foundCertificate!.Description.Should().Be(description);
    }

    [Then("the certificate should have the description {string}")]
    public void ThenTheCertificateShouldHaveTheDescription(string description)
    {
        _foundCertificate!.Description.Should().Be(description);
    }

    [Then("the certificate list should be empty")]
    public async Task ThenTheCertificateListShouldBeEmpty()
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        certs.Should().BeEmpty();
    }

    [Then("the certificate {string} should have an expiry date in the past")]
    public async Task ThenTheCertificateShouldHaveAnExpiryDateInThePast(string description)
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        var cert = certs.Single(c => c.Description == description);
        cert.ExpiresAt.Should().BeBefore(DateTime.Now);
    }

    [Then("the certificate {string} should expire within {int} days")]
    public async Task ThenTheCertificateShouldExpireWithinDays(string description, int days)
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        var cert = certs.Single(c => c.Description == description);
        cert.ExpiresAt.Should().BeBefore(DateTime.Now.AddDays(days));
        cert.ExpiresAt.Should().BeAfter(DateTime.Now);
    }

    [Then("the certificate {string} expiry date should be more than {int} days from now")]
    public async Task ThenTheCertificateExpiryShouldBeMoreThanDaysFromNow(string description, int days)
    {
        var certs = await _state.CertificateRepository.GetAllAsync();
        var cert = certs.Single(c => c.Description == description);
        cert.ExpiresAt.Should().BeAfter(DateTime.Now.AddDays(days));
    }
}
