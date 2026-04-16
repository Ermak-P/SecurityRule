using FluentAssertions;
using Reqnroll;
using SecurityRule.BDD.Tests.Support;
using SecurityRule.Domain.Models;

namespace SecurityRule.BDD.Tests.StepDefinitions;

[Binding]
public class FirewallRuleStepDefinitions
{
    private readonly ScenarioState _state;
    private FirewallRule? _foundRule;

    public FirewallRuleStepDefinitions(ScenarioState state)
    {
        _state = state;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the firewall rule database is empty")]
    public void GivenTheFirewallRuleDatabaseIsEmpty() { }

    [Given("a firewall rule from {string} to {string} expiring in {int} year(s) with description {string} exists")]
    public async Task GivenAFirewallRuleExistsYears(string src, string dst, int expiresInYears, string description)
    {
        var rule = new FirewallRule
        {
            SourceIp = src,
            DestinationIp = dst,
            ExpiresAt = DateTime.Now.AddYears(expiresInYears),
            Description = description
        };
        await _state.FirewallRuleRepository.AddAsync(rule);
        _state.LastFirewallRuleId = rule.Id;
    }

    [Given("the following firewall rules exist:")]
    public async Task GivenTheFollowingFirewallRulesExist(DataTable dataTable)
    {
        foreach (var row in dataTable.Rows)
        {
            var rule = new FirewallRule
            {
                SourceIp = row["SourceIp"],
                DestinationIp = row["DestinationIp"],
                ExpiresAt = DateTime.Now.AddDays(int.Parse(row["ExpiresInDays"])),
                Description = row["Description"]
            };
            await _state.FirewallRuleRepository.AddAsync(rule);
        }
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When("I add a firewall rule from {string} to {string} expiring in {int} year(s) with description {string}")]
    public async Task WhenIAddAFirewallRuleYears(string src, string dst, int expiresInYears, string description)
    {
        var rule = new FirewallRule
        {
            SourceIp = src,
            DestinationIp = dst,
            ExpiresAt = DateTime.Now.AddYears(expiresInYears),
            Description = description
        };
        await _state.FirewallRuleRepository.AddAsync(rule);
        _state.LastFirewallRuleId = rule.Id;
    }

    [When("I add a firewall rule from {string} to {string} that expired {int} day(s) ago with description {string}")]
    public async Task WhenIAddAnExpiredFirewallRule(string src, string dst, int expiredDaysAgo, string description)
    {
        var rule = new FirewallRule
        {
            SourceIp = src,
            DestinationIp = dst,
            ExpiresAt = DateTime.Now.AddDays(-expiredDaysAgo),
            Description = description
        };
        await _state.FirewallRuleRepository.AddAsync(rule);
        _state.LastFirewallRuleId = rule.Id;
    }

    [When("I add a firewall rule from {string} to {string} expiring in {int} days with description {string}")]
    public async Task WhenIAddAFirewallRuleExpiringInDays(string src, string dst, int expiresInDays, string description)
    {
        var rule = new FirewallRule
        {
            SourceIp = src,
            DestinationIp = dst,
            ExpiresAt = DateTime.Now.AddDays(expiresInDays),
            Description = description
        };
        await _state.FirewallRuleRepository.AddAsync(rule);
        _state.LastFirewallRuleId = rule.Id;
    }

    [When("I request all firewall rules")]
    public Task WhenIRequestAllFirewallRules() => Task.CompletedTask;

    [When("I search for the firewall rule by its ID")]
    public async Task WhenISearchForTheFirewallRuleByItsId()
    {
        _foundRule = await _state.FirewallRuleRepository.GetByIdAsync(_state.LastFirewallRuleId);
    }

    [When("I search for the firewall rule with ID {int}")]
    public async Task WhenISearchForTheFirewallRuleWithId(int id)
    {
        _foundRule = await _state.FirewallRuleRepository.GetByIdAsync(id);
    }

    [When("I update the firewall rule description to {string}")]
    public async Task WhenIUpdateTheFirewallRuleDescriptionTo(string newDescription)
    {
        var rule = await _state.FirewallRuleRepository.GetByIdAsync(_state.LastFirewallRuleId);
        rule.Should().NotBeNull();
        rule!.Description = newDescription;
        await _state.FirewallRuleRepository.UpdateAsync(rule);
        _foundRule = await _state.FirewallRuleRepository.GetByIdAsync(_state.LastFirewallRuleId);
    }

    [When("I delete the firewall rule")]
    public async Task WhenIDeleteTheFirewallRule()
    {
        await _state.FirewallRuleRepository.DeleteAsync(_state.LastFirewallRuleId);
    }

    [When("I delete the firewall rule with ID {int}")]
    public async Task WhenIDeleteTheFirewallRuleWithId(int id)
    {
        try
        {
            await _state.FirewallRuleRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _state.ThrownException = ex;
        }
    }

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then("the firewall rule list should contain {int} rule(s)")]
    public async Task ThenTheFirewallRuleListShouldContain(int count)
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        rules.Should().HaveCount(count);
    }

    [Then("the rule with source {string} should exist in the list")]
    public async Task ThenTheRuleWithSourceShouldExistInTheList(string sourceIp)
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        rules.Should().Contain(r => r.SourceIp == sourceIp);
    }

    [Then("the firewall rule should be found")]
    public void ThenTheFirewallRuleShouldBeFound()
    {
        _foundRule.Should().NotBeNull();
    }

    [Then("no firewall rule should be found")]
    public void ThenNoFirewallRuleShouldBeFound()
    {
        _foundRule.Should().BeNull();
    }

    [Then("the firewall rule source IP should be {string}")]
    public void ThenTheFirewallRuleSourceIpShouldBe(string sourceIp)
    {
        _foundRule!.SourceIp.Should().Be(sourceIp);
    }

    [Then("the firewall rule should have the description {string}")]
    public void ThenTheFirewallRuleShouldHaveTheDescription(string description)
    {
        _foundRule!.Description.Should().Be(description);
    }

    [Then("the firewall rule list should be empty")]
    public async Task ThenTheFirewallRuleListShouldBeEmpty()
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        rules.Should().BeEmpty();
    }

    [Then("the firewall rule {string} should have an expiry date in the past")]
    public async Task ThenTheFirewallRuleShouldHaveAnExpiryDateInThePast(string description)
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        var rule = rules.Single(r => r.Description == description);
        rule.ExpiresAt.Should().BeBefore(DateTime.Now);
    }

    [Then("the firewall rule {string} should expire within {int} days")]
    public async Task ThenTheFirewallRuleShouldExpireWithinDays(string description, int days)
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        var rule = rules.Single(r => r.Description == description);
        rule.ExpiresAt.Should().BeBefore(DateTime.Now.AddDays(days));
        rule.ExpiresAt.Should().BeAfter(DateTime.Now);
    }

    [Then("the firewall rule {string} expiry date should be more than {int} days from now")]
    public async Task ThenTheFirewallRuleExpiryShouldBeMoreThanDaysFromNow(string description, int days)
    {
        var rules = await _state.FirewallRuleRepository.GetAllAsync();
        var rule = rules.Single(r => r.Description == description);
        rule.ExpiresAt.Should().BeAfter(DateTime.Now.AddDays(days));
    }

    [Then("no exception should be thrown for the firewall deletion")]
    public void ThenNoExceptionShouldBeThrownForFirewallDeletion()
    {
        _state.ThrownException.Should().BeNull();
    }
}
