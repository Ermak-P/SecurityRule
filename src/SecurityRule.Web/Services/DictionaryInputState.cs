namespace SecurityRule.Web.Services;

/// <summary>
/// Pure logic for a generic dictionary-selection dialog: tracks the current selection against
/// the full list of available items loaded from the database.
/// Kept framework-free so it can be unit-tested without a Blazor host.
/// </summary>
public class DictionaryInputState
{
    private readonly List<string> _availableItems;
    private readonly HashSet<string> _selected;

    public DictionaryInputState(IEnumerable<string> availableItems, IEnumerable<string>? preSelected = null)
    {
        _availableItems = availableItems.ToList();
        _selected = preSelected?.ToHashSet() ?? [];
    }

    public IReadOnlySet<string>  SelectedNames  => _selected;
    public IReadOnlyList<string> AvailableItems => _availableItems;

    /// <summary>
    /// Returns items whose name contains <paramref name="filter"/>
    /// (case-insensitive). When filter is null or whitespace, all items are returned.
    /// </summary>
    public IEnumerable<string> FilteredItems(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return _availableItems;
        return _availableItems.Where(p =>
            p.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Selects the item if not already selected; deselects it otherwise.
    /// Returns <c>true</c> when the item ended up selected.
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

    /// <summary>Adds all currently filtered items to the selection.</summary>
    public void SelectAll(string? filter = null)
    {
        foreach (var item in FilteredItems(filter))
            _selected.Add(item);
    }

    /// <summary>Clears the entire selection.</summary>
    public void DeselectAll() => _selected.Clear();

    /// <summary>Removes a single item from the selection.</summary>
    public bool Remove(string name) => _selected.Remove(name);
}
