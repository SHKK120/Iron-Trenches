using IronTrenches.Core;

var assertions = 0;

void Expect(bool condition, string message)
{
    assertions++;
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

void ExpectThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        assertions++;
        return;
    }

    throw new InvalidOperationException(message);
}

var destination = new WorldPoint(12.5f, -4f);
var move = new CommandOrder("squad-alpha", SquadCommandType.Move, destination);

Expect(move.SquadId == "squad-alpha", "Squad id was not retained.");
Expect(move.Type == SquadCommandType.Move, "Move command type was not retained.");
Expect(move.Destination.X == 12.5f, "Destination X was not retained.");
Expect(move.Destination.Z == -4f, "Destination Z was not retained.");

var sector = new SectorState("sector-west", "blue");
Expect(sector.OwnerId == "blue", "Initial sector owner was not retained.");

sector.TransferOwnershipTo("red");
Expect(sector.OwnerId == "red", "Sector ownership did not transfer.");

try
{
    _ = new CommandOrder("", SquadCommandType.AttackMove, destination);
    throw new InvalidOperationException("Empty squad id was accepted.");
}
catch (ArgumentException)
{
    assertions++;
}

var topology = new SectorTopology();
topology.RegisterSector("sector-west");
topology.RegisterSector("sector-center");
topology.RegisterSector("sector-east");
topology.RegisterSector("sector-west");

Expect(topology.ContainsSector("sector-west"), "Registered west sector was not retained.");
Expect(topology.ContainsSector("sector-center"), "Registered center sector was not retained.");
Expect(topology.ContainsSector("sector-east"), "Registered east sector was not retained.");

topology.AddBidirectionalAdjacency("sector-west", "sector-center");
topology.AddBidirectionalAdjacency("sector-center", "sector-east");
topology.AddBidirectionalAdjacency("sector-west", "sector-center");

Expect(topology.AreAdjacent("sector-west", "sector-center"), "West-to-center adjacency was not retained.");
Expect(topology.AreAdjacent("sector-center", "sector-west"), "Center-to-west adjacency was not bidirectional.");
Expect(!topology.AreAdjacent("sector-west", "sector-east"), "Non-adjacent sectors were reported as adjacent.");
Expect(topology.GetNeighbors("sector-west").Count == 1, "Duplicate adjacency accumulated for west.");
Expect(topology.GetNeighbors("sector-center").Count == 2, "Center neighbor count was incorrect.");
ExpectThrows<ArgumentException>(
    () => topology.AddBidirectionalAdjacency("sector-west", "sector-west"),
    "Self-adjacency was accepted.");
ExpectThrows<ArgumentException>(
    () => topology.RegisterSector(" "),
    "Blank sector id was accepted.");

var westCenter = new FrontlineEdge("sector-west", "sector-center");
var centerWest = new FrontlineEdge("sector-center", "sector-west");
Expect(westCenter == centerWest, "Frontline edge identity depended on endpoint order.");
Expect(
    new HashSet<FrontlineEdge> { westCenter, centerWest }.Count == 1,
    "Reversed frontline edges accumulated as duplicates.");

var west = new SectorState("sector-west", "blue");
var center = new SectorState("sector-center", "red");
var east = new SectorState("sector-east", "red");
var sectorStates = new[] { west, center, east };

var beforeCapture = FrontlineResolver.Resolve(topology, sectorStates);
Expect(beforeCapture.Count == 1, "Initial frontline count was not one.");
Expect(beforeCapture.Contains(westCenter), "West-center was not an initial frontline.");
Expect(
    !beforeCapture.Contains(new FrontlineEdge("sector-center", "sector-east")),
    "Same-owner center-east sectors formed a frontline.");
Expect(
    !beforeCapture.Contains(new FrontlineEdge("sector-west", "sector-east")),
    "Non-adjacent west-east sectors formed a frontline.");

var capture = SectorCapture.Complete(center, "blue");
Expect(capture.Changed, "Completed capture did not report an ownership change.");
Expect(capture.PreviousOwnerId == "red", "Capture result lost the previous owner.");
Expect(capture.NewOwnerId == "blue", "Capture result lost the new owner.");
Expect(center.OwnerId == "blue", "Completed capture did not update sector ownership.");

var afterCapture = FrontlineResolver.Resolve(topology, sectorStates);
Expect(afterCapture.Count == 1, "Post-capture frontline count was not one.");
Expect(
    afterCapture.Contains(new FrontlineEdge("sector-center", "sector-east")),
    "Center-east was not a frontline after center changed owner.");
Expect(!afterCapture.Contains(westCenter), "West-center remained a frontline after owners matched.");

