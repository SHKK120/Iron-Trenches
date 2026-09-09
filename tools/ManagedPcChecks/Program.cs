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

void ExpectPlacementFailure(
    BuildingPlacementResult result,
    BuildingPlacementFailureReason expectedReason,
    EconomyState economy,
    long expectedBalance,
    ICollection<ConstructionSite> sites,
    int expectedSiteCount,
    string context)
{
    Expect(!result.Success, $"{context}: placement unexpectedly succeeded.");
    Expect(result.FailureReason == expectedReason, $"{context}: failure reason was incorrect.");
    Expect(result.ConstructionSite == null, $"{context}: a failed placement returned a site.");
    Expect(result.SpentAmount == 0, $"{context}: a failed placement reported spending.");
    Expect(result.PreviousBalance == expectedBalance, $"{context}: previous balance was incorrect.");
    Expect(result.NewBalance == expectedBalance, $"{context}: new balance was incorrect.");
    Expect(economy.Balance == expectedBalance, $"{context}: economy balance changed.");
    Expect(sites.Count == expectedSiteCount, $"{context}: construction site collection changed.");
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

var incomeProfile = new SectorIncomeProfile("income-west", 40);
Expect(incomeProfile.SectorId == "income-west", "Income profile sector id was not retained.");
Expect(incomeProfile.IncomeValue == 40, "Income profile value was not retained.");
ExpectThrows<ArgumentException>(
    () => new SectorIncomeProfile(" ", 40),
    "Blank income profile sector id was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new SectorIncomeProfile("income-negative", -1),
    "Negative sector income was accepted.");
Expect(
    new SectorIncomeProfile("income-zero", 0).IncomeValue == 0,
    "Zero-income sector was not accepted.");

var initialEconomy = new EconomyState("blue", 25);
Expect(initialEconomy.FactionId == "blue", "Economy faction id was not retained.");
Expect(initialEconomy.Balance == 25, "Initial economy balance was not retained.");
ExpectThrows<ArgumentException>(
    () => new EconomyState("", 0),
    "Blank economy faction id was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new EconomyState("blue", -1),
    "Negative initial economy balance was accepted.");

var economyTopology = new SectorTopology();
foreach (var sectorId in new[] { "economy-west", "economy-center", "economy-east", "economy-north", "economy-south" })
{
    economyTopology.RegisterSector(sectorId);
}

economyTopology.AddBidirectionalAdjacency("economy-west", "economy-center");
economyTopology.AddBidirectionalAdjacency("economy-center", "economy-east");
economyTopology.AddBidirectionalAdjacency("economy-center", "economy-north");
economyTopology.AddBidirectionalAdjacency("economy-center", "economy-south");

var economyWest = new SectorState("economy-west", "blue");
var economyCenter = new SectorState("economy-center", "red");
var economyEast = new SectorState("economy-east", "red");
var economyNorth = new SectorState("economy-north", "red");
var economySouth = new SectorState("economy-south", "blue");
var economyTerritory = new TerritoryGraph(
    economyTopology,
    new[] { economyWest, economyCenter, economyEast, economyNorth, economySouth },
    new[]
    {
        new SectorControlAnchor("economy-anchor-west", "economy-west"),
        new SectorControlAnchor("economy-anchor-center", "economy-center"),
        new SectorControlAnchor("economy-anchor-east", "economy-east"),
        new SectorControlAnchor("economy-anchor-north", "economy-north"),
        new SectorControlAnchor("economy-anchor-south", "economy-south")
    });
var economyProfiles = new[]
{
    new SectorIncomeProfile("economy-west", 40),
    new SectorIncomeProfile("economy-center", 60),
    new SectorIncomeProfile("economy-east", 50),
    new SectorIncomeProfile("economy-north", 35),
    new SectorIncomeProfile("economy-south", 25)
};

Expect(
    SectorIncomeResolver.Resolve(economyTerritory, economyProfiles, "blue") == 65,
    "Blue income did not include only blue-owned sectors.");
Expect(
    SectorIncomeResolver.Resolve(economyTerritory, economyProfiles, "red") == 145,
    "Red income did not include only red-owned sectors.");
Expect(
    SectorIncomeResolver.Resolve(economyTerritory, economyProfiles, "green") == 0,
    "Faction with no owned sectors did not resolve zero income.");

ExpectThrows<ArgumentException>(
    () => SectorIncomeResolver.Resolve(
        economyTerritory,
        new[]
        {
            new SectorIncomeProfile("economy-west", 40),
            new SectorIncomeProfile("economy-west", 80),
            new SectorIncomeProfile("economy-center", 60),
            new SectorIncomeProfile("economy-east", 50),
            new SectorIncomeProfile("economy-north", 35),
            new SectorIncomeProfile("economy-south", 25)
        },
        "blue"),
    "Duplicate sector income profile was accepted.");
ExpectThrows<ArgumentException>(
    () => SectorIncomeResolver.Resolve(
        economyTerritory,
        new[]
        {
            new SectorIncomeProfile("economy-west", 40),
            new SectorIncomeProfile("economy-center", 60),
            new SectorIncomeProfile("economy-east", 50),
            new SectorIncomeProfile("economy-south", 25)
        },
        "blue"),
    "Missing sector income profile was accepted.");
ExpectThrows<ArgumentException>(
    () => SectorIncomeResolver.Resolve(
        economyTerritory,
        economyProfiles.Append(new SectorIncomeProfile("economy-outside", 10)),
        "blue"),
    "Income profile for an unregistered sector was accepted.");

var repeatedEconomy = new EconomyState("blue", 0);
var firstRepeatedCollection = SectorIncomeCollector.Collect(
    economyTerritory,
    economyProfiles,
    repeatedEconomy);
var secondRepeatedCollection = SectorIncomeCollector.Collect(
    economyTerritory,
    economyProfiles,
    repeatedEconomy);
Expect(firstRepeatedCollection.CollectedAmount == 65, "First repeated collection amount was incorrect.");
Expect(firstRepeatedCollection.PreviousBalance == 0, "First repeated collection previous balance was incorrect.");
Expect(firstRepeatedCollection.NewBalance == 65, "First repeated collection new balance was incorrect.");
Expect(secondRepeatedCollection.PreviousBalance == 65, "Second collection did not start at the first balance.");
Expect(secondRepeatedCollection.NewBalance == 130, "Second collection did not accumulate income.");
Expect(repeatedEconomy.Balance == 130, "Economy state did not retain accumulated income.");

var blueEconomy = new EconomyState("blue", 0);
var blueInitialCollection = SectorIncomeCollector.Collect(
    economyTerritory,
    economyProfiles,
    blueEconomy);
Expect(blueInitialCollection.FactionId == "blue", "Collection result lost the faction id.");
Expect(blueInitialCollection.CollectedAmount == 65, "Initial integrated blue collection was not 65.");
Expect(blueEconomy.Balance == 65, "Initial integrated blue balance was not 65.");

var redEconomy = new EconomyState("red", 0);
var redCollection = SectorIncomeCollector.Collect(economyTerritory, economyProfiles, redEconomy);
Expect(redCollection.CollectedAmount == 145, "Initial red collection was not 145.");
Expect(redEconomy.Balance == 145, "Red balance did not receive red income.");
Expect(blueEconomy.Balance == 65, "Red collection changed the blue balance.");

var economyBeforeCaptureFrontlines = economyTerritory.GetFrontlines();
Expect(
    economyBeforeCaptureFrontlines.Contains(new FrontlineEdge("economy-west", "economy-center")),
    "Economy fixture initial west-center frontline was missing.");
var economyCapture = economyTerritory.CompleteAnchorCapture("economy-anchor-center", "blue");
Expect(economyCapture.Changed, "Economy fixture center capture did not change ownership.");
Expect(economyCenter.OwnerId == "blue", "Economy fixture center owner did not become blue.");
var economyAfterCaptureFrontlines = economyTerritory.GetFrontlines();
Expect(
    !economyAfterCaptureFrontlines.Contains(new FrontlineEdge("economy-west", "economy-center")),
    "Economy fixture old west-center frontline remained after capture.");
Expect(
    economyAfterCaptureFrontlines.Contains(new FrontlineEdge("economy-center", "economy-east")),
    "Economy fixture new center-east frontline was missing after capture.");
Expect(
    SectorIncomeResolver.Resolve(economyTerritory, economyProfiles, "blue") == 125,
    "Center capture did not increase blue income from 65 to 125.");

var bluePostCaptureCollection = SectorIncomeCollector.Collect(
    economyTerritory,
    economyProfiles,
    blueEconomy);
Expect(bluePostCaptureCollection.CollectedAmount == 125, "Post-capture collection amount was not 125.");
Expect(bluePostCaptureCollection.PreviousBalance == 65, "Post-capture previous balance was not 65.");
Expect(bluePostCaptureCollection.NewBalance == 190, "Post-capture new balance was not 190.");
Expect(blueEconomy.Balance == 190, "Post-capture economy balance was not retained.");

var overflowEconomy = new EconomyState("blue", long.MaxValue);
ExpectThrows<OverflowException>(
    () => SectorIncomeCollector.Collect(economyTerritory, economyProfiles, overflowEconomy),
    "Economy balance overflow wrapped silently.");

var spendingEconomy = new EconomyState("blue", 25);
Expect(spendingEconomy.CanAfford(25), "Economy could not afford its exact balance.");
Expect(!spendingEconomy.CanAfford(26), "Economy afforded more than its balance.");
Expect(spendingEconomy.Spend(10), "Affordable spending failed.");
Expect(spendingEconomy.Balance == 15, "Successful spending did not reduce balance.");
Expect(!spendingEconomy.Spend(16), "Unaffordable spending succeeded.");
Expect(spendingEconomy.Balance == 15, "Failed spending changed balance.");
Expect(spendingEconomy.Spend(0), "Zero spending was not accepted.");
Expect(spendingEconomy.Balance == 15, "Zero spending changed balance.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => spendingEconomy.CanAfford(-1),
    "Negative affordability amount was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => spendingEconomy.Spend(-1),
    "Negative spending amount was accepted.");

var barracksDefinition = new BuildingDefinition("barracks", 50, 18f);
var depotDefinition = new BuildingDefinition("depot", 35, 14f);
Expect(barracksDefinition.BuildingTypeId == "barracks", "Building type id was not retained.");
Expect(barracksDefinition.Cost == 50, "Building cost was not retained.");
Expect(barracksDefinition.FootprintRadius == 18f, "Building footprint was not retained.");
Expect(new BuildingDefinition("free-marker", 0, 1f).Cost == 0, "Zero-cost definition was rejected.");
ExpectThrows<ArgumentException>(
    () => new BuildingDefinition(" ", 50, 18f),
    "Blank building type id was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingDefinition("negative-cost", -1, 18f),
    "Negative building cost was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingDefinition("zero-radius", 1, 0f),
    "Zero footprint radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingDefinition("negative-radius", 1, -1f),
    "Negative footprint radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingDefinition("nan-radius", 1, float.NaN),
    "NaN footprint radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingDefinition("infinite-radius", 1, float.PositiveInfinity),
    "Infinite footprint radius was accepted.");

var placementPoint = new WorldPoint(37.3f, 42.8f);
var placementRequest = new BuildingPlacementRequest(
    "site-west-barracks",
    "blue",
    "construction-west",
    "barracks",
    placementPoint);
Expect(placementRequest.SiteId == "site-west-barracks", "Placement request site id was not retained.");
Expect(placementRequest.BuilderFactionId == "blue", "Placement request builder was not retained.");
Expect(placementRequest.TargetSectorId == "construction-west", "Placement target sector was not retained.");
Expect(placementRequest.BuildingTypeId == "barracks", "Placement building type was not retained.");
Expect(placementRequest.Position.X == 37.3f && placementRequest.Position.Z == 42.8f, "Placement position was not retained.");
ExpectThrows<ArgumentException>(
    () => new BuildingPlacementRequest("", "blue", "construction-west", "barracks", placementPoint),
    "Blank placement site id was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingPlacementRequest("site", "", "construction-west", "barracks", placementPoint),
    "Blank placement builder id was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingPlacementRequest("site", "blue", "", "barracks", placementPoint),
    "Blank placement target sector id was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingPlacementRequest("site", "blue", "construction-west", "", placementPoint),
    "Blank placement building type id was accepted.");

var directSite = new ConstructionSite(
    "direct-site",
    "depot",
    "blue",
    "construction-west",
    new WorldPoint(17.3f, 42.8f),
    14f);
Expect(directSite.SiteId == "direct-site", "Construction site id was not retained.");
Expect(directSite.BuildingTypeId == "depot", "Construction site building type was not retained.");
Expect(directSite.OwnerId == "blue", "Construction site owner was not retained.");
Expect(directSite.SectorId == "construction-west", "Construction site sector was not retained.");
Expect(directSite.Position.X == 17.3f && directSite.Position.Z == 42.8f, "Construction site position was not retained.");
Expect(directSite.FootprintRadius == 14f, "Construction site footprint was not retained.");
ExpectThrows<ArgumentException>(
    () => new ConstructionSite("", "depot", "blue", "construction-west", placementPoint, 14f),
    "Blank construction site id was accepted.");
ExpectThrows<ArgumentException>(
    () => new ConstructionSite("site", "", "blue", "construction-west", placementPoint, 14f),
    "Blank construction site building type was accepted.");
ExpectThrows<ArgumentException>(
    () => new ConstructionSite("site", "depot", "", "construction-west", placementPoint, 14f),
    "Blank construction site owner was accepted.");
ExpectThrows<ArgumentException>(
    () => new ConstructionSite("site", "depot", "blue", "", placementPoint, 14f),
    "Blank construction site sector was accepted.");
ExpectThrows<ArgumentException>(
    () => new ConstructionSite("site", "depot", "blue", "construction-west", new WorldPoint(float.NaN, 0f), 14f),
    "Non-finite construction site position was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ConstructionSite("site", "depot", "blue", "construction-west", placementPoint, 0f),
    "Invalid construction site footprint was accepted.");

var constructionTopology = new SectorTopology();
foreach (var sectorId in new[]
{
    "construction-west",
    "construction-center",
    "construction-east",
    "construction-north",
    "construction-south"
})
{
    constructionTopology.RegisterSector(sectorId);
}

constructionTopology.AddBidirectionalAdjacency("construction-west", "construction-center");
constructionTopology.AddBidirectionalAdjacency("construction-center", "construction-east");
constructionTopology.AddBidirectionalAdjacency("construction-center", "construction-north");
constructionTopology.AddBidirectionalAdjacency("construction-center", "construction-south");

var constructionCenter = new SectorState("construction-center", "red");
var constructionTerritory = new TerritoryGraph(
    constructionTopology,
    new[]
    {
        new SectorState("construction-west", "blue"),
        constructionCenter,
        new SectorState("construction-east", "red"),
        new SectorState("construction-north", "red"),
        new SectorState("construction-south", "blue")
    },
    new[]
    {
        new SectorControlAnchor("construction-anchor-west", "construction-west"),
        new SectorControlAnchor("construction-anchor-center", "construction-center"),
        new SectorControlAnchor("construction-anchor-east", "construction-east"),
        new SectorControlAnchor("construction-anchor-north", "construction-north"),
        new SectorControlAnchor("construction-anchor-south", "construction-south")
    });
var constructionProfiles = new[]
{
    new SectorIncomeProfile("construction-west", 40),
    new SectorIncomeProfile("construction-center", 60),
    new SectorIncomeProfile("construction-east", 50),
    new SectorIncomeProfile("construction-north", 35),
    new SectorIncomeProfile("construction-south", 25)
};
var constructionAreas = new RectangularSectorPlacementAreaResolver()
    .Add("construction-west", 0f, 100f, 0f, 100f)
    .Add("construction-center", 100f, 200f, 0f, 100f)
    .Add("construction-east", 200f, 300f, 0f, 100f)
    .Add("construction-north", 100f, 200f, 100f, 200f)
    .Add("construction-south", 100f, 200f, -100f, 0f);
var constructionEconomy = new EconomyState("blue", 0);
var constructionSites = new List<ConstructionSite>();

var constructionIncome = SectorIncomeCollector.Collect(
    constructionTerritory,
    constructionProfiles,
    constructionEconomy);
Expect(constructionIncome.CollectedAmount == 65, "Construction fixture initial income was not 65.");
Expect(constructionEconomy.Balance == 65, "Construction fixture did not retain collected income.");

var invalidTypeResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("invalid-type", "blue", "construction-west", "depot", placementPoint),
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    invalidTypeResult,
    BuildingPlacementFailureReason.InvalidRequest,
    constructionEconomy,
    65,
    constructionSites,
    0,
    "Mismatched building definition");

var unknownSectorResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("unknown-sector", "blue", "construction-missing", "barracks", placementPoint),
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    unknownSectorResult,
    BuildingPlacementFailureReason.UnknownSector,
    constructionEconomy,
    65,
    constructionSites,
    0,
    "Unknown target sector");

var enemySectorResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("enemy-center", "blue", "construction-center", "barracks", new WorldPoint(150f, 50f)),
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    enemySectorResult,
    BuildingPlacementFailureReason.EnemyTerritory,
    constructionEconomy,
    65,
    constructionSites,
    0,
    "Enemy territory");

var outsideSelectedResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("outside-west", "blue", "construction-west", "barracks", new WorldPoint(150f, 50f)),
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    outsideSelectedResult,
    BuildingPlacementFailureReason.OutsideTargetSector,
    constructionEconomy,
    65,
    constructionSites,
    0,
    "Position in another sector");

var boundaryResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("cross-west", "blue", "construction-west", "barracks", new WorldPoint(95f, 50f)),
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    boundaryResult,
    BuildingPlacementFailureReason.FootprintCrossesSectorBoundary,
    constructionEconomy,
    65,
    constructionSites,
    0,
    "Footprint crossing sector boundary");

foreach (var invalidPoint in new[]
{
    new WorldPoint(float.NaN, 50f),
    new WorldPoint(50f, float.PositiveInfinity),
    new WorldPoint(float.NegativeInfinity, 50f)
})
{
    var invalidPositionResult = BuildingPlacementService.Place(
        new BuildingPlacementRequest("invalid-position-" + assertions, "blue", "construction-west", "barracks", invalidPoint),
        barracksDefinition,
        constructionEconomy,
        constructionTerritory,
        constructionAreas,
        constructionSites);
    ExpectPlacementFailure(
        invalidPositionResult,
        BuildingPlacementFailureReason.InvalidPosition,
        constructionEconomy,
        65,
        constructionSites,
        0,
        "Non-finite placement position");
}

