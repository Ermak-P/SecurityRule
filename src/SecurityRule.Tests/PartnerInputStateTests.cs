using FluentAssertions;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

// Tests for DictionaryInputState have been migrated to DictionaryInputStateTests.cs.
[TestFixture]
public class PartnerInputStateTests
{
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Obsolete", "CS0618")]
#pragma warning disable CS0618
    public void PartnerInputState_Inherits_DictionaryInputState_And_Behaves_Identically()
    {
        // Arrange / Act
        var state = new PartnerInputState(["Alpha", "Beta"], ["Alpha"]);
#pragma warning restore CS0618

        // Assert: inherited behaviour works correctly through the subclass
        state.Should().BeAssignableTo<DictionaryInputState>();
        state.SelectedNames.Should().ContainSingle().Which.Should().Be("Alpha");
        state.AvailableItems.Should().HaveCount(2);
        state.Toggle("Beta");
        state.SelectedNames.Should().BeEquivalentTo(["Alpha", "Beta"]);
    }
}