var unchangedCapture = SectorCapture.Complete(center, "blue");
Expect(!unchangedCapture.Changed, "Same-owner capture reported a change.");
Expect(unchangedCapture.PreviousOwnerId == "blue", "Same-owner result lost the previous owner.");
Expect(unchangedCapture.NewOwnerId == "blue", "Same-owner result lost the requested owner.");
Expect(center.OwnerId == "blue", "Same-owner capture altered ownership.");
ExpectThrows<ArgumentException>(
    () => SectorCapture.Complete(center, ""),
    "Blank capture owner was accepted.");
ExpectThrows<ArgumentNullException>(
    () => SectorCapture.Complete(null!, "red"),
    "Null capture sector was accepted.");

var controlAnchor = new SectorControlAnchor("anchor-west", "territory-west");
Expect(controlAnchor.AnchorId == "anchor-west", "Control anchor id was not retained.");
Expect(controlAnchor.SectorId == "territory-west", "Control anchor sector id was not retained.");
ExpectThrows<ArgumentException>(
    () => new SectorControlAnchor("", "territory-west"),
    "Blank control anchor id was accepted.");
ExpectThrows<ArgumentException>(
    () => new SectorControlAnchor("anchor-invalid", " "),
    "Blank control anchor sector id was accepted.");

var territoryTopology = new SectorTopology();
territoryTopology.RegisterSector("territory-west");
territoryTopology.RegisterSector("territory-center");
territoryTopology.RegisterSector("territory-east");
territoryTopology.AddBidirectionalAdjacency("territory-west", "territory-center");
territoryTopology.AddBidirectionalAdjacency("territory-center", "territory-east");

var territoryWest = new SectorState("territory-west", "blue");
var territoryCenter = new SectorState("territory-center", "red");
var territoryEast = new SectorState("territory-east", "red");
var territoryStates = new[] { territoryWest, territoryCenter, territoryEast };
var territoryAnchors = new[]
{
    new SectorControlAnchor("anchor-west", "territory-west"),
    new SectorControlAnchor("anchor-center", "territory-center"),
    new SectorControlAnchor("anchor-east", "territory-east")
};
var territory = new TerritoryGraph(territoryTopology, territoryStates, territoryAnchors);

Expect(ReferenceEquals(territory.Topology, territoryTopology), "Territory topology reference was not retained.");
Expect(
    territory.GetControlAnchorForSector("territory-center").AnchorId == "anchor-center",
    "Sector-to-anchor lookup returned the wrong anchor.");
Expect(
    ReferenceEquals(territory.GetSectorForAnchor("anchor-center"), territoryCenter),
    "Anchor-to-sector lookup returned the wrong sector.");
Expect(
    territory.GetSectorForAnchor("anchor-west").OwnerId == "blue",
    "Anchor owner did not resolve through its sector owner.");

var territoryBeforeCapture = territory.GetFrontlines();
var territoryWestCenter = new FrontlineEdge("territory-west", "territory-center");
var territoryCenterEast = new FrontlineEdge("territory-center", "territory-east");
Expect(territoryBeforeCapture.Count == 1, "Territory initial frontline count was not one.");
Expect(territoryBeforeCapture.Contains(territoryWestCenter), "Territory initial west-center frontline was missing.");

var territoryCapture = territory.CompleteAnchorCapture("anchor-center", "blue");
Expect(territoryCapture.Changed, "Enemy anchor capture did not report a change.");
Expect(territoryCapture.PreviousOwnerId == "red", "Anchor capture lost the previous owner.");
Expect(territoryCapture.NewOwnerId == "blue", "Anchor capture lost the new owner.");
Expect(territoryCenter.OwnerId == "blue", "Anchor capture did not change its sector owner.");
Expect(territoryWest.OwnerId == "blue", "Anchor capture changed another sector owner.");
Expect(territoryEast.OwnerId == "red", "Anchor capture changed the east sector owner.");

var territoryAfterCapture = territory.GetFrontlines();
Expect(territoryAfterCapture.Count == 1, "Territory post-capture frontline count was not one.");
Expect(!territoryAfterCapture.Contains(territoryWestCenter), "Old territory frontline remained after capture.");
Expect(territoryAfterCapture.Contains(territoryCenterEast), "New territory frontline was missing after capture.");

var territorySameOwnerCapture = territory.CompleteAnchorCapture("anchor-center", "blue");
Expect(!territorySameOwnerCapture.Changed, "Same-owner anchor capture reported a change.");
Expect(
    territory.GetFrontlines().Contains(territoryCenterEast),
    "Same-owner anchor capture changed the frontline.");
ExpectThrows<KeyNotFoundException>(
    () => territory.CompleteAnchorCapture("anchor-missing", "blue"),
    "Missing control anchor capture was accepted.");
