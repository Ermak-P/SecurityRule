namespace SecurityRule.Web.Models.Graph;

/// <summary>
/// The output produced by <see cref="SecurityRule.Web.Services.GraphMapElementsBuilder.Build"/>.
/// </summary>
/// <param name="Elements">
///     Flat list of Cytoscape node and edge elements ready for JS serialisation.
/// </param>
/// <param name="RelatedServerIds">
///     Server ids that are related (dimmed) to the current filter selection.
/// </param>
public sealed record GraphMapResult(
    IReadOnlyList<GraphElement> Elements,
    IReadOnlySet<int>           RelatedServerIds);