var firstPlacement = BuildingPlacementService.Place(
    placementRequest,
    barracksDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
Expect(firstPlacement.Success, "Valid free placement failed.");
Expect(firstPlacement.FailureReason == BuildingPlacementFailureReason.None, "Successful placement reported a failure.");
Expect(firstPlacement.ConstructionSite != null, "Successful placement did not return a site.");
Expect(firstPlacement.ConstructionSite!.SectorId == "construction-west", "Placed site lost its target sector.");
Expect(firstPlacement.ConstructionSite.OwnerId == "blue", "Placed site lost its owner.");
Expect(firstPlacement.ConstructionSite.Position.X == 37.3f, "Placed site snapped away from its free X position.");
Expect(firstPlacement.ConstructionSite.Position.Z == 42.8f, "Placed site snapped away from its free Z position.");
Expect(firstPlacement.SpentAmount == 50, "Successful placement spent the wrong amount.");
Expect(firstPlacement.PreviousBalance == 65 && firstPlacement.NewBalance == 15, "Placement balance result was incorrect.");
Expect(constructionEconomy.Balance == 15, "Successful placement did not reduce economy balance to 15.");
Expect(constructionSites.Count == 1, "Successful placement did not add exactly one site.");

var occupiedResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("occupied-depot", "blue", "construction-west", "depot", new WorldPoint(50f, 45f)),
    depotDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    occupiedResult,
    BuildingPlacementFailureReason.Occupied,
    constructionEconomy,
    15,
    constructionSites,
    1,
    "Overlapping footprint");