ExpectThrows<KeyNotFoundException>(
    () => territory.GetControlAnchorForSector("territory-missing"),
    "Missing sector anchor lookup was accepted.");

ExpectThrows<ArgumentException>(
    () => new TerritoryGraph(
        territoryTopology,
        territoryStates,
        new[]
        {
            new SectorControlAnchor("anchor-duplicate", "territory-west"),
            new SectorControlAnchor("anchor-duplicate", "territory-center"),
            new SectorControlAnchor("anchor-east", "territory-east")
        }),
    "Duplicate control anchor id was accepted.");
ExpectThrows<ArgumentException>(
    () => new TerritoryGraph(
        territoryTopology,
        territoryStates,
        new[]
        {
            new SectorControlAnchor("anchor-west-a", "territory-west"),
            new SectorControlAnchor("anchor-west-b", "territory-west"),
            new SectorControlAnchor("anchor-center", "territory-center"),
            new SectorControlAnchor("anchor-east", "territory-east")
        }),
    "Second control anchor for one sector was accepted.");
ExpectThrows<ArgumentException>(
    () => new TerritoryGraph(
        territoryTopology,
        territoryStates,
        new[]
        {
            new SectorControlAnchor("anchor-west", "territory-west"),
            new SectorControlAnchor("anchor-center", "territory-center"),
            new SectorControlAnchor("anchor-east", "territory-east"),
            new SectorControlAnchor("anchor-outside", "territory-outside")
        }),
    "Control anchor for an unregistered sector was accepted.");
ExpectThrows<ArgumentException>(
    () => new TerritoryGraph(
        territoryTopology,
        territoryStates,
        new[]
        {
            new SectorControlAnchor("anchor-west", "territory-west"),
            new SectorControlAnchor("anchor-center", "territory-center")
        }),
    "Territory with a sector missing its control anchor was accepted.");

var crossTopology = new SectorTopology();
foreach (var sectorId in new[] { "cross-west", "cross-center", "cross-east", "cross-north", "cross-south" })
{
    crossTopology.RegisterSector(sectorId);
}

crossTopology.AddBidirectionalAdjacency("cross-center", "cross-west");
crossTopology.AddBidirectionalAdjacency("cross-center", "cross-east");
crossTopology.AddBidirectionalAdjacency("cross-center", "cross-north");
crossTopology.AddBidirectionalAdjacency("cross-center", "cross-south");

var crossCenter = new SectorState("cross-center", "blue");
var crossEast = new SectorState("cross-east", "red");
var crossTerritory = new TerritoryGraph(
    crossTopology,
    new[]
    {
        new SectorState("cross-west", "blue"),
        crossCenter,
        crossEast,
        new SectorState("cross-north", "red"),
        new SectorState("cross-south", "red")
    },
    new[]
    {
        new SectorControlAnchor("cross-anchor-west", "cross-west"),
        new SectorControlAnchor("cross-anchor-center", "cross-center"),
        new SectorControlAnchor("cross-anchor-east", "cross-east"),
        new SectorControlAnchor("cross-anchor-north", "cross-north"),
        new SectorControlAnchor("cross-anchor-south", "cross-south")
    });

var crossCenterEast = new FrontlineEdge("cross-center", "cross-east");
var crossCenterNorth = new FrontlineEdge("cross-center", "cross-north");
var crossCenterSouth = new FrontlineEdge("cross-center", "cross-south");
var crossBeforeCapture = crossTerritory.GetFrontlines();
Expect(crossBeforeCapture.Count == 3, "Five-sector initial frontline count was not three.");
Expect(crossBeforeCapture.Contains(crossCenterEast), "Five-sector center-east frontline was missing.");
Expect(crossBeforeCapture.Contains(crossCenterNorth), "Five-sector center-north frontline was missing.");
Expect(crossBeforeCapture.Contains(crossCenterSouth), "Five-sector center-south frontline was missing.");

var crossCapture = crossTerritory.CompleteAnchorCapture("cross-anchor-east", "blue");
Expect(crossCapture.Changed, "Five-sector east anchor capture did not report a change.");
Expect(crossEast.OwnerId == "blue", "Five-sector east anchor capture did not update ownership.");
var crossAfterCapture = crossTerritory.GetFrontlines();
Expect(crossAfterCapture.Count == 2, "Five-sector post-capture frontline count was not two.");
Expect(!crossAfterCapture.Contains(crossCenterEast), "Captured center-east boundary remained a frontline.");
Expect(crossAfterCapture.Contains(crossCenterNorth), "Center-north frontline was lost after east capture.");
Expect(crossAfterCapture.Contains(crossCenterSouth), "Center-south frontline was lost after east capture.");

Console.WriteLine($"PASS ManagedPcChecks ({assertions} assertions)");
