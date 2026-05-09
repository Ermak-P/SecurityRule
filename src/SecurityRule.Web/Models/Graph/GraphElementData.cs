using System.Text.Json.Serialization;

namespace SecurityRule.Web.Models.Graph;

/// <summary>
/// Data payload of a single Cytoscape.js element (node or edge).
/// <para>
/// Node elements populate <see cref="Type"/>, <see cref="NodeType"/> and <see cref="Dimmed"/>;
/// edge elements populate <see cref="Source"/> and <see cref="Target"/> instead.
/// Fields irrelevant to a given element type are <see langword="null"/> and are
/// therefore omitted from the serialised JSON.
/// </para>
/// </summary>
public sealed record GraphElementData
{
    /// <summary>Unique element id used by Cytoscape (e.g. "srv-1", "svc-3", "edge-7").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Visible text label for nodes and edges.</summary>
    public string Label { get; init; } = string.Empty;

    // ── Node-only fields ─────────────────────────────────────────────────────

    /// <summary>"server" or "service" — drives Cytoscape style selectors.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>"server" or "app" — selects the background icon.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NodeType { get; init; }

    /// <summary>"1" if the node is a related (dimmed) node; "0" otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Dimmed { get; init; }

    /// <summary>Id of the compound-node parent (set for service nodes only).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parent { get; init; }

    // ── Edge-only fields ─────────────────────────────────────────────────────

    /// <summary>Source node id (edges only).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    /// <summary>Target node id (edges only).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Target { get; init; }

    /// <summary>"1" if the edge originates from a service node; "0" from a server node.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromService { get; init; }

    /// <summary>Free-text description shown in the edge tooltip.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}
