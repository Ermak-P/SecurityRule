using SecurityRule.Domain.Models;
using SecurityRule.Web.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class GraphMapElementsBuilderTests
{
    private GraphMapElementsBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new GraphMapElementsBuilder();

    // ── Edge label ────────────────────────────────────────────────────────────

    [Test]
    public void BuildEdgeLabel_WithPortAndProtocol_ReturnsCombined()
        => Assert.That(GraphMapElementsBuilder.BuildEdgeLabel(443, "HTTPS"), Is.EqualTo("443/HTTPS"));

    [Test]
    public void BuildEdgeLabel_WithPortOnly_ReturnsPort()
        => Assert.That(GraphMapElementsBuilder.BuildEdgeLabel(80, null), Is.EqualTo("80"));

    [Test]
    public void BuildEdgeLabel_WithProtocolOnly_ReturnsProtocol()
        => Assert.That(GraphMapElementsBuilder.BuildEdgeLabel(null, "TCP"), Is.EqualTo("TCP"));

    [Test]
    public void BuildEdgeLabel_WithNeither_ReturnsEmpty()
        => Assert.That(GraphMapElementsBuilder.BuildEdgeLabel(null, null), Is.EqualTo(string.Empty));

    [Test]
    public void BuildEdgeLabel_WithWhitespaceProtocol_ReturnsEmpty()
        => Assert.That(GraphMapElementsBuilder.BuildEdgeLabel(null, "   "), Is.EqualTo(string.Empty));

    // ── Empty data ────────────────────────────────────────────────────────────

    [Test]
    public void Build_EmptyData_ReturnsEmptyElements()
    {
        var result = _builder.Build([], [], [], [], showRelated: false);

        Assert.That(result.Elements,        Is.Empty);
        Assert.That(result.RelatedServerIds, Is.Empty);
    }

    // ── Server nodes ──────────────────────────────────────────────────────────

    [Test]
    public void Build_SingleServer_ProducesServerNode()
    {
        var server = MakeServer(1, "WebServer", "10.0.0.1");

        var result = _builder.Build([], [server], [], [1], showRelated: false);

        Assert.That(result.Elements, Has.Count.EqualTo(1));
        var data = result.Elements[0].Data;
        Assert.Multiple(() =>
        {
            Assert.That(data.Id,       Is.EqualTo("srv-1"));
            Assert.That(data.Label,    Is.EqualTo("WebServer\n10.0.0.1"));
            Assert.That(data.Type,     Is.EqualTo("server"));
            Assert.That(data.NodeType, Is.EqualTo("server"));
            Assert.That(data.Dimmed,   Is.EqualTo("0"));
            Assert.That(data.Parent,   Is.Null);
            Assert.That(data.Source,   Is.Null);
            Assert.That(data.Target,   Is.Null);
        });
    }

    [Test]
    public void Build_ServerWithoutIpAddress_LabelIsNameOnly()
    {
        var server = MakeServer(2, "DbServer", ipAddress: null);

        var result = _builder.Build([], [server], [], [2], showRelated: false);

        Assert.That(result.Elements[0].Data.Label, Is.EqualTo("DbServer"));
    }

    [Test]
    public void Build_AllServersSelectedWhenSelectedIdsEmpty()
    {
        var s1 = MakeServer(1, "A");
        var s2 = MakeServer(2, "B");

        var result = _builder.Build([], [s1, s2], [], [], showRelated: false);

        var nodeIds = result.Elements.Select(e => e.Data.Id);
        Assert.That(nodeIds, Is.EquivalentTo(new[] { "srv-1", "srv-2" }));
    }

    // ── Service nodes ─────────────────────────────────────────────────────────

    [Test]
    public void Build_ServerWithService_ProducesServiceChildNode()
    {
        var service = MakeService(10, "API", port: 8080);
        var server  = MakeServer(1, "AppServer", services: [service]);

        var result = _builder.Build([], [server], [service], [1], showRelated: false);

        Assert.That(result.Elements, Has.Count.EqualTo(2));
        var svcData = result.Elements.First(e => e.Data.Id == "svc-10").Data;
        Assert.Multiple(() =>
        {
            Assert.That(svcData.Id,       Is.EqualTo("svc-10"));
            Assert.That(svcData.Label,    Is.EqualTo("API"));
            Assert.That(svcData.Type,     Is.EqualTo("service"));
            Assert.That(svcData.NodeType, Is.EqualTo("app"));
            Assert.That(svcData.Dimmed,   Is.EqualTo("0"));
            Assert.That(svcData.Parent,   Is.EqualTo("srv-1"));
        });
    }

    [Test]
    public void Build_ServiceNotDuplicatedAcrossServers()
    {
        // Same service id attached to two servers (shouldn't happen in practice
        // but the builder must de-duplicate by id).
        var service = MakeService(10, "SharedSvc");
        var srv1    = MakeServer(1, "Srv1", services: [service]);
        var srv2    = MakeServer(2, "Srv2", services: [service]);

        var result = _builder.Build([], [srv1, srv2], [service], [1, 2], showRelated: false);

        var svcElements = result.Elements.Where(e => e.Data.Id == "svc-10").ToList();
        Assert.That(svcElements, Has.Count.EqualTo(1));
    }

    // ── Edges ─────────────────────────────────────────────────────────────────

    [Test]
    public void Build_ConnectionBetweenServices_ProducesEdge()
    {
        var srcSvc = MakeService(1, "Frontend");
        var dstSvc = MakeService(2, "Backend", port: 5000);
        var srv    = MakeServer(1, "AppSrv", services: [srcSvc, dstSvc]);
        var conn   = new ServiceConnection
        {
            Id                   = 7,
            SourceServerId       = 1,
            SourceServiceId      = 1,
            DestinationServerId  = 1,
            DestinationServiceId = 2,
            Protocol             = "TCP",
            Description          = "internal call"
        };

        var result = _builder.Build([conn], [srv], [srcSvc, dstSvc], [1], showRelated: false);

        var edge = result.Elements.First(e => e.Data.Id == "edge-7").Data;
        Assert.Multiple(() =>
        {
            Assert.That(edge.Source,      Is.EqualTo("svc-1"));
            Assert.That(edge.Target,      Is.EqualTo("svc-2"));
            Assert.That(edge.Label,       Is.EqualTo("5000/TCP"));
            Assert.That(edge.FromService, Is.EqualTo("1"));
            Assert.That(edge.Description, Is.EqualTo("internal call"));
            Assert.That(edge.Type,        Is.Null);
            Assert.That(edge.NodeType,    Is.Null);
        });
    }

    [Test]
    public void Build_ConnectionFromServer_FromServiceIsZero()
    {
        var dstSvc = MakeService(2, "DB", port: 5432);
        var srv1   = MakeServer(1, "AppSrv");
        var srv2   = MakeServer(2, "DbSrv", services: [dstSvc]);
        var conn   = new ServiceConnection
        {
            Id                   = 3,
            SourceServerId       = 1,
            DestinationServerId  = 2,
            DestinationServiceId = 2,
            Protocol             = "TCP"
        };

        var result = _builder.Build([conn], [srv1, srv2], [dstSvc], [1, 2], showRelated: false);

        var edge = result.Elements.First(e => e.Data.Id == "edge-3").Data;
        Assert.Multiple(() =>
        {
            Assert.That(edge.Source,      Is.EqualTo("srv-1"));
            Assert.That(edge.FromService, Is.EqualTo("0"));
        });
    }

    [Test]
    public void Build_ConnectionOutsideVisibleServers_EdgeNotIncluded()
    {
        var svc  = MakeService(1, "Svc");
        var srv1 = MakeServer(1, "A", services: [svc]);
        var srv2 = MakeServer(2, "B");
        var conn = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 2,
            DestinationServerId  = 1,
            DestinationServiceId = 1
        };

        // Only server 1 selected — server 2 is invisible → edge excluded
        var result = _builder.Build([conn], [srv1, srv2], [svc], [1], showRelated: false);

        Assert.That(result.Elements.Any(e => e.Data.Id == "edge-1"), Is.False);
    }

    // ── Related servers ───────────────────────────────────────────────────────

    [Test]
    public void Build_ShowRelated_AddsRelatedServerAsDimmed()
    {
        var svc1 = MakeService(1, "Frontend");
        var svc2 = MakeService(2, "Backend");
        var srv1 = MakeServer(1, "PrimaryServer", services: [svc1]);
        var srv2 = MakeServer(2, "RelatedServer",  services: [svc2]);
        var conn = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 1,
            SourceServiceId      = 1,
            DestinationServerId  = 2,
            DestinationServiceId = 2
        };

        var result = _builder.Build([conn], [srv1, srv2], [svc1, svc2], [1], showRelated: true);

        // Related server node must be present and dimmed
        var relatedNode = result.Elements.FirstOrDefault(e => e.Data.Id == "srv-2")?.Data;
        Assert.That(relatedNode,         Is.Not.Null);
        Assert.That(relatedNode!.Dimmed, Is.EqualTo("1"));

        // RelatedServerIds contains server 2
        Assert.That(result.RelatedServerIds, Contains.Item(2));
    }

    [Test]
    public void Build_ShowRelatedFalse_RelatedServerNotIncluded()
    {
        var svc1 = MakeService(1, "Frontend");
        var svc2 = MakeService(2, "Backend");
        var srv1 = MakeServer(1, "Primary",  services: [svc1]);
        var srv2 = MakeServer(2, "External", services: [svc2]);
        var conn = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 1,
            SourceServiceId      = 1,
            DestinationServerId  = 2,
            DestinationServiceId = 2
        };

        var result = _builder.Build([conn], [srv1, srv2], [svc1, svc2], [1], showRelated: false);

        Assert.That(result.Elements.Any(e => e.Data.Id == "srv-2"),    Is.False);
        Assert.That(result.RelatedServerIds, Is.Empty);
    }

    [Test]
    public void Build_RelatedServerServicesAreDimmed()
    {
        var svc1 = MakeService(1, "FE");
        var svc2 = MakeService(2, "BE");
        var srv1 = MakeServer(1, "Srv1", services: [svc1]);
        var srv2 = MakeServer(2, "Srv2", services: [svc2]);
        var conn = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 1,
            SourceServiceId      = 1,
            DestinationServerId  = 2,
            DestinationServiceId = 2
        };

        var result = _builder.Build([conn], [srv1, srv2], [svc1, svc2], [1], showRelated: true);

        var svcNode = result.Elements.FirstOrDefault(e => e.Data.Id == "svc-2")?.Data;
        Assert.That(svcNode,         Is.Not.Null);
        Assert.That(svcNode!.Dimmed, Is.EqualTo("1"));
    }

    // ── Filter selection ──────────────────────────────────────────────────────

    [Test]
    public void Build_FilterBySelectedServers_ExcludesUnselected()
    {
        var srv1 = MakeServer(1, "Selected");
        var srv2 = MakeServer(2, "NotSelected");

        var result = _builder.Build([], [srv1, srv2], [], [1], showRelated: false);

        var nodeIds = result.Elements.Select(e => e.Data.Id).ToList();
        Assert.That(nodeIds, Contains.Item("srv-1"));
        Assert.That(nodeIds, Does.Not.Contain("srv-2"));
    }

    // ── Effective server resolution ───────────────────────────────────────────

    [Test]
    public void Build_EdgeWithNullDestinationServerId_ResolvesViaServiceLookup()
    {
        var dstSvc = MakeService(5, "TargetSvc", port: 3306);
        var srcSrv = MakeServer(1, "Source");
        var dstSrv = MakeServer(2, "Destination", services: [dstSvc]);
        var conn   = new ServiceConnection
        {
            Id                   = 9,
            SourceServerId       = 1,
            DestinationServerId  = null,   // intentionally absent
            DestinationServiceId = 5,
            Protocol             = "MySQL"
        };

        var result = _builder.Build([conn], [srcSrv, dstSrv], [dstSvc], [1, 2], showRelated: false);

        var edge = result.Elements.FirstOrDefault(e => e.Data.Id == "edge-9")?.Data;
        Assert.That(edge,        Is.Not.Null);
        Assert.That(edge!.Target, Is.EqualTo("svc-5"));
    }

    // ── showEdgeLabels option ─────────────────────────────────────────────────

    [Test]
    public void Build_ShowEdgeLabels_True_IncludesPortProtocolLabel()
    {
        var dstSvc = MakeService(2, "API", port: 443);
        var srv    = MakeServer(1, "Srv", services: [dstSvc]);
        var conn   = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 1,
            DestinationServerId  = 1,
            DestinationServiceId = 2,
            Protocol             = "HTTPS"
        };

        var result = _builder.Build([conn], [srv], [dstSvc], [1], showRelated: false, showEdgeLabels: true);

        var edge = result.Elements.First(e => e.Data.Id == "edge-1").Data;
        Assert.That(edge.Label, Is.EqualTo("443/HTTPS"));
    }

    [Test]
    public void Build_ShowEdgeLabels_False_EdgeLabelIsEmpty()
    {
        var dstSvc = MakeService(2, "API", port: 443);
        var srv    = MakeServer(1, "Srv", services: [dstSvc]);
        var conn   = new ServiceConnection
        {
            Id                   = 1,
            SourceServerId       = 1,
            DestinationServerId  = 1,
            DestinationServiceId = 2,
            Protocol             = "HTTPS"
        };

        var result = _builder.Build([conn], [srv], [dstSvc], [1], showRelated: false, showEdgeLabels: false);

        var edge = result.Elements.First(e => e.Data.Id == "edge-1").Data;
        Assert.That(edge.Label, Is.EqualTo(string.Empty));
    }

    // ── showServerIp option ───────────────────────────────────────────────────

    [Test]
    public void Build_ShowServerIp_True_IncludesIpInLabel()
    {
        var server = MakeServer(1, "WebServer", "192.168.1.1");

        var result = _builder.Build([], [server], [], [1], showRelated: false, showServerIp: true);

        Assert.That(result.Elements[0].Data.Label, Is.EqualTo("WebServer\n192.168.1.1"));
    }

    [Test]
    public void Build_ShowServerIp_False_LabelIsNameOnly()
    {
        var server = MakeServer(1, "WebServer", "192.168.1.1");

        var result = _builder.Build([], [server], [], [1], showRelated: false, showServerIp: false);

        Assert.That(result.Elements[0].Data.Label, Is.EqualTo("WebServer"));
    }

    [Test]
    public void Build_ShowServerIp_False_ServerWithoutIp_LabelIsNameOnly()
    {
        var server = MakeServer(1, "NoIpServer", ipAddress: null);

        var result = _builder.Build([], [server], [], [1], showRelated: false, showServerIp: false);

        Assert.That(result.Elements[0].Data.Label, Is.EqualTo("NoIpServer"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Server MakeServer(
        int id, string name, string? ipAddress = null,
        IEnumerable<AppService>? services = null)
        => new()
        {
            Id        = id,
            Name      = name,
            IpAddress = ipAddress ?? string.Empty,
            Services  = services?.ToList() ?? []
        };

    private static AppService MakeService(int id, string name, int? port = null)
        => new() { Id = id, Name = name, Port = port };
}
