namespace SecurityRule.Web.Services;

/// <summary>
/// Pure logic for the tag-input control: tracks selected tags, extends the known-tags
/// list with newly created tags, and provides filtered search.
/// Kept framework-free so it can be unit-tested without a Blazor host.
/// </summary>
public class TagInputState
{
    private readonly List<string> _knownTags;
    private readonly HashSet<string> _selected;

    public TagInputState(IEnumerable<string> existingTags, IEnumerable<string>? preSelected = null)
    {
        _knownTags = existingTags.ToList();
        _selected  = preSelected?.ToHashSet() ?? [];
    }

    public IReadOnlySet<string>    SelectedTags => _selected;
    public IReadOnlyList<string>   KnownTags    => _knownTags;

    /// <summary>
    /// Adds <paramref name="tag"/> to the selection.
    /// If the tag is not yet in the known list it is appended there too.
    /// Returns <c>true</c> when the tag was actually added (i.e. was not already selected).
    /// </summary>
    public bool AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        var trimmed = tag.Trim();
        if (!_knownTags.Contains(trimmed))
            _knownTags.Add(trimmed);
        return _selected.Add(trimmed);
    }

    /// <summary>Removes <paramref name="tag"/> from the selection.</summary>
    public bool RemoveTag(string tag) => _selected.Remove(tag);

    /// <summary>
    /// Renames <paramref name="oldTag"/> to <paramref name="newTag"/> inside the selection.
    /// If <paramref name="newTag"/> is already selected the old tag is simply removed (merge).
    /// Returns <c>false</c> when <paramref name="oldTag"/> was not selected.
    /// </summary>
    public bool RenameTag(string oldTag, string newTag)
    {
        if (!_selected.Remove(oldTag)) return false;
        var trimmed = newTag.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return true; // just remove, no replacement
        if (!_knownTags.Contains(trimmed))
            _knownTags.Add(trimmed);
        _selected.Add(trimmed); // HashSet.Add is idempotent: no-op if trimmed already present
        return true;
    }

    /// <summary>
    /// Returns known tags that are not yet selected and whose name contains
    /// <paramref name="filter"/> (case-insensitive).
    /// When <paramref name="filter"/> is null or whitespace all available tags are returned.
    /// </summary>
    public IEnumerable<string> SearchAvailable(string? filter)
    {
        var available = _knownTags.Where(t => !_selected.Contains(t));
        if (string.IsNullOrWhiteSpace(filter))
            return available;
        return available.Where(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
