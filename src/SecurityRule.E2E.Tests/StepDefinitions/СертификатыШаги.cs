using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Сертификаты" (Certificates) feature.
/// </summary>
[Binding]
public sealed class СертификатыШаги
{
    private readonly ScenarioState _state;

    public СертификатыШаги(ScenarioState state) => _state = state;

    // ── Given: seed data directly into the in-memory database ────────────────

    /// <summary>Creates a certificate directly in the database.</summary>
    [Given("в системе существует сертификат Desc {string}")]
    public async Task ВСистемеСуществуетСертификат(string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.ICertificateRepository>();
        await repo.AddAsync(new Certificate
        {
            SerialNumber = "SN-TEST",
            Thumbprint = "TESTTHUMBPRINT",
            RequestNumber = "REQ-TEST",
            Description = description,
            IssuedAt = DateTime.Now.AddYears(-1),
            ExpiresAt = DateTime.Now.AddYears(2)
        });
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу сертификатов")]
    public async Task ПерейтиНаСтраницуСертификатов()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/certificates");
    }

    [When("я перехожу на страницу добавления сертификата")]
    public async Task ПерейтиНаСтраницуДобавленияСертификата()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/certificates/create");
    }

    [When("я открываю страницу деталей сертификата {string}")]
    public async Task ОткрытьСтраницуДеталейСертификата(string description)
    {
        var id = await GetCertificateIdAsync(description);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/certificates/{id}");
    }

    [When("я открываю страницу редактирования сертификата {string}")]
    public async Task ОткрытьСтраницуРедактированияСертификата(string description)
    {
        var id = await GetCertificateIdAsync(description);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/certificates/edit/{id}");
    }

    [When("я открываю страницу клонирования сертификата {string}")]
    public async Task ОткрытьСтраницуКлонированияСертификата(string description)
    {
        var id = await GetCertificateIdAsync(description);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/certificates/create?cloneFrom={id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetCertificateIdAsync(string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo  = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.ICertificateRepository>();
        var certs = await repo.GetAllAsync();
        return certs.First(c => c.Description == description).Id;
    }
}
