using FluentAssertions;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class DictionaryInputStateTests
{
    // ── FilteredItems ─────────────────────────────────────────────────────────

    [Test]
    public void FilteredItems_Returns_All_When_Filter_Is_Empty()
    {
        var state = new DictionaryInputState(["Alpha", "Beta"]);

        var result = state.FilteredItems(string.Empty).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public void FilteredItems_Returns_All_When_Filter_Is_Null()
    {
        var state = new DictionaryInputState(["Alpha", "Beta"]);

        var result = state.FilteredItems(null).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public void FilteredItems_Filters_By_Name_Case_Insensitive()
    {
        var state = new DictionaryInputState(["Alpha", "Beta", "Gamma"]);

        var result = state.FilteredItems("al").ToList();

        result.Should().ContainSingle().Which.Should().Be("Alpha");
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Test]
    public void Toggle_Selects_Item_When_Not_Selected()
    {
        var state = new DictionaryInputState(["Alpha"]);

        var result = state.Toggle("Alpha");

        result.Should().BeTrue();
        state.SelectedNames.Should().Contain("Alpha");
    }

    [Test]
    public void Toggle_Deselects_Item_When_Already_Selected()
    {
        var state = new DictionaryInputState(["Alpha"], ["Alpha"]);

        var result = state.Toggle("Alpha");

        result.Should().BeFalse();
        state.SelectedNames.Should().NotContain("Alpha");
    }

    // ── IsSelected ────────────────────────────────────────────────────────────

    [Test]
    public void IsSelected_Returns_True_For_Selected_Item()
    {
        var state = new DictionaryInputState([], ["Alpha"]);

        state.IsSelected("Alpha").Should().BeTrue();
    }

    [Test]
    public void IsSelected_Returns_False_For_Unselected_Item()
    {
        var state = new DictionaryInputState([]);

        state.IsSelected("Unknown").Should().BeFalse();
    }

    // ── SelectAll ─────────────────────────────────────────────────────────────

    [Test]
    public void SelectAll_Selects_All_Available_Items()
    {
        var state = new DictionaryInputState(["Alpha", "Beta", "Gamma"]);

        state.SelectAll();

        state.SelectedNames.Should().BeEquivalentTo(["Alpha", "Beta", "Gamma"]);
    }

    [Test]
    public void SelectAll_With_Filter_Selects_Only_Filtered_Items()
    {
        var state = new DictionaryInputState(["Alpha", "Beta", "AlphaTwo"]);

        state.SelectAll("alph");

        state.SelectedNames.Should().BeEquivalentTo(["Alpha", "AlphaTwo"]);
        state.SelectedNames.Should().NotContain("Beta");
    }

    // ── DeselectAll ───────────────────────────────────────────────────────────

    [Test]
    public void DeselectAll_Clears_Selection()
    {
        var state = new DictionaryInputState(["Alpha", "Beta"], ["Alpha", "Beta"]);

        state.DeselectAll();

        state.SelectedNames.Should().BeEmpty();
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Test]
    public void Remove_Removes_Item_From_Selection()
    {
        var state = new DictionaryInputState([], ["Alpha", "Beta"]);

        var removed = state.Remove("Alpha");

        removed.Should().BeTrue();
        state.SelectedNames.Should().ContainSingle().Which.Should().Be("Beta");
    }

    [Test]
    public void Remove_Returns_False_When_Not_Selected()
    {
        var state = new DictionaryInputState([]);

        state.Remove("Ghost").Should().BeFalse();
    }

    // ── Pre-selected (edit scenario) ─────────────────────────────────────────

    [Test]
    public void Constructor_With_PreSelected_Sets_SelectedNames()
    {
        var state = new DictionaryInputState(["Alpha", "Beta"], ["Alpha"]);

        state.SelectedNames.Should().ContainSingle().Which.Should().Be("Alpha");
    }

    [Test]
    public void AvailableItems_Returns_Full_List_Regardless_Of_Selection()
    {
        var state = new DictionaryInputState(["Alpha", "Beta"], ["Alpha"]);

        state.AvailableItems.Should().HaveCount(2);
    }
}
