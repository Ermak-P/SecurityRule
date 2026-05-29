namespace SecurityRule.Web.Services;

/// <summary>
/// Obsolete: replaced by <see cref="DictionaryInputState"/>.
/// This type alias exists only for backward compatibility.
/// </summary>
[Obsolete("Use DictionaryInputState instead.", error: false)]
public class PartnerInputState : DictionaryInputState
{
    public PartnerInputState(IEnumerable<string> availablePartners, IEnumerable<string>? preSelected = null)
        : base(availablePartners, preSelected) { }
}
