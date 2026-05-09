using SecurityRule.Domain.Models;
using SecurityRule.Web.Models.Graph;

namespace SecurityRule.Web.Services;

/// <summary>
/// Builds the Cytoscape.js element graph from domain data and the current filter state.
/// All returned objects are fully typed — no anonymous or <see langword="object"/> types are used.
/// </summary>
public sealed class GraphMapElementsBuilder
{
    /// <summary>
    /// Builds the full set of Cytoscape elements for the given filter selection.
    /// </summary>
    /// <param name="connections">All service connections loaded from the repository.</param>
    /// <param name="servers">All servers loaded from the repository.</param>
    /// <param name="services">All services loaded from the repository (used for port lookup).</param>
    /// <param name="selectedServerIds">
    ///     Ids of servers currently selected in the filter.
    ///     Pass an empty collection to display all servers.
    /// </param>
    /// <param name="showRelated">
    ///     When <see langword="true"/>, servers that are connected to selected servers but not
    ///     themselves selected are included as dimmed (related) nodes.
    /// </param>
    /// <param name="showEdgeLabels">
    ///     When <see langword="true"/>, edge labels display port/protocol.
    ///     When <see langword="false"/>, edge labels are empty.
    /// </param>
    /// <param name="showServerIp">
    ///     When <see langword="true"/>, server node labels include the IP address on a second line.
    ///     When <see langword="false"/>, only the server name is shown.
    /// </param>
    public GraphMapResult Build(
        IEnumerable<ServiceConnection> connections,
        IEnumerable<Server>            servers,
        IEnumerable<AppService>        services,
        IReadOnlyCollection<int>       selectedServerIds,
        bool                           showRelated,
        bool                           showEdgeLabels = true,
        bool                           showServerIp   = true)
    {
        var allServers      = servers.ToList();
        var allServicesList = services.ToList();
        var allConnections  = connections.ToList();

        var selectedIds = selectedServerIds.ToHashSet();

        var primaryServers = selectedIds.Count == 0
            ? allServers
            : allServers.Where(s => selectedIds.Contains(s.Id)).ToList();

        var primaryServerIds = primaryServers.Select(s => s.Id).ToHashSet();

        // Build service→server lookup to infer the effective server
        // when DestinationServerId is not explicitly set.
        var svcToSrvId = BuildServiceToServerLookup(allServers);

        // Find related (connected-but-not-selected) servers.
        var relatedServerIds = showRelated && primaryServerIds.Count > 0 && selectedIds.Count > 0
            ? FindRelatedServerIds(allConnections, primaryServerIds, allServers, svcToSrvId)
            : (IReadOnlySet<int>)new HashSet<int>();

        var relatedServers      = allServers.Where(s => relatedServerIds.Contains(s.Id)).ToList();
        var allVisibleServers   = primaryServers.Concat(relatedServers).ToList();
        var allVisibleServerIds = allVisibleServers.Select(s => s.Id).ToHashSet();

        var elements        = new List<GraphElement>();
        var addedServiceIds = new HashSet<int>();

        // Primary server compound nodes.
        foreach (var srv in primaryServers.OrderBy(s => s.Name))
            elements.Add(CreateServerElement(srv, dimmed: false, showIp: showServerIp));

        // Related (dimmed) server compound nodes.
        foreach (var srv in relatedServers.OrderBy(s => s.Name))
            elements.Add(CreateServerElement(srv, dimmed: true, showIp: showServerIp));

        // Service nodes — children of their server compound nodes.
        foreach (var srv in allVisibleServers.OrderBy(s => s.Name))
        {
            bool isDimmed = relatedServerIds.Contains(srv.Id);
            foreach (var svc in srv.Services.OrderBy(s => s.Name))
            {
                if (addedServiceIds.Add(svc.Id))
                    elements.Add(CreateServiceElement(svc, parentId: $"srv-{srv.Id}", dimmed: isDimmed));
            }
        }

        // Edge elements — only between visible servers.
        foreach (var conn in allConnections)
        {
            int? srcSrvId = EffectiveServerId(conn.SourceServerId,      conn.SourceServiceId,      svcToSrvId);
            int? dstSrvId = EffectiveServerId(conn.DestinationServerId, conn.DestinationServiceId, svcToSrvId);

            bool srcInScope = srcSrvId.HasValue && allVisibleServerIds.Contains(srcSrvId.Value);
            bool dstInScope = dstSrvId.HasValue && allVisibleServerIds.Contains(dstSrvId.Value);
            if (!srcInScope || !dstInScope) continue;

            string srcNodeId = conn.SourceServiceId.HasValue && addedServiceIds.Contains(conn.SourceServiceId.Value)
                ? $"svc-{conn.SourceServiceId.Value}"
                : $"srv-{srcSrvId!.Value}";

            string dstNodeId = addedServiceIds.Contains(conn.DestinationServiceId)
                ? $"svc-{conn.DestinationServiceId}"
                : $"srv-{dstSrvId!.Value}";

            string label = showEdgeLabels
                ? BuildEdgeLabel(
                    conn.DestinationService?.Port
                        ?? allServicesList.FirstOrDefault(s => s.Id == conn.DestinationServiceId)?.Port,
                    conn.Protocol)
                : string.Empty;

            elements.Add(CreateEdgeElement(
                id:          $"edge-{conn.Id}",
                source:      srcNodeId,
                target:      dstNodeId,
                label:       label,
                fromService: conn.SourceServiceId.HasValue,
                description: conn.Description));
        }

        return new GraphMapResult(elements.AsReadOnly(), relatedServerIds);
    }

