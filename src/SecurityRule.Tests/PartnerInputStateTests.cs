using FluentAssertions;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class PartnerInputStateTests
{
    // ── FilteredPartners ─────────────────────────────────────────────────────

    [Test]
    public void FilteredPartners_Returns_All_When_Filter_Is_Empty()
    {
        var state = new PartnerInputState(["Alpha", "Beta"]);

        var result = state.FilteredPartners(string.Empty).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public void FilteredPartners_Returns_All_When_Filter_Is_Null()
    {
        var state = new PartnerInputState(["Alpha", "Beta"]);

        var result = state.FilteredPartners(null).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public void FilteredPartners_Filters_By_Name_Case_Insensitive()
    {
        var state = new PartnerInputState(["Alpha", "Beta", "Gamma"]);

        var result = state.FilteredPartners("al").ToList();

        result.Should().ContainSingle().Which.Should().Be("Alpha");
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Test]
    public void Toggle_Selects_Partner_When_Not_Selected()
    {
        var state = new PartnerInputState(["Alpha"]);

        var result = state.Toggle("Alpha");

        result.Should().BeTrue();
        state.SelectedNames.Should().Contain("Alpha");
    }

    [Test]
    public void Toggle_Deselects_Partner_When_Already_Selected()
    {
        var state = new PartnerInputState(["Alpha"], ["Alpha"]);

        var result = state.Toggle("Alpha");

        result.Should().BeFalse();
        state.SelectedNames.Should().NotContain("Alpha");
    }

    // ── IsSelected ────────────────────────────────────────────────────────────

    [Test]
    public void IsSelected_Returns_True_For_Selected_Partner()
    {
        var state = new PartnerInputState([], ["Alpha"]);

        state.IsSelected("Alpha").Should().BeTrue();
    }

    [Test]
    public void IsSelected_Returns_False_For_Unselected_Partner()
    {
        var state = new PartnerInputState([]);

        state.IsSelected("Unknown").Should().BeFalse();
    }

    // ── SelectAll ─────────────────────────────────────────────────────────────

    [Test]
    public void SelectAll_Selects_All_Available_Partners()
    {
        var state = new PartnerInputState(["Alpha", "Beta", "Gamma"]);

        state.SelectAll();

        state.SelectedNames.Should().BeEquivalentTo(["Alpha", "Beta", "Gamma"]);
    }

    [Test]
    public void SelectAll_With_Filter_Selects_Only_Filtered_Partners()
    {
        var state = new PartnerInputState(["Alpha", "Beta", "AlphaTwo"]);

        state.SelectAll("alph");

        state.SelectedNames.Should().BeEquivalentTo(["Alpha", "AlphaTwo"]);
        state.SelectedNames.Should().NotContain("Beta");
    }

    // ── DeselectAll ───────────────────────────────────────────────────────────

    [Test]
    public void DeselectAll_Clears_Selection()
    {
        var state = new PartnerInputState(["Alpha", "Beta"], ["Alpha", "Beta"]);

        state.DeselectAll();

        state.SelectedNames.Should().BeEmpty();
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Test]
    public void Remove_Removes_Partner_From_Selection()
    {
        var state = new PartnerInputState([], ["Alpha", "Beta"]);

        var removed = state.Remove("Alpha");

        removed.Should().BeTrue();
        state.SelectedNames.Should().ContainSingle().Which.Should().Be("Beta");
    }

    [Test]
    public void Remove_Returns_False_When_Not_Selected()
    {
        var state = new PartnerInputState([]);

        state.Remove("Ghost").Should().BeFalse();
    }

    // ── Pre-selected (edit scenario) ─────────────────────────────────────────

    [Test]
    public void Constructor_With_PreSelected_Sets_SelectedNames()
    {
        var state = new PartnerInputState(["Alpha", "Beta"], ["Alpha"]);

        state.SelectedNames.Should().ContainSingle().Which.Should().Be("Alpha");
    }

    [Test]
    public void AvailablePartners_Returns_Full_List_Regardless_Of_Selection()
    {
        var state = new PartnerInputState(["Alpha", "Beta"], ["Alpha"]);

        // Available partners include all (even selected ones), unlike TagInputState.
        state.AvailablePartners.Should().HaveCount(2);
    }
}
