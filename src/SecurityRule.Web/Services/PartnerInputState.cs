namespace SecurityRule.Web.Services;

/// <summary>
/// Pure logic for the partner-select dialog: tracks the current selection against
/// the full list of available partner names loaded from the database.
/// Kept framework-free so it can be unit-tested without a Blazor host.
/// </summary>
public class PartnerInputState
{
    private readonly List<string> _availablePartners;
    private readonly HashSet<string> _selected;

    public PartnerInputState(IEnumerable<string> availablePartners, IEnumerable<string>? preSelected = null)
    {
        _availablePartners = availablePartners.ToList();
        _selected = preSelected?.ToHashSet() ?? [];
    }

    public IReadOnlySet<string>   SelectedNames     => _selected;
    public IReadOnlyList<string>  AvailablePartners => _availablePartners;

    /// <summary>
    /// Returns partners whose Name contains <paramref name="filter"/>
    /// (case-insensitive). When filter is null or whitespace, all partners are returned.
    /// </summary>
    public IEnumerable<string> FilteredPartners(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return _availablePartners;
        return _availablePartners.Where(p =>
            p.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Selects the partner if not already selected; deselects it otherwise.
    /// Returns <c>true</c> when the partner ended up selected.
    /// </summary>
    public bool Toggle(string name)
    {
        if (_selected.Contains(name))
        {
            _selected.Remove(name);
            return false;
        }
        _selected.Add(name);
        return true;
    }

    public bool IsSelected(string name) => _selected.Contains(name);

    /// <summary>Adds all currently filtered partners to the selection.</summary>
    public void SelectAll(string? filter = null)
    {
        foreach (var p in FilteredPartners(filter))
            _selected.Add(p);
    }

    /// <summary>Clears the entire selection.</summary>
    public void DeselectAll() => _selected.Clear();

    /// <summary>Removes a single partner from the selection.</summary>
    public bool Remove(string name) => _selected.Remove(name);
}