    // ── Private factory methods ───────────────────────────────────────────────

    private static GraphElement CreateServerElement(Server srv, bool dimmed, bool showIp)
    {
        string label = showIp && !string.IsNullOrWhiteSpace(srv.IpAddress)
            ? $"{srv.Name}\n{srv.IpAddress}"
            : srv.Name;

        return new GraphElement(new GraphElementData
        {
            Id       = $"srv-{srv.Id}",
            Label    = label,
            Type     = "server",
            NodeType = "server",
            Dimmed   = dimmed ? "1" : "0"
        });
    }

    private static GraphElement CreateServiceElement(AppService svc, string parentId, bool dimmed)
        => new(new GraphElementData
        {
            Id       = $"svc-{svc.Id}",
            Label    = svc.Name,
            Type     = "service",
            NodeType = "app",
            Dimmed   = dimmed ? "1" : "0",
            Parent   = parentId
        });

    private static GraphElement CreateEdgeElement(
        string id, string source, string target,
        string label, bool fromService, string? description)
        => new(new GraphElementData
        {
            Id          = id,
            Label       = label,
            Source      = source,
            Target      = target,
            FromService = fromService ? "1" : "0",
            Description = description ?? string.Empty
        });

    // ── Private helpers ───────────────────────────────────────────────────────

    private static Dictionary<int, int> BuildServiceToServerLookup(IEnumerable<Server> servers)
    {
        var lookup = new Dictionary<int, int>();
        foreach (var srv in servers)
            foreach (var svc in srv.Services)
                lookup.TryAdd(svc.Id, srv.Id);
        return lookup;
    }

    private static HashSet<int> FindRelatedServerIds(
        IEnumerable<ServiceConnection> connections,
        HashSet<int>                   primaryServerIds,
        IEnumerable<Server>            allServers,
        Dictionary<int, int>           svcToSrvId)
    {
        var allServerIdSet = allServers.Select(s => s.Id).ToHashSet();
        var related        = new HashSet<int>();

        foreach (var conn in connections)
        {
            int? srcSrv = EffectiveServerId(conn.SourceServerId,      conn.SourceServiceId,      svcToSrvId);
            int? dstSrv = EffectiveServerId(conn.DestinationServerId, conn.DestinationServiceId, svcToSrvId);

            if (srcSrv.HasValue && primaryServerIds.Contains(srcSrv.Value)
                && dstSrv.HasValue && !primaryServerIds.Contains(dstSrv.Value)
                && allServerIdSet.Contains(dstSrv.Value))
                related.Add(dstSrv.Value);

            if (dstSrv.HasValue && primaryServerIds.Contains(dstSrv.Value)
                && srcSrv.HasValue && !primaryServerIds.Contains(srcSrv.Value)
                && allServerIdSet.Contains(srcSrv.Value))
                related.Add(srcSrv.Value);
        }

        return related;
    }

    private static int? EffectiveServerId(int? explicitServerId, int? serviceId, Dictionary<int, int> svcToSrvId)
    {
        if (explicitServerId.HasValue) return explicitServerId;
        if (serviceId.HasValue && svcToSrvId.TryGetValue(serviceId.Value, out int sid)) return sid;
        return null;
    }

    /// <summary>
    /// Builds the edge label from a destination port and/or protocol.
    /// Returns an empty string when neither is specified.
    /// </summary>
    public static string BuildEdgeLabel(int? port, string? protocol)
    {
        bool hasPort  = port.HasValue;
        bool hasProto = !string.IsNullOrWhiteSpace(protocol);
        if (hasPort && hasProto) return $"{port}/{protocol}";
        if (hasPort)             return $"{port}";
        if (hasProto)            return protocol!;
        return string.Empty;
    }
}
