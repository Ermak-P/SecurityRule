using FluentAssertions;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class TagInputStateTests
{
    // ── AddTag ────────────────────────────────────────────────────────────────

    [Test]
    public void AddTag_Adds_New_Tag_To_Selection()
    {
        var state = new TagInputState([]);

        var added = state.AddTag("backend");

        added.Should().BeTrue();
        state.SelectedTags.Should().ContainSingle().Which.Should().Be("backend");
    }

    [Test]
    public void AddTag_Trims_Whitespace_Before_Adding()
    {
        var state = new TagInputState([]);

        state.AddTag("  prod  ");

        state.SelectedTags.Should().ContainSingle().Which.Should().Be("prod");
    }

    [Test]
    public void AddTag_Returns_False_When_Tag_Already_Selected()
    {
        var state = new TagInputState([], ["existing"]);

        var added = state.AddTag("existing");

        added.Should().BeFalse();
        state.SelectedTags.Should().ContainSingle();
    }

    [Test]
    public void AddTag_Ignores_Empty_Or_Whitespace_Input()
    {
        var state = new TagInputState([]);

        state.AddTag("").Should().BeFalse();
        state.AddTag("   ").Should().BeFalse();

        state.SelectedTags.Should().BeEmpty();
    }

    [Test]
    public void AddTag_New_Tag_Not_In_Known_List_Is_Appended_To_KnownTags()
    {
        var state = new TagInputState(["alpha"]);

        state.AddTag("beta");

        state.KnownTags.Should().Contain("beta");
    }

    [Test]
    public void AddTag_Existing_Known_Tag_Does_Not_Duplicate_KnownTags()
    {
        var state = new TagInputState(["alpha"]);

        state.AddTag("alpha");

        state.KnownTags.Where(t => t == "alpha").Should().ContainSingle();
    }

    // ── RemoveTag ─────────────────────────────────────────────────────────────

    [Test]
    public void RemoveTag_Removes_Tag_From_Selection()
    {
        var state = new TagInputState([], ["backend", "frontend"]);

        var removed = state.RemoveTag("backend");

        removed.Should().BeTrue();
        state.SelectedTags.Should().ContainSingle().Which.Should().Be("frontend");
    }

    [Test]
    public void RemoveTag_Returns_False_When_Tag_Not_In_Selection()
    {
        var state = new TagInputState([]);

        state.RemoveTag("ghost").Should().BeFalse();
    }

    // ── SearchAvailable ───────────────────────────────────────────────────────

    [Test]
    public void SearchAvailable_Returns_All_Known_Tags_When_Filter_Is_Empty()
    {
        var state = new TagInputState(["alpha", "beta", "gamma"]);

        var result = state.SearchAvailable(string.Empty).ToList();

        result.Should().BeEquivalentTo(["alpha", "beta", "gamma"]);
    }

    [Test]
    public void SearchAvailable_Excludes_Already_Selected_Tags()
    {
        var state = new TagInputState(["alpha", "beta", "gamma"], ["beta"]);

        var result = state.SearchAvailable(string.Empty).ToList();

        result.Should().NotContain("beta");
        result.Should().BeEquivalentTo(["alpha", "gamma"]);
    }

    [Test]
    public void SearchAvailable_Filters_By_Substring_Case_Insensitive()
    {
        var state = new TagInputState(["production", "staging", "prod-db"]);

        var result = state.SearchAvailable("PROD").ToList();

        result.Should().BeEquivalentTo(["production", "prod-db"]);
    }

    [Test]
    public void SearchAvailable_Returns_Newly_Created_Tag_After_AddTag()
    {
        var state = new TagInputState([]);
        state.AddTag("new-tag");

        // After adding, the tag is selected, so search should NOT return it
        var inSearch = state.SearchAvailable(string.Empty).ToList();
        inSearch.Should().NotContain("new-tag");
    }

    // ── Pre-selected tags (edit scenario) ────────────────────────────────────

    [Test]
    public void Constructor_With_PreSelected_Sets_SelectedTags()
    {
        var state = new TagInputState(["alpha", "beta"], ["alpha"]);

        state.SelectedTags.Should().ContainSingle().Which.Should().Be("alpha");
    }

    [Test]
    public void Constructor_PreSelected_Tags_Are_Excluded_From_SearchAvailable()
    {
        var state = new TagInputState(["alpha", "beta", "gamma"], ["alpha", "gamma"]);

        var result = state.SearchAvailable(null).ToList();

        result.Should().ContainSingle().Which.Should().Be("beta");
    }
}
