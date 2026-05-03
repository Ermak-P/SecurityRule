namespace SecurityRule.Web.Models.Graph;

/// <summary>
/// A single Cytoscape.js element in the standard <c>{ "data": … }</c> envelope.
/// </summary>
/// <param name="Data">The element data payload.</param>
public sealed record GraphElement(GraphElementData Data);