var insufficientResult = BuildingPlacementService.Place(
    new BuildingPlacementRequest("insufficient-depot", "blue", "construction-west", "depot", new WorldPoint(75f, 75f)),
    depotDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
ExpectPlacementFailure(
    insufficientResult,
    BuildingPlacementFailureReason.InsufficientFunds,
    constructionEconomy,
    15,
    constructionSites,
    1,
    "Insufficient funds");

var secondWestCollection = SectorIncomeCollector.Collect(
    constructionTerritory,
    constructionProfiles,
    constructionEconomy);
Expect(secondWestCollection.CollectedAmount == 65, "Second pre-capture construction income was not 65.");
Expect(constructionEconomy.Balance == 80, "Second pre-capture collection balance was not 80.");

var secondFreePlacement = BuildingPlacementService.Place(
    new BuildingPlacementRequest("site-west-depot", "blue", "construction-west", "depot", new WorldPoint(75f, 75f)),
    depotDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
Expect(secondFreePlacement.Success, "Second free position in the same sector was rejected.");
Expect(secondFreePlacement.ConstructionSite!.SectorId == "construction-west", "Second free site lost West ownership scope.");
Expect(secondFreePlacement.ConstructionSite.Position.X == 75f, "Second free site did not retain its independent position.");
Expect(constructionSites.Count == 2, "Second free placement did not add a site.");
Expect(constructionEconomy.Balance == 45, "Second free placement did not spend 35.");

var beforeConstructionCaptureFrontlines = constructionTerritory.GetFrontlines();
Expect(
    beforeConstructionCaptureFrontlines.Contains(new FrontlineEdge("construction-west", "construction-center")),
    "Construction fixture initial West-Center frontline was missing.");
var constructionCapture = constructionTerritory.CompleteAnchorCapture("construction-anchor-center", "blue");
Expect(constructionCapture.Changed, "Construction fixture Center capture did not change ownership.");
Expect(constructionCenter.OwnerId == "blue", "Construction fixture Center did not become Blue.");
Expect(
    !constructionTerritory.GetFrontlines().Contains(new FrontlineEdge("construction-west", "construction-center")),
    "Construction fixture old West-Center frontline remained after capture.");
Expect(
    SectorIncomeResolver.Resolve(constructionTerritory, constructionProfiles, "blue") == 125,
    "Construction fixture Center capture did not increase income to 125.");

var postCaptureConstructionIncome = SectorIncomeCollector.Collect(
    constructionTerritory,
    constructionProfiles,
    constructionEconomy);
Expect(postCaptureConstructionIncome.CollectedAmount == 125, "Post-capture construction income was not 125.");
Expect(constructionEconomy.Balance == 170, "Post-capture construction balance was not 170.");

var centerPlacement = BuildingPlacementService.Place(
    new BuildingPlacementRequest("site-center-depot", "blue", "construction-center", "depot", new WorldPoint(150f, 50f)),
    depotDefinition,
    constructionEconomy,
    constructionTerritory,
    constructionAreas,
    constructionSites);
Expect(centerPlacement.Success, "Captured Center did not become a valid construction area.");
Expect(centerPlacement.ConstructionSite!.SectorId == "construction-center", "Center site did not retain Center SectorId.");
Expect(centerPlacement.ConstructionSite.OwnerId == "blue", "Center site did not retain Blue OwnerId.");
Expect(constructionSites.Count == 3, "Center placement did not add exactly one site.");
Expect(constructionEconomy.Balance == 135, "Center Depot did not spend 35 from the post-capture balance.");

var zeroCostEconomy = new EconomyState("blue", 0);
var zeroCostSites = new List<ConstructionSite>();
var zeroCostPlacement = BuildingPlacementService.Place(
    new BuildingPlacementRequest("zero-cost-site", "blue", "construction-west", "free-marker", new WorldPoint(10f, 10f)),
    new BuildingDefinition("free-marker", 0, 5f),
    zeroCostEconomy,
    constructionTerritory,
    constructionAreas,
    zeroCostSites);
Expect(zeroCostPlacement.Success, "Valid zero-cost placement failed.");
Expect(zeroCostEconomy.Balance == 0, "Zero-cost placement changed balance.");
Expect(zeroCostSites.Count == 1, "Zero-cost placement did not create a site.");

Console.WriteLine($"PASS ManagedPcChecks ({assertions} assertions)");

sealed class RectangularSectorPlacementAreaResolver : ISectorPlacementAreaResolver
{
    private readonly List<Area> areas = new();

    public RectangularSectorPlacementAreaResolver Add(
        string sectorId,
        float minimumX,
        float maximumX,
        float minimumZ,
        float maximumZ)
    {
        areas.Add(new Area(sectorId, minimumX, maximumX, minimumZ, maximumZ));
        return this;
    }

    public string? ResolveSector(WorldPoint point)
    {
        foreach (var area in areas)
        {
            if (point.X >= area.MinimumX
                && point.X <= area.MaximumX
                && point.Z >= area.MinimumZ
                && point.Z <= area.MaximumZ)
            {
                return area.SectorId;
            }
        }

        return null;
    }

    public bool ContainsFootprint(string sectorId, WorldPoint center, float radius)
    {
        foreach (var area in areas)
        {
            if (area.SectorId == sectorId)
            {
                return center.X - radius >= area.MinimumX
                    && center.X + radius <= area.MaximumX
                    && center.Z - radius >= area.MinimumZ
                    && center.Z + radius <= area.MaximumZ;
            }
        }

        return false;
    }

    private readonly record struct Area(
        string SectorId,
        float MinimumX,
        float MaximumX,
        float MinimumZ,
        float MaximumZ);
}
