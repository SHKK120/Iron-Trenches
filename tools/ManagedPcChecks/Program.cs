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

void ExpectCompletionFailure(
    ConstructionCompletionResult result,
    ConstructionCompletionFailureReason expectedReason,
    ICollection<ConstructionSite> sites,
    int expectedSiteCount,
    ICollection<BuildingState> buildings,
    int expectedBuildingCount,
    string context)
{
    Expect(!result.Success, $"{context}: completion unexpectedly succeeded.");
    Expect(result.FailureReason == expectedReason, $"{context}: failure reason was incorrect.");
    Expect(result.CompletedSiteId == null, $"{context}: failed completion returned a site id.");
    Expect(result.Building == null, $"{context}: failed completion returned a building.");
    Expect(sites.Count == expectedSiteCount, $"{context}: site collection changed.");
    Expect(buildings.Count == expectedBuildingCount, $"{context}: building collection changed.");
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

var directBuildingPosition = new WorldPoint(31.7f, 15.2f);
var directBuilding = new BuildingState(
    "building-direct",
    "barracks",
    "blue",
    "construction-west",
    directBuildingPosition,
    18f);
Expect(directBuilding.BuildingId == "building-direct", "Building id was not retained.");
Expect(directBuilding.BuildingTypeId == "barracks", "Building type was not retained.");
Expect(directBuilding.OwnerId == "blue", "Building owner was not retained.");
Expect(directBuilding.SectorId == "construction-west", "Building sector was not retained.");
Expect(
    directBuilding.Position.X == 31.7f && directBuilding.Position.Z == 15.2f,
    "Building position was not retained.");
Expect(directBuilding.FootprintRadius == 18f, "Building footprint was not retained.");
ExpectThrows<ArgumentException>(
    () => new BuildingState("", "barracks", "blue", "construction-west", directBuildingPosition, 18f),
    "Blank building id was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingState("building", "", "blue", "construction-west", directBuildingPosition, 18f),
    "Blank completed building type was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingState("building", "barracks", "", "construction-west", directBuildingPosition, 18f),
    "Blank completed building owner was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingState("building", "barracks", "blue", "", directBuildingPosition, 18f),
    "Blank completed building sector was accepted.");
ExpectThrows<ArgumentException>(
    () => new BuildingState(
        "building",
        "barracks",
        "blue",
        "construction-west",
        new WorldPoint(float.PositiveInfinity, 0f),
        18f),
    "Non-finite completed building position was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingState("building", "barracks", "blue", "construction-west", directBuildingPosition, 0f),
    "Zero completed building footprint was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new BuildingState(
        "building",
        "barracks",
        "blue",
        "construction-west",
        directBuildingPosition,
        float.NaN),
    "NaN completed building footprint was accepted.");

var directBuildingSectorBeforeTransfer = directBuilding.SectorId;
directBuilding.TransferOwnershipTo("red");
Expect(directBuilding.OwnerId == "red", "Building ownership did not transfer.");
Expect(directBuilding.SectorId == directBuildingSectorBeforeTransfer, "Building sector changed during ownership transfer.");
ExpectThrows<ArgumentException>(
    () => directBuilding.TransferOwnershipTo(" "),
    "Blank building transfer owner was accepted.");
Expect(directBuilding.OwnerId == "red", "Rejected building transfer changed owner.");

var completionSite = firstPlacement.ConstructionSite!;
var completionSites = new List<ConstructionSite> { completionSite };
var completionBuildings = new List<BuildingState>
{
    new BuildingState(
        "building-existing",
        "depot",
        "red",
        "construction-east",
        new WorldPoint(250f, 50f),
        14f)
};

var unknownCompletion = ConstructionCompletionService.Complete(
    "site-missing",
    "building-missing-site",
    completionSites,
    completionBuildings);
ExpectCompletionFailure(
    unknownCompletion,
    ConstructionCompletionFailureReason.UnknownSite,
    completionSites,
    1,
    completionBuildings,
    1,
    "Unknown construction site");

var invalidSiteCompletion = ConstructionCompletionService.Complete(
    " ",
    "building-invalid-site",
    completionSites,
    completionBuildings);
ExpectCompletionFailure(
    invalidSiteCompletion,
    ConstructionCompletionFailureReason.InvalidSiteId,
    completionSites,
    1,
    completionBuildings,
    1,
    "Invalid construction site id");

var invalidBuildingCompletion = ConstructionCompletionService.Complete(
    completionSite.SiteId,
    " ",
    completionSites,
    completionBuildings);
ExpectCompletionFailure(
    invalidBuildingCompletion,
    ConstructionCompletionFailureReason.InvalidBuildingId,
    completionSites,
    1,
    completionBuildings,
    1,
    "Invalid completed building id");

var duplicateBuildingCompletion = ConstructionCompletionService.Complete(
    completionSite.SiteId,
    "building-existing",
    completionSites,
    completionBuildings);
ExpectCompletionFailure(
    duplicateBuildingCompletion,
    ConstructionCompletionFailureReason.DuplicateBuildingId,
    completionSites,
    1,
    completionBuildings,
    1,
    "Duplicate completed building id");

var successfulCompletion = ConstructionCompletionService.Complete(
    completionSite.SiteId,
    "building-west-01",
    completionSites,
    completionBuildings);
Expect(successfulCompletion.Success, "Valid construction completion failed.");
Expect(
    successfulCompletion.FailureReason == ConstructionCompletionFailureReason.None,
    "Successful completion reported a failure.");
Expect(successfulCompletion.CompletedSiteId == completionSite.SiteId, "Completion lost the completed site id.");
Expect(successfulCompletion.Building != null, "Completion did not return a building.");
Expect(completionSites.Count == 0, "Successful completion did not remove the site.");
Expect(completionBuildings.Count == 2, "Successful completion did not add exactly one building.");
var completedWestBuilding = successfulCompletion.Building!;
Expect(completedWestBuilding.BuildingId == "building-west-01", "Completion did not use the requested building id.");
Expect(completedWestBuilding.BuildingTypeId == completionSite.BuildingTypeId, "Completion changed building type.");
Expect(completedWestBuilding.OwnerId == completionSite.OwnerId, "Completion changed owner.");
Expect(completedWestBuilding.SectorId == completionSite.SectorId, "Completion changed SectorId.");
Expect(
    completedWestBuilding.Position.X == completionSite.Position.X
        && completedWestBuilding.Position.Z == completionSite.Position.Z,
    "Completion changed position.");
Expect(
    completedWestBuilding.FootprintRadius == completionSite.FootprintRadius,
    "Completion changed footprint.");
Expect(
    completedWestBuilding.BuildingId != completionSite.SiteId,
    "Completion forced BuildingId to equal SiteId.");

var duplicateCollectionSite = new ConstructionSite(
    "duplicate-completion-site",
    "depot",
    "blue",
    "construction-west",
    new WorldPoint(68.4f, 55.9f),
    14f);
var duplicateCollectionSites = new List<ConstructionSite> { duplicateCollectionSite };
var invalidCompletedCollection = new List<BuildingState>
{
    new BuildingState("duplicate-building", "depot", "blue", "construction-west", new WorldPoint(20f, 20f), 5f),
    new BuildingState("duplicate-building", "depot", "blue", "construction-west", new WorldPoint(30f, 30f), 5f)
};
ExpectThrows<ArgumentException>(
    () => ConstructionCompletionService.Complete(
        duplicateCollectionSite.SiteId,
        "new-building",
        duplicateCollectionSites,
        invalidCompletedCollection),
    "Duplicate ids in completed building collection were accepted.");
Expect(duplicateCollectionSites.Count == 1, "Invalid building collection removed a construction site.");
Expect(invalidCompletedCollection.Count == 2, "Invalid building collection was mutated.");

var transferTopology = new SectorTopology();
foreach (var sectorId in new[]
{
    "transfer-west",
    "transfer-center",
    "transfer-east",
    "transfer-north",
    "transfer-south"
})
{
    transferTopology.RegisterSector(sectorId);
}

transferTopology.AddBidirectionalAdjacency("transfer-west", "transfer-center");
transferTopology.AddBidirectionalAdjacency("transfer-center", "transfer-east");
transferTopology.AddBidirectionalAdjacency("transfer-center", "transfer-north");
transferTopology.AddBidirectionalAdjacency("transfer-center", "transfer-south");

var transferCenterSector = new SectorState("transfer-center", "red");
var transferEastSector = new SectorState("transfer-east", "red");
var transferTerritory = new TerritoryGraph(
    transferTopology,
    new[]
    {
        new SectorState("transfer-west", "blue"),
        transferCenterSector,
        transferEastSector,
        new SectorState("transfer-north", "red"),
        new SectorState("transfer-south", "blue")
    },
    new[]
    {
        new SectorControlAnchor("transfer-anchor-west", "transfer-west"),
        new SectorControlAnchor("transfer-anchor-center", "transfer-center"),
        new SectorControlAnchor("transfer-anchor-east", "transfer-east"),
        new SectorControlAnchor("transfer-anchor-north", "transfer-north"),
        new SectorControlAnchor("transfer-anchor-south", "transfer-south")
    });
var transferProfiles = new[]
{
    new SectorIncomeProfile("transfer-west", 40),
    new SectorIncomeProfile("transfer-center", 60),
    new SectorIncomeProfile("transfer-east", 50),
    new SectorIncomeProfile("transfer-north", 35),
    new SectorIncomeProfile("transfer-south", 25)
};
var centerBarracks = new BuildingState(
    "transfer-center-barracks",
    "barracks",
    "red",
    "transfer-center",
    new WorldPoint(130f, 40f),
    18f);
var centerDepot = new BuildingState(
    "transfer-center-depot",
    "depot",
    "red",
    "transfer-center",
    new WorldPoint(170f, 70f),
    14f);
var centerAlreadyBlue = new BuildingState(
    "transfer-center-blue",
    "depot",
    "blue",
    "transfer-center",
    new WorldPoint(150f, 20f),
    10f);
var eastBarracks = new BuildingState(
    "transfer-east-barracks",
    "barracks",
    "red",
    "transfer-east",
    new WorldPoint(250f, 50f),
    18f);
var westBarracks = new BuildingState(
    "transfer-west-barracks",
    "barracks",
    "blue",
    "transfer-west",
    new WorldPoint(50f, 50f),
    18f);
var transferBuildings = new List<BuildingState>
{
    centerBarracks,
    centerDepot,
    centerAlreadyBlue,
    eastBarracks,
    westBarracks
};

Expect(
    SectorIncomeResolver.Resolve(transferTerritory, transferProfiles, "blue") == 65,
    "Transfer fixture initial Blue income was not 65.");
var transferFrontlinesBefore = transferTerritory.GetFrontlines();
Expect(
    transferFrontlinesBefore.Contains(new FrontlineEdge("transfer-west", "transfer-center")),
    "Transfer fixture initial West-Center frontline was missing.");

var duplicateTransferInputs = new[]
{
    new BuildingState("transfer-duplicate", "depot", "red", "transfer-east", new WorldPoint(220f, 30f), 5f),
    new BuildingState("transfer-duplicate", "depot", "red", "transfer-east", new WorldPoint(230f, 40f), 5f)
};
ExpectThrows<ArgumentException>(
    () => TerritoryBuildingOwnershipService.CaptureAndTransfer(
        transferTerritory,
        "transfer-anchor-east",
        "blue",
        duplicateTransferInputs),
    "Duplicate transfer building ids were accepted.");
Expect(transferEastSector.OwnerId == "red", "Invalid transfer collection partially captured East.");

var centerTransfer = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    transferTerritory,
    "transfer-anchor-center",
    "blue",
    transferBuildings);
Expect(centerTransfer.CaptureChanged, "Center building transfer did not report a capture change.");
Expect(centerTransfer.SectorId == "transfer-center", "Transfer result lost the captured SectorId.");
Expect(centerTransfer.PreviousOwnerId == "red", "Transfer result lost previous sector owner.");
Expect(centerTransfer.NewOwnerId == "blue", "Transfer result lost new sector owner.");
Expect(centerTransfer.TransferredBuildingCount == 2, "Transfer count did not include exactly changed buildings.");
Expect(
    centerTransfer.TransferredBuildingIds.Contains("transfer-center-barracks"),
    "Center Barracks was not listed as transferred.");
Expect(
    centerTransfer.TransferredBuildingIds.Contains("transfer-center-depot"),
    "Center Depot was not listed as transferred.");
Expect(
    !centerTransfer.TransferredBuildingIds.Contains("transfer-center-blue"),
    "Already-Blue Center building was incorrectly counted as transferred.");
Expect(transferCenterSector.OwnerId == "blue", "Center Sector owner did not become Blue.");
Expect(centerBarracks.OwnerId == "blue", "Center Barracks owner did not become Blue.");
Expect(centerDepot.OwnerId == "blue", "Center Depot owner did not become Blue.");
Expect(centerAlreadyBlue.OwnerId == "blue", "Already-Blue Center building changed incorrectly.");
Expect(eastBarracks.OwnerId == "red", "East building changed during Center capture.");
Expect(westBarracks.OwnerId == "blue", "West building changed during Center capture.");
Expect(centerBarracks.SectorId == "transfer-center", "Center Barracks SectorId changed.");
Expect(centerDepot.SectorId == "transfer-center", "Center Depot SectorId changed.");
Expect(eastBarracks.SectorId == "transfer-east", "East Barracks SectorId changed.");
var transferFrontlinesAfter = transferTerritory.GetFrontlines();
Expect(
    !transferFrontlinesAfter.Contains(new FrontlineEdge("transfer-west", "transfer-center")),
    "Old West-Center frontline remained after transfer capture.");
Expect(
    transferFrontlinesAfter.Contains(new FrontlineEdge("transfer-center", "transfer-east")),
    "New Center-East frontline was missing after transfer capture.");
Expect(
    transferFrontlinesAfter.Contains(new FrontlineEdge("transfer-center", "transfer-north")),
    "New Center-North frontline was missing after transfer capture.");
Expect(
    SectorIncomeResolver.Resolve(transferTerritory, transferProfiles, "blue") == 125,
    "Center transfer capture did not increase Blue income to 125.");

var lateMismatchedCenterBuilding = new BuildingState(
    "transfer-center-late-red",
    "depot",
    "red",
    "transfer-center",
    new WorldPoint(145f, 85f),
    8f);
transferBuildings.Add(lateMismatchedCenterBuilding);
var sameOwnerTransfer = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    transferTerritory,
    "transfer-anchor-center",
    "blue",
    transferBuildings);
Expect(!sameOwnerTransfer.CaptureChanged, "Same-owner capture reported a building transfer capture change.");
Expect(sameOwnerTransfer.TransferredBuildingCount == 0, "Same-owner capture transferred buildings.");
Expect(
    lateMismatchedCenterBuilding.OwnerId == "red",
    "Same-owner capture repaired mismatched building ownership without a capture change.");
Expect(
    lateMismatchedCenterBuilding.SectorId == "transfer-center",
    "Same-owner capture changed mismatched building SectorId.");

var supplySourceDefinition = new SupplySourceDefinition(
    "blue-rear",
    "blue",
    "supply-west");
Expect(supplySourceDefinition.SourceId == "blue-rear", "Supply source id was not retained.");
Expect(supplySourceDefinition.FactionId == "blue", "Supply source faction was not retained.");
Expect(supplySourceDefinition.SectorId == "supply-west", "Supply source sector was not retained.");
ExpectThrows<ArgumentException>(
    () => new SupplySourceDefinition(" ", "blue", "supply-west"),
    "Blank supply source id was accepted.");
ExpectThrows<ArgumentException>(
    () => new SupplySourceDefinition("source", "", "supply-west"),
    "Blank supply source faction was accepted.");
ExpectThrows<ArgumentException>(
    () => new SupplySourceDefinition("source", "blue", ""),
    "Blank supply source sector was accepted.");

var supplyTopology = new SectorTopology();
foreach (var sectorId in new[]
{
    "supply-west",
    "supply-center",
    "supply-east",
    "supply-north",
    "supply-south"
})
{
    supplyTopology.RegisterSector(sectorId);
}

supplyTopology.AddBidirectionalAdjacency("supply-west", "supply-center");
supplyTopology.AddBidirectionalAdjacency("supply-center", "supply-east");
supplyTopology.AddBidirectionalAdjacency("supply-west", "supply-south");
supplyTopology.AddBidirectionalAdjacency("supply-south", "supply-east");
supplyTopology.AddBidirectionalAdjacency("supply-north", "supply-center");
supplyTopology.AddBidirectionalAdjacency("supply-north", "supply-east");
supplyTopology.AddBidirectionalAdjacency("supply-north", "supply-south");

var supplyWest = new SectorState("supply-west", "blue");
var supplyCenter = new SectorState("supply-center", "blue");
var supplyEast = new SectorState("supply-east", "blue");
var supplyNorth = new SectorState("supply-north", "red");
var supplySouth = new SectorState("supply-south", "blue");
var supplyTerritory = new TerritoryGraph(
    supplyTopology,
    new[] { supplyWest, supplyCenter, supplyEast, supplyNorth, supplySouth },
    new[]
    {
        new SectorControlAnchor("supply-anchor-west", "supply-west"),
        new SectorControlAnchor("supply-anchor-center", "supply-center"),
        new SectorControlAnchor("supply-anchor-east", "supply-east"),
        new SectorControlAnchor("supply-anchor-north", "supply-north"),
        new SectorControlAnchor("supply-anchor-south", "supply-south")
    });
var redNorthSource = new SupplySourceDefinition("red-north", "red", "supply-north");
var supplySources = new[] { supplySourceDefinition, redNorthSource };

ExpectThrows<ArgumentNullException>(
    () => StrategicSupplyResolver.Resolve(null!, "blue", supplySources),
    "Null supply territory was accepted.");
ExpectThrows<ArgumentException>(
    () => StrategicSupplyResolver.Resolve(supplyTerritory, " ", supplySources),
    "Blank supply faction was accepted.");
ExpectThrows<ArgumentNullException>(
    () => StrategicSupplyResolver.Resolve(supplyTerritory, "blue", null!),
    "Null supply source collection was accepted.");
ExpectThrows<ArgumentException>(
    () => StrategicSupplyResolver.Resolve(
        supplyTerritory,
        "blue",
        new SupplySourceDefinition[] { supplySourceDefinition, null! }),
    "Null supply source item was accepted.");
ExpectThrows<ArgumentException>(
    () => StrategicSupplyResolver.Resolve(
        supplyTerritory,
        "blue",
        new[]
        {
            supplySourceDefinition,
            new SupplySourceDefinition("blue-rear", "red", "supply-north")
        }),
    "Duplicate supply source id was accepted.");
ExpectThrows<ArgumentException>(
    () => StrategicSupplyResolver.Resolve(
        supplyTerritory,
        "blue",
        new[] { new SupplySourceDefinition("outside-source", "blue", "supply-missing") }),
    "Supply source in an unknown sector was accepted.");

var initialSupplySnapshot = StrategicSupplyResolver.Resolve(
    supplyTerritory,
    "blue",
    supplySources);
Expect(initialSupplySnapshot.FactionId == "blue", "Supply snapshot lost its faction id.");
Expect(initialSupplySnapshot.ActiveSupplySourceIds.Count == 1, "Initial active source count was not one.");
Expect(initialSupplySnapshot.ActiveSupplySourceIds.Contains("blue-rear"), "Blue rear source was not active.");
Expect(!initialSupplySnapshot.ActiveSupplySourceIds.Contains("red-north"), "Red source powered Blue supply.");
Expect(initialSupplySnapshot.SuppliedSectorIds.Count == 4, "Initial supplied sector count was not four.");
Expect(initialSupplySnapshot.CutOffSectorIds.Count == 0, "Initial snapshot contained cut-off sectors.");
Expect(initialSupplySnapshot.GetStatus("supply-west") == SectorSupplyStatus.Supplied, "Source sector was not supplied.");
Expect(initialSupplySnapshot.GetStatus("supply-center") == SectorSupplyStatus.Supplied, "Center was not initially supplied.");
Expect(initialSupplySnapshot.GetStatus("supply-south") == SectorSupplyStatus.Supplied, "South was not initially supplied.");
Expect(initialSupplySnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied, "East was not initially supplied.");
Expect(initialSupplySnapshot.GetStatus("supply-north") == SectorSupplyStatus.NotOwned, "Enemy North was reported cut off.");
ExpectThrows<KeyNotFoundException>(
    () => initialSupplySnapshot.GetStatus("supply-missing"),
    "Unknown snapshot sector was accepted.");
ExpectThrows<ArgumentException>(
    () => initialSupplySnapshot.GetStatus(" "),
    "Blank snapshot sector was accepted.");

var supplyProfiles = new[]
{
    new SectorIncomeProfile("supply-west", 40),
    new SectorIncomeProfile("supply-center", 60),
    new SectorIncomeProfile("supply-east", 50),
    new SectorIncomeProfile("supply-north", 35),
    new SectorIncomeProfile("supply-south", 25)
};
Expect(
    SectorIncomeResolver.Resolve(supplyTerritory, supplyProfiles, "blue") == 175,
    "Initial supply fixture income was not 175.");
var initialSupplyFrontlines = supplyTerritory.GetFrontlines();
Expect(initialSupplyFrontlines.Count == 3, "Initial supply fixture frontline count was not three.");
Expect(
    initialSupplyFrontlines.Contains(new FrontlineEdge("supply-north", "supply-center")),
    "Initial North-Center frontline was missing.");
Expect(
    initialSupplyFrontlines.Contains(new FrontlineEdge("supply-north", "supply-east")),
    "Initial North-East frontline was missing.");
Expect(
    initialSupplyFrontlines.Contains(new FrontlineEdge("supply-north", "supply-south")),
    "Initial North-South frontline was missing.");

var frontDepot = new BuildingState(
    "supply-front-depot",
    "depot",
    "blue",
    "supply-east",
    new WorldPoint(250f, 50f),
    14f);
var centerSupplyDepot = new BuildingState(
    "supply-center-depot",
    "depot",
    "blue",
    "supply-center",
    new WorldPoint(150f, 50f),
    14f);
var southSupplyBarracks = new BuildingState(
    "supply-south-barracks",
    "barracks",
    "blue",
    "supply-south",
    new WorldPoint(150f, -50f),
    18f);
var supplyBuildings = new List<BuildingState>
{
    frontDepot,
    centerSupplyDepot,
    southSupplyBarracks
};
Expect(
    initialSupplySnapshot.GetStatus(frontDepot.SectorId) == SectorSupplyStatus.Supplied,
    "Front Depot did not derive initial supply from East SectorId.");

var redCenterCapture = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    supplyTerritory,
    "supply-anchor-center",
    "red",
    supplyBuildings);
Expect(redCenterCapture.CaptureChanged, "Red Center capture did not change territory ownership.");
Expect(centerSupplyDepot.OwnerId == "red", "Center building transfer regressed during supply test.");
Expect(frontDepot.OwnerId == "blue", "Center capture changed the East building owner.");
Expect(
    initialSupplySnapshot.GetStatus("supply-center") == SectorSupplyStatus.Supplied,
    "Previously resolved snapshot was mutated after Center capture.");

var alternateRouteSnapshot = StrategicSupplyResolver.Resolve(
    supplyTerritory,
    "blue",
    supplySources);
Expect(alternateRouteSnapshot.GetStatus("supply-center") == SectorSupplyStatus.NotOwned, "Captured Center was not NotOwned.");
Expect(alternateRouteSnapshot.GetStatus("supply-south") == SectorSupplyStatus.Supplied, "South alternate route was not supplied.");
Expect(alternateRouteSnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied, "East lost supply while alternate route remained.");
Expect(
    alternateRouteSnapshot.GetStatus(frontDepot.SectorId) == SectorSupplyStatus.Supplied,
    "Front Depot lost supply while South route remained.");
Expect(
    supplyTerritory.GetFrontlines().Contains(new FrontlineEdge("supply-west", "supply-center")),
    "Center capture did not update the ownership-derived frontline.");
Expect(
    SectorIncomeResolver.Resolve(supplyTerritory, supplyProfiles, "blue") == 115,
    "Center capture did not preserve ownership-based economy behavior.");

var redSouthCapture = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    supplyTerritory,
    "supply-anchor-south",
    "red",
    supplyBuildings);
Expect(redSouthCapture.CaptureChanged, "Red South capture did not change territory ownership.");
Expect(southSupplyBarracks.OwnerId == "red", "South building did not transfer to Red.");
var cutOffSnapshot = StrategicSupplyResolver.Resolve(supplyTerritory, "blue", supplySources);
Expect(cutOffSnapshot.GetStatus("supply-west") == SectorSupplyStatus.Supplied, "Rear source sector lost supply.");
Expect(cutOffSnapshot.GetStatus("supply-south") == SectorSupplyStatus.NotOwned, "Captured South was not NotOwned.");
Expect(cutOffSnapshot.GetStatus("supply-east") == SectorSupplyStatus.CutOff, "East was not cut off after both routes fell.");
Expect(cutOffSnapshot.CutOffSectorIds.Count == 1, "Full route loss did not produce exactly one cut-off sector.");
Expect(supplyEast.OwnerId == "blue", "Cut-off status changed East territory ownership.");
Expect(frontDepot.OwnerId == "blue", "Cut-off status changed Front Depot ownership.");
Expect(frontDepot.SectorId == "supply-east", "Cut-off status changed Front Depot SectorId.");
Expect(
    cutOffSnapshot.GetStatus(frontDepot.SectorId) == SectorSupplyStatus.CutOff,
    "Front Depot did not derive cut-off status from East SectorId.");
Expect(
    alternateRouteSnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied,
    "Earlier alternate-route snapshot was mutated by later capture.");
Expect(
    SectorIncomeResolver.Resolve(supplyTerritory, supplyProfiles, "blue") == 90,
    "Cut-off East was incorrectly removed from ownership-based income.");

var cutOffPlacementEconomy = new EconomyState("blue", 0);
var cutOffPlacementSites = new List<ConstructionSite>();
var cutOffPlacementAreas = new RectangularSectorPlacementAreaResolver()
    .Add("supply-west", 0f, 100f, 0f, 100f)
    .Add("supply-center", 100f, 200f, 0f, 100f)
    .Add("supply-east", 200f, 300f, 0f, 100f)
    .Add("supply-north", 100f, 200f, 100f, 200f)
    .Add("supply-south", 100f, 200f, -100f, 0f);
var cutOffPlacement = BuildingPlacementService.Place(
    new BuildingPlacementRequest(
        "supply-cutoff-site",
        "blue",
        "supply-east",
        "supply-test-marker",
        new WorldPoint(225f, 25f)),
    new BuildingDefinition("supply-test-marker", 0, 5f),
    cutOffPlacementEconomy,
    supplyTerritory,
    cutOffPlacementAreas,
    cutOffPlacementSites);
Expect(cutOffPlacement.Success, "Supply status was incorrectly added as a construction requirement.");
Expect(cutOffPlacementSites.Count == 1, "Valid cut-off-sector placement did not create a site.");

var blueSouthRecapture = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    supplyTerritory,
    "supply-anchor-south",
    "blue",
    supplyBuildings);
Expect(blueSouthRecapture.CaptureChanged, "Blue South recapture did not change ownership.");
Expect(southSupplyBarracks.OwnerId == "blue", "South building did not return to Blue after recapture.");
var restoredSupplySnapshot = StrategicSupplyResolver.Resolve(
    supplyTerritory,
    "blue",
    supplySources);
Expect(restoredSupplySnapshot.GetStatus("supply-south") == SectorSupplyStatus.Supplied, "Recaptured South was not supplied.");
Expect(restoredSupplySnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied, "East supply did not recover through South.");
Expect(
    restoredSupplySnapshot.GetStatus(frontDepot.SectorId) == SectorSupplyStatus.Supplied,
    "Front Depot supply did not recover through its SectorId.");
Expect(
    cutOffSnapshot.GetStatus("supply-east") == SectorSupplyStatus.CutOff,
    "Cut-off snapshot was mutated during supply restoration.");

var redWestCapture = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    supplyTerritory,
    "supply-anchor-west",
    "red",
    supplyBuildings);
Expect(redWestCapture.CaptureChanged, "Source Sector capture did not change ownership.");
var sourceLostSnapshot = StrategicSupplyResolver.Resolve(supplyTerritory, "blue", supplySources);
Expect(sourceLostSnapshot.ActiveSupplySourceIds.Count == 0, "Lost source Sector remained active.");
Expect(sourceLostSnapshot.GetStatus("supply-west") == SectorSupplyStatus.NotOwned, "Lost source Sector was not NotOwned.");
Expect(sourceLostSnapshot.GetStatus("supply-south") == SectorSupplyStatus.CutOff, "South was not cut off after source loss.");
Expect(sourceLostSnapshot.GetStatus("supply-east") == SectorSupplyStatus.CutOff, "East was not cut off after source loss.");
Expect(sourceLostSnapshot.CutOffSectorIds.Count == 2, "Source loss did not cut off all remaining Blue sectors.");
Expect(supplySourceDefinition.FactionId == "blue", "Source loss changed SupplySourceDefinition faction.");
Expect(supplySourceDefinition.SectorId == "supply-west", "Source loss changed SupplySourceDefinition sector.");

var backupSource = new SupplySourceDefinition("blue-front-backup", "blue", "supply-east");
var multipleSourceSnapshot = StrategicSupplyResolver.Resolve(
    supplyTerritory,
    "blue",
    new[] { supplySourceDefinition, backupSource, redNorthSource });
Expect(multipleSourceSnapshot.ActiveSupplySourceIds.Count == 1, "Multiple-source active count was incorrect.");
Expect(multipleSourceSnapshot.ActiveSupplySourceIds.Contains("blue-front-backup"), "Active backup source was not used.");
Expect(!multipleSourceSnapshot.ActiveSupplySourceIds.Contains("blue-rear"), "Inactive rear source was reported active.");
Expect(multipleSourceSnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied, "Backup source did not supply East.");
Expect(multipleSourceSnapshot.GetStatus("supply-south") == SectorSupplyStatus.Supplied, "Backup source did not reach South.");

var noOwnedSectorSnapshot = StrategicSupplyResolver.Resolve(supplyTerritory, "green", supplySources);
Expect(noOwnedSectorSnapshot.SuppliedSectorIds.Count == 0, "Faction with no territory had supplied sectors.");
Expect(noOwnedSectorSnapshot.CutOffSectorIds.Count == 0, "Faction with no territory had cut-off sectors.");
Expect(noOwnedSectorSnapshot.GetStatus("supply-east") == SectorSupplyStatus.NotOwned, "Foreign sector was not NotOwned.");

var blueWestRecapture = supplyTerritory.CompleteAnchorCapture("supply-anchor-west", "blue");
Expect(blueWestRecapture.Changed, "Blue did not recapture its rear source Sector.");
var reactivatedSourceSnapshot = StrategicSupplyResolver.Resolve(
    supplyTerritory,
    "blue",
    supplySources);
Expect(reactivatedSourceSnapshot.ActiveSupplySourceIds.Contains("blue-rear"), "Recaptured rear source did not reactivate.");
Expect(reactivatedSourceSnapshot.GetStatus("supply-west") == SectorSupplyStatus.Supplied, "Reactivated source Sector was not supplied.");
Expect(reactivatedSourceSnapshot.GetStatus("supply-south") == SectorSupplyStatus.Supplied, "Reactivated source did not reach South.");
Expect(reactivatedSourceSnapshot.GetStatus("supply-east") == SectorSupplyStatus.Supplied, "Reactivated source did not reach East.");

// RTS-CORE-03B: Terrain movement profiles remain data-driven trial inputs.
var openMovement = new TerrainMovementProfile("Open", 1f);
var forestMovement = new TerrainMovementProfile("Forest", 0.7f);
var rockyMovement = new TerrainMovementProfile("Rocky", 0.8f);
var mudMovement = new TerrainMovementProfile("Mud", 0.6f);
Expect(openMovement.TerrainId == "Open", "Terrain id was not retained.");
Expect(openMovement.MovementMultiplier == 1f, "Open movement multiplier was not retained.");
Expect(forestMovement.MovementMultiplier == 0.7f, "Forest trial multiplier was not retained.");
Expect(rockyMovement.MovementMultiplier == 0.8f, "Rocky trial multiplier was not retained.");
Expect(mudMovement.MovementMultiplier == 0.6f, "Mud trial multiplier was not retained.");
ExpectThrows<ArgumentException>(
    () => new TerrainMovementProfile(" ", 1f),
    "Blank terrain id was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new TerrainMovementProfile("Zero", 0f),
    "Zero terrain multiplier was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new TerrainMovementProfile("Negative", -1f),
    "Negative terrain multiplier was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new TerrainMovementProfile("NaN", float.NaN),
    "NaN terrain multiplier was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new TerrainMovementProfile("Infinity", float.PositiveInfinity),
    "Infinite terrain multiplier was accepted.");

var terrainMovementResolver = new RectangularTerrainMovementResolver(openMovement)
    .Add(forestMovement, 0f, 100f, -20f, 20f)
    .Add(rockyMovement, 0f, 20f, 30f, 50f)
    .Add(mudMovement, 30f, 50f, 30f, 50f);
Expect(terrainMovementResolver.Resolve(new WorldPoint(25f, 10f)).TerrainId == "Forest", "Forest terrain did not resolve.");
Expect(terrainMovementResolver.Resolve(new WorldPoint(10f, 40f)).TerrainId == "Rocky", "Rocky terrain did not resolve.");
Expect(terrainMovementResolver.Resolve(new WorldPoint(40f, 40f)).TerrainId == "Mud", "Mud terrain did not resolve.");
Expect(terrainMovementResolver.Resolve(new WorldPoint(200f, 0f)).TerrainId == "Open", "Open fallback terrain did not resolve.");

var arbitraryRoadStart = new WorldPoint(14.2f, 31.7f);
var arbitraryRoadEnd = new WorldPoint(63.4f, 48.1f);
var arbitraryRoad = new RoadSegment(
    "road-arbitrary",
    arbitraryRoadStart,
    arbitraryRoadEnd,
    4.5f,
    new[] { "road-west", "road-center" });
Expect(arbitraryRoad.RoadSegmentId == "road-arbitrary", "Road id was not retained.");
Expect(arbitraryRoad.Start.X == 14.2f && arbitraryRoad.Start.Z == 31.7f, "Free road start was not retained.");
Expect(arbitraryRoad.End.X == 63.4f && arbitraryRoad.End.Z == 48.1f, "Free road end was not retained.");
Expect(arbitraryRoad.Width == 4.5f, "Road width was not retained.");
Expect(arbitraryRoad.TraversedSectorIds.Count == 2, "Multi-sector ids were not retained.");
Expect(arbitraryRoad.Length > 50f, "Road length was not derived from free endpoints.");
ExpectThrows<ArgumentException>(
    () => new RoadSegment(" ", arbitraryRoadStart, arbitraryRoadEnd, 4f, new[] { "road-west" }),
    "Blank road id was accepted.");
ExpectThrows<ArgumentException>(
    () => new RoadSegment("road-same", arbitraryRoadStart, arbitraryRoadStart, 4f, new[] { "road-west" }),
    "Identical road endpoints were accepted.");
ExpectThrows<ArgumentException>(
    () => new RoadSegment("road-invalid-point", new WorldPoint(float.NaN, 0f), arbitraryRoadEnd, 4f, new[] { "road-west" }),
    "Invalid road point was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoadSegment("road-zero-width", arbitraryRoadStart, arbitraryRoadEnd, 0f, new[] { "road-west" }),
    "Zero road width was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoadSegment("road-negative-width", arbitraryRoadStart, arbitraryRoadEnd, -1f, new[] { "road-west" }),
    "Negative road width was accepted.");
ExpectThrows<ArgumentException>(
    () => new RoadSegment("road-duplicate-sector", arbitraryRoadStart, arbitraryRoadEnd, 4f, new[] { "road-west", "road-west" }),
    "Duplicate traversed sector id was accepted.");
ExpectThrows<ArgumentException>(
    () => new RoadSegment("road-empty-sector", arbitraryRoadStart, arbitraryRoadEnd, 4f, Array.Empty<string>()),
    "Road without traversed sectors was accepted.");

var roadTopology = new SectorTopology();
roadTopology.RegisterSector("road-west");
roadTopology.RegisterSector("road-center");
roadTopology.RegisterSector("road-east");
roadTopology.AddBidirectionalAdjacency("road-west", "road-center");
roadTopology.AddBidirectionalAdjacency("road-center", "road-east");
var roadWest = new SectorState("road-west", "blue");
var roadCenter = new SectorState("road-center", "blue");
var roadEast = new SectorState("road-east", "red");
var roadTerritory = new TerritoryGraph(
    roadTopology,
    new[] { roadWest, roadCenter, roadEast },
    new[]
    {
        new SectorControlAnchor("road-anchor-west", "road-west"),
        new SectorControlAnchor("road-anchor-center", "road-center"),
        new SectorControlAnchor("road-anchor-east", "road-east")
    });
var roadPlacementAreas = new ConfigurableRoadPlacementAreaResolver();
var placedRoads = new RoadNetwork();
roadPlacementAreas.Set("road-west");
var singleSectorPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-free-one", "blue", new WorldPoint(14.2f, 31.7f), new WorldPoint(63.4f, 48.1f), 4f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(singleSectorPlacement.Success, "Owned-sector free road placement failed.");
Expect(singleSectorPlacement.RoadSegment != null, "Successful road placement returned no segment.");
Expect(placedRoads.Segments.Count == 1, "Successful road placement did not mutate the network once.");

roadPlacementAreas.Set("road-west", "road-center");
var multiSectorPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-free-multi", "blue", new WorldPoint(63.4f, 48.1f), new WorldPoint(92.6f, 17.3f), 5f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(multiSectorPlacement.Success, "Owned multi-sector road placement failed.");
Expect(multiSectorPlacement.RoadSegment!.TraversedSectorIds.Count == 2, "Placed road lost traversed sectors.");
Expect(placedRoads.GetConnectedSegments("road-free-one").Count == 1, "Dynamic endpoint junction did not connect roads.");

var placementCountBeforeFailure = placedRoads.Segments.Count;
roadPlacementAreas.Set("road-center", "road-east");
var enemyRoadPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-enemy", "blue", new WorldPoint(92.6f, 17.3f), new WorldPoint(120f, 15f), 4f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(!enemyRoadPlacement.Success, "Enemy-sector road placement succeeded.");
Expect(enemyRoadPlacement.FailureReason == RoadPlacementFailureReason.EnemyTerritory, "Enemy road failure reason was incorrect.");
Expect(placedRoads.Segments.Count == placementCountBeforeFailure, "Failed enemy road placement mutated the network.");

roadPlacementAreas.Set("road-unknown");
var unknownRoadPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-unknown", "blue", new WorldPoint(1f, 1f), new WorldPoint(3f, 3f), 2f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(!unknownRoadPlacement.Success, "Unknown-sector road placement succeeded.");
Expect(unknownRoadPlacement.FailureReason == RoadPlacementFailureReason.UnknownSector, "Unknown road failure reason was incorrect.");
Expect(placedRoads.Segments.Count == placementCountBeforeFailure, "Unknown-sector road placement mutated the network.");

roadPlacementAreas.Set("road-west");
var duplicateRoadPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-free-one", "blue", new WorldPoint(1f, 2f), new WorldPoint(3f, 4f), 2f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(!duplicateRoadPlacement.Success, "Duplicate road id placement succeeded.");
Expect(duplicateRoadPlacement.FailureReason == RoadPlacementFailureReason.DuplicateRoadSegmentId, "Duplicate road failure reason was incorrect.");
Expect(placedRoads.Segments.Count == placementCountBeforeFailure, "Duplicate road placement mutated the network.");

roadPlacementAreas.Set();
var noAreaRoadPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-no-area", "blue", new WorldPoint(2f, 2f), new WorldPoint(8f, 8f), 2f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(!noAreaRoadPlacement.Success, "Road without traversed sectors succeeded.");
Expect(noAreaRoadPlacement.FailureReason == RoadPlacementFailureReason.NoTraversedSectors, "No-area road failure reason was incorrect.");
Expect(placedRoads.Segments.Count == placementCountBeforeFailure, "No-area road placement mutated the network.");

roadPlacementAreas.Set("road-west");
var invalidRoadPlacement = RoadPlacementService.Place(
    new RoadPlacementRequest("road-invalid", "blue", new WorldPoint(5f, 5f), new WorldPoint(5f, 5f), 2f),
    roadTerritory,
    roadPlacementAreas,
    placedRoads);
Expect(!invalidRoadPlacement.Success, "Zero-length road placement succeeded.");
Expect(invalidRoadPlacement.FailureReason == RoadPlacementFailureReason.InvalidGeometry, "Invalid road geometry reason was incorrect.");
Expect(placedRoads.Segments.Count == placementCountBeforeFailure, "Invalid road placement mutated the network.");

var routeRear = new WorldPoint(0f, 0f);
var routeJunction = new WorldPoint(50f, 0f);
var routeFront = new WorldPoint(100f, 0f);
var routeNorth = new WorldPoint(0f, 50f);
var routeNorthEast = new WorldPoint(100f, 50f);
var reinforcementRoads = new RoadNetwork();
reinforcementRoads.Register(new RoadSegment("route-main-a", routeRear, routeJunction, 4f, new[] { "transit-rear", "transit-middle" }));
reinforcementRoads.Register(new RoadSegment("route-main-b", routeJunction, routeFront, 4f, new[] { "transit-middle", "transit-front" }));
reinforcementRoads.Register(new RoadSegment("route-alt-a", routeRear, routeNorth, 4f, new[] { "transit-rear", "transit-middle" }));
reinforcementRoads.Register(new RoadSegment("route-alt-b", routeNorth, routeNorthEast, 4f, new[] { "transit-middle" }));
reinforcementRoads.Register(new RoadSegment("route-alt-c", routeNorthEast, routeFront, 4f, new[] { "transit-middle", "transit-front" }));
Expect(reinforcementRoads.TryFindPath(routeRear, routeFront, out var shortestRoadPath), "Connected road path was not found.");
Expect(shortestRoadPath.Count == 2, "Road network did not choose the shorter connected path.");
Expect(shortestRoadPath[0].RoadSegmentId == "route-main-a", "Road path did not start on the main route.");
Expect(reinforcementRoads.IsPointOnRoad(new WorldPoint(25f, 0f)), "Road center was not detected.");
Expect(!reinforcementRoads.IsPointOnRoad(new WorldPoint(25f, 10f)), "Point outside road width was detected as road.");

var alternateOnlyRoads = new RoadNetwork();
alternateOnlyRoads.Register(new RoadSegment("route-alt-a", routeRear, routeNorth, 4f, new[] { "transit-rear", "transit-middle" }));
alternateOnlyRoads.Register(new RoadSegment("route-alt-b", routeNorth, routeNorthEast, 4f, new[] { "transit-middle" }));
alternateOnlyRoads.Register(new RoadSegment("route-alt-c", routeNorthEast, routeFront, 4f, new[] { "transit-middle", "transit-front" }));
Expect(alternateOnlyRoads.TryFindPath(routeRear, routeFront, out var alternateRoadPath), "Alternate road route was not found.");
Expect(alternateRoadPath.Count == 3, "Alternate route segment count was incorrect.");

var trialRoadMovement = new RoadMovementProfile(1.4f);
var openOffroadSpeed = MovementSpeedResolver.Resolve(10f, new WorldPoint(200f, 0f), terrainMovementResolver, reinforcementRoads, trialRoadMovement);
var forestOffroadSpeed = MovementSpeedResolver.Resolve(10f, new WorldPoint(25f, 10f), terrainMovementResolver, reinforcementRoads, trialRoadMovement);
var rockyOffroadSpeed = MovementSpeedResolver.Resolve(10f, new WorldPoint(10f, 40f), terrainMovementResolver, reinforcementRoads, trialRoadMovement);
var mudOffroadSpeed = MovementSpeedResolver.Resolve(10f, new WorldPoint(40f, 40f), terrainMovementResolver, reinforcementRoads, trialRoadMovement);
var roadOverForestSpeed = MovementSpeedResolver.Resolve(10f, new WorldPoint(25f, 0f), terrainMovementResolver, reinforcementRoads, trialRoadMovement);
Expect(openOffroadSpeed.FinalSpeed == 10f && !openOffroadSpeed.IsOnRoad, "Open offroad speed was incorrect.");
Expect(forestOffroadSpeed.FinalSpeed == 7f && forestOffroadSpeed.Terrain.TerrainId == "Forest", "Forest offroad speed was incorrect.");
Expect(rockyOffroadSpeed.FinalSpeed == 8f && rockyOffroadSpeed.Terrain.TerrainId == "Rocky", "Rocky offroad speed was incorrect.");
Expect(mudOffroadSpeed.FinalSpeed == 6f && mudOffroadSpeed.Terrain.TerrainId == "Mud", "Mud offroad speed was incorrect.");
Expect(roadOverForestSpeed.IsOnRoad, "Road over Forest was not detected as road.");
Expect(roadOverForestSpeed.FinalSpeed == 14f, "Road speed did not override underlying Forest movement.");
Expect(roadOverForestSpeed.MovementMultiplier == 1.4f, "Road and Forest multipliers were incorrectly combined.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoadMovementProfile(0f),
    "Zero road movement multiplier was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => MovementSpeedResolver.Resolve(0f, routeRear, terrainMovementResolver, reinforcementRoads, trialRoadMovement),
    "Zero base movement speed was accepted.");

var transitTopology = new SectorTopology();
transitTopology.RegisterSector("transit-rear");
transitTopology.RegisterSector("transit-middle");
transitTopology.RegisterSector("transit-front");
transitTopology.AddBidirectionalAdjacency("transit-rear", "transit-middle");
transitTopology.AddBidirectionalAdjacency("transit-middle", "transit-front");
var transitRearSector = new SectorState("transit-rear", "blue");
var transitMiddleSector = new SectorState("transit-middle", "blue");
var transitFrontSector = new SectorState("transit-front", "blue");
var transitTerritory = new TerritoryGraph(
    transitTopology,
    new[] { transitRearSector, transitMiddleSector, transitFrontSector },
    new[]
    {
        new SectorControlAnchor("transit-anchor-rear", "transit-rear"),
        new SectorControlAnchor("transit-anchor-middle", "transit-middle"),
        new SectorControlAnchor("transit-anchor-front", "transit-front")
    });
var transitSource = new SupplySourceDefinition("transit-source", "blue", "transit-rear");
var suppliedTransitSnapshot = StrategicSupplyResolver.Resolve(transitTerritory, "blue", new[] { transitSource });
var roadRoutePlanner = new ReinforcementRoutePlanner(reinforcementRoads, trialRoadMovement, terrainMovementResolver);
var activeTransits = new List<ReinforcementTransit>();
var roadDispatchRequest = new ReinforcementDispatchRequest(
    "reinforcement-road",
    "blue",
    "transit-rear",
    routeRear,
    "transit-front",
    routeFront,
    10f);
var roadDispatch = ReinforcementDispatchService.Dispatch(
    roadDispatchRequest,
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(roadDispatch.Success, "Supplied road reinforcement dispatch failed.");
Expect(roadDispatch.Transit != null, "Successful dispatch returned no transit.");
var roadTransit = roadDispatch.Transit!;
Expect(roadTransit.State == ReinforcementState.EnRoute, "Dispatch did not begin EnRoute.");
Expect(roadTransit.Route.Legs.Count == 2, "Road dispatch route leg count was incorrect.");
Expect(roadTransit.Route.Legs.All(leg => leg.Surface == ReinforcementRouteSurface.Road), "Road-preferred dispatch used offroad legs.");
Expect(roadTransit.Route.TotalDistance == 100f, "Road route total distance was incorrect.");
Expect(roadTransit.Route.EstimateTravelTime(10f) < 8f, "Road route trial travel time was not accelerated.");
roadTransit.Advance(1f);
Expect(roadTransit.State == ReinforcementState.EnRoute, "Partial road advance arrived too early.");
Expect(roadTransit.DistanceTravelled == 14f, "Explicit road advance distance was incorrect.");
Expect(roadTransit.CurrentPosition.X == 14f, "Road transit world position did not advance.");
roadTransit.Advance(10f);
Expect(roadTransit.State == ReinforcementState.Arrived, "Completed road route did not arrive.");
Expect(roadTransit.CurrentPosition.X == routeFront.X, "Arrived reinforcement was not at the destination.");
ExpectThrows<InvalidOperationException>(
    () => roadTransit.MarkDestroyedEnRoute(),
    "Arrived reinforcement was destroyed en route.");

var noRoadNetwork = new RoadNetwork();
var forestOnlyResolver = new RectangularTerrainMovementResolver(forestMovement);
var offroadPlanner = new ReinforcementRoutePlanner(noRoadNetwork, trialRoadMovement, forestOnlyResolver);
var offroadRequest = new ReinforcementDispatchRequest(
    "reinforcement-offroad",
    "blue",
    "transit-rear",
    routeRear,
    "transit-front",
    routeFront,
    10f);
var offroadDispatch = ReinforcementDispatchService.Dispatch(
    offroadRequest,
    transitTerritory,
    suppliedTransitSnapshot,
    offroadPlanner,
    activeTransits);
Expect(offroadDispatch.Success, "Supplied offroad fallback dispatch failed.");
var offroadTransit = offroadDispatch.Transit!;
Expect(offroadTransit.Route.Legs.Count == 1, "Offroad fallback did not produce one direct leg.");
Expect(offroadTransit.Route.Legs[0].Surface == ReinforcementRouteSurface.Offroad, "Offroad fallback used a road leg.");
Expect(offroadTransit.Route.Legs[0].MovementMultiplier == 0.7f, "Offroad route did not use terrain movement.");
roadTransit = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-road-speed", "blue", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits).Transit!;
roadTransit.Advance(8f);
offroadTransit.Advance(8f);
Expect(roadTransit.State == ReinforcementState.Arrived, "Road transit did not beat slow offroad transit.");
Expect(offroadTransit.State == ReinforcementState.EnRoute, "Slow offroad transit arrived as quickly as road transit.");

var interceptedDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-intercepted", "blue", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
var interceptedTransit = interceptedDispatch.Transit!;
interceptedTransit.Advance(1f);
var interceptedPosition = interceptedTransit.CurrentPosition;
interceptedTransit.MarkDestroyedEnRoute();
Expect(interceptedTransit.State == ReinforcementState.DestroyedEnRoute, "En-route destruction state was not retained.");
interceptedTransit.Advance(100f);
Expect(interceptedTransit.State == ReinforcementState.DestroyedEnRoute, "Destroyed transit later arrived.");
Expect(interceptedTransit.CurrentPosition.X == interceptedPosition.X, "Destroyed transit continued moving.");
Expect(suppliedTransitSnapshot.GetStatus("transit-front") == SectorSupplyStatus.Supplied, "Physical interdiction changed strategic supply.");

var duplicateDispatchCount = activeTransits.Count;
var duplicateDispatch = ReinforcementDispatchService.Dispatch(
    roadDispatchRequest,
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(!duplicateDispatch.Success, "Duplicate reinforcement id dispatch succeeded.");
Expect(duplicateDispatch.FailureReason == ReinforcementDispatchFailureReason.DuplicateReinforcementId, "Duplicate reinforcement failure reason was incorrect.");
Expect(activeTransits.Count == duplicateDispatchCount, "Failed duplicate dispatch mutated transit collection.");

var unknownSourceDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-unknown-source", "blue", "missing", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(unknownSourceDispatch.FailureReason == ReinforcementDispatchFailureReason.UnknownSourceSector, "Unknown source dispatch reason was incorrect.");
var unknownDestinationDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-unknown-destination", "blue", "transit-rear", routeRear, "missing", routeFront, 10f),
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(unknownDestinationDispatch.FailureReason == ReinforcementDispatchFailureReason.UnknownDestinationSector, "Unknown destination dispatch reason was incorrect.");
var factionMismatchDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-red", "red", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    suppliedTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(factionMismatchDispatch.FailureReason == ReinforcementDispatchFailureReason.FactionMismatch, "Faction mismatch dispatch reason was incorrect.");

transitMiddleSector.TransferOwnershipTo("red");
var transitCutOffSnapshot = StrategicSupplyResolver.Resolve(transitTerritory, "blue", new[] { transitSource });
Expect(transitCutOffSnapshot.GetStatus("transit-front") == SectorSupplyStatus.CutOff, "Corridor loss did not cut off transit destination.");
var activeBeforeCutOffDispatch = activeTransits.Count;
var cutOffDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-cutoff", "blue", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    transitCutOffSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(!cutOffDispatch.Success, "Cut-off destination accepted a new dispatch.");
Expect(cutOffDispatch.FailureReason == ReinforcementDispatchFailureReason.DestinationCutOff, "Cut-off dispatch failure reason was incorrect.");
Expect(activeTransits.Count == activeBeforeCutOffDispatch, "Cut-off dispatch mutated transit collection.");
Expect(offroadTransit.State == ReinforcementState.EnRoute, "CutOff automatically deleted an existing in-flight reinforcement.");

var sourceNotSuppliedDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-source-cutoff", "blue", "transit-front", routeFront, "transit-rear", routeRear, 10f),
    transitTerritory,
    transitCutOffSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(sourceNotSuppliedDispatch.FailureReason == ReinforcementDispatchFailureReason.SourceNotSupplied, "Cut-off source dispatch reason was incorrect.");

transitFrontSector.TransferOwnershipTo("red");
var transitNotOwnedSnapshot = StrategicSupplyResolver.Resolve(transitTerritory, "blue", new[] { transitSource });
var notOwnedDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-not-owned", "blue", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    transitNotOwnedSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(notOwnedDispatch.FailureReason == ReinforcementDispatchFailureReason.DestinationNotOwned, "Not-owned destination dispatch reason was incorrect.");

transitMiddleSector.TransferOwnershipTo("blue");
transitFrontSector.TransferOwnershipTo("blue");
var restoredTransitSnapshot = StrategicSupplyResolver.Resolve(transitTerritory, "blue", new[] { transitSource });
var restoredDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest("reinforcement-restored", "blue", "transit-rear", routeRear, "transit-front", routeFront, 10f),
    transitTerritory,
    restoredTransitSnapshot,
    roadRoutePlanner,
    activeTransits);
Expect(restoredDispatch.Success, "Restored strategic route did not allow a new dispatch.");
Expect(restoredDispatch.Transit!.State == ReinforcementState.EnRoute, "Restored dispatch did not begin physical transit.");

// RTS-CORE-03C: Production completes at its Building source before physical dispatch.
var rifleSquadProduction = new ProductionDefinition("rifle-squad", 50, 12f, 10f);
Expect(rifleSquadProduction.UnitTypeId == "rifle-squad", "Production unit type was not retained.");
Expect(rifleSquadProduction.PrototypeCost == 50, "Prototype production cost was not retained.");
Expect(rifleSquadProduction.ProductionSeconds == 12f, "Production seconds were not retained.");
Expect(rifleSquadProduction.BaseMovementSpeed == 10f, "Production movement speed was not retained.");
ExpectThrows<ArgumentException>(
    () => new ProductionDefinition(" ", 50, 12f, 10f),
    "Blank production unit type was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("negative-cost", -1, 12f, 10f),
    "Negative production cost was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("zero-time", 0, 0f, 10f),
    "Zero production time was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("negative-time", 0, -1f, 10f),
    "Negative production time was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("nan-time", 0, float.NaN, 10f),
    "NaN production time was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("infinite-time", 0, float.PositiveInfinity, 10f),
    "Infinite production time was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("zero-speed", 0, 12f, 0f),
    "Zero production movement speed was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ProductionDefinition("nan-speed", 0, 12f, float.NaN),
    "NaN production movement speed was accepted.");

var barracksProduction = new ProductionFacilityProfile("barracks", new[] { "rifle-squad" });
var depotProduction = new ProductionFacilityProfile("depot", Array.Empty<string>());
Expect(barracksProduction.BuildingTypeId == "barracks", "Facility profile building type was not retained.");
Expect(barracksProduction.CanProduce("rifle-squad"), "Barracks could not produce its registered squad.");
Expect(!depotProduction.CanProduce("rifle-squad"), "Depot unexpectedly produced a rifle squad.");
Expect(barracksProduction.ProducibleUnitTypeIds.Count == 1, "Facility unit type count was incorrect.");
ExpectThrows<ArgumentException>(
    () => new ProductionFacilityProfile(" ", new[] { "rifle-squad" }),
    "Blank facility building type was accepted.");
ExpectThrows<ArgumentException>(
    () => new ProductionFacilityProfile("duplicate-barracks", new[] { "rifle-squad", "rifle-squad" }),
    "Duplicate producible unit type was accepted.");

var productionTopology = new SectorTopology();
productionTopology.RegisterSector("production-rear");
productionTopology.RegisterSector("production-middle");
productionTopology.RegisterSector("production-front");
productionTopology.AddBidirectionalAdjacency("production-rear", "production-middle");
productionTopology.AddBidirectionalAdjacency("production-middle", "production-front");
var productionRearSector = new SectorState("production-rear", "blue");
var productionMiddleSector = new SectorState("production-middle", "blue");
var productionFrontSector = new SectorState("production-front", "blue");
var productionTerritory = new TerritoryGraph(
    productionTopology,
    new[] { productionRearSector, productionMiddleSector, productionFrontSector },
    new[]
    {
        new SectorControlAnchor("production-anchor-rear", "production-rear"),
        new SectorControlAnchor("production-anchor-middle", "production-middle"),
        new SectorControlAnchor("production-anchor-front", "production-front")
    });
var barracksPosition = new WorldPoint(10f, 20f);
var productionJunction = new WorldPoint(60f, 20f);
var productionDestination = new WorldPoint(110f, 20f);
var blueBarracks = new BuildingState(
    "production-barracks",
    "barracks",
    "blue",
    "production-rear",
    barracksPosition,
    5f);
var redBarracks = new BuildingState(
    "red-production-barracks",
    "barracks",
    "red",
    "production-rear",
    new WorldPoint(20f, 40f),
    5f);
var blueDepot = new BuildingState(
    "production-depot",
    "depot",
    "blue",
    "production-rear",
    new WorldPoint(30f, 40f),
    5f);
var productionBuildings = new List<BuildingState> { blueBarracks, redBarracks, blueDepot };
var incompleteBarracks = new ConstructionSite(
    "production-site",
    "barracks",
    "blue",
    "production-rear",
    new WorldPoint(35f, 15f),
    5f);
var productionSites = new List<ConstructionSite> { incompleteBarracks };
var productionDefinitions = new[] { rifleSquadProduction };
var productionProfiles = new[] { barracksProduction, depotProduction };
var productionEconomy = new EconomyState("blue", 500);
var productionQueues = new List<ProductionQueue>();
var readyAtSource = new List<ReadyReinforcement>();

ProductionRequest CreateProductionRequest(string orderId, string reinforcementId)
{
    return new ProductionRequest(
        orderId,
        reinforcementId,
        blueBarracks.BuildingId,
        "blue",
        "rifle-squad",
        "production-front",
        productionDestination);
}

var enqueueA = ProductionService.Enqueue(
    CreateProductionRequest("production-order-a", "produced-reinforcement-a"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(enqueueA.Success, "Valid production request did not enqueue.");
Expect(enqueueA.PreviousBalance == 500 && enqueueA.NewBalance == 450, "Production cost result was incorrect.");
Expect(productionEconomy.Balance == 450, "Production cost was not deducted on enqueue.");
Expect(productionQueues.Count == 1, "Production queue was not created for the facility.");
Expect(productionQueues[0].FacilityBuildingId == blueBarracks.BuildingId, "Production queue facility id was incorrect.");
Expect(productionQueues[0].Orders.Count == 1, "First production order was not queued.");
Expect(productionQueues[0].CurrentOrder!.OrderId == "production-order-a", "FIFO head was not the first order.");

var enqueueB = ProductionService.Enqueue(
    CreateProductionRequest("production-order-b", "produced-reinforcement-b"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
var enqueueC = ProductionService.Enqueue(
    CreateProductionRequest("production-order-c", "produced-reinforcement-c"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(enqueueB.Success && enqueueC.Success, "FIFO follow-up orders did not enqueue.");
Expect(productionQueues[0].Orders.Count == 3, "FIFO queue did not retain three orders.");
Expect(productionEconomy.Balance == 350, "Three production costs were not deducted exactly once.");

var queueCountBeforeFailure = productionQueues[0].Orders.Count;
var balanceBeforeFailure = productionEconomy.Balance;
var duplicateOrder = ProductionService.Enqueue(
    CreateProductionRequest("production-order-a", "produced-reinforcement-duplicate-order"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!duplicateOrder.Success && duplicateOrder.FailureReason == ProductionEnqueueFailureReason.DuplicateOrderId, "Duplicate order id was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Duplicate order failure was not atomic.");

var duplicateReinforcement = ProductionService.Enqueue(
    CreateProductionRequest("production-order-duplicate-reinforcement", "produced-reinforcement-a"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!duplicateReinforcement.Success && duplicateReinforcement.FailureReason == ProductionEnqueueFailureReason.DuplicateReinforcementId, "Duplicate reinforcement id was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Duplicate reinforcement failure was not atomic.");

var enemyFacilityRequest = new ProductionRequest(
    "production-order-enemy",
    "produced-reinforcement-enemy",
    redBarracks.BuildingId,
    "blue",
    "rifle-squad",
    "production-front",
    productionDestination);
var enemyFacilityResult = ProductionService.Enqueue(
    enemyFacilityRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!enemyFacilityResult.Success && enemyFacilityResult.FailureReason == ProductionEnqueueFailureReason.EnemyFacility, "Enemy production facility was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Enemy facility failure was not atomic.");

var depotRequest = new ProductionRequest(
    "production-order-depot",
    "produced-reinforcement-depot",
    blueDepot.BuildingId,
    "blue",
    "rifle-squad",
    "production-front",
    productionDestination);
var depotResult = ProductionService.Enqueue(
    depotRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!depotResult.Success && depotResult.FailureReason == ProductionEnqueueFailureReason.UnsupportedUnitType, "Non-producing Depot was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Unsupported facility failure was not atomic.");

var incompleteRequest = new ProductionRequest(
    "production-order-site",
    "produced-reinforcement-site",
    incompleteBarracks.SiteId,
    "blue",
    "rifle-squad",
    "production-front",
    productionDestination);
var incompleteResult = ProductionService.Enqueue(
    incompleteRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!incompleteResult.Success && incompleteResult.FailureReason == ProductionEnqueueFailureReason.FacilityIncomplete, "ConstructionSite produced a unit.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Incomplete facility failure was not atomic.");

var unknownFacilityRequest = new ProductionRequest(
    "production-order-missing",
    "produced-reinforcement-missing",
    "missing-building",
    "blue",
    "rifle-squad",
    "production-front",
    productionDestination);
var unknownFacilityResult = ProductionService.Enqueue(
    unknownFacilityRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!unknownFacilityResult.Success && unknownFacilityResult.FailureReason == ProductionEnqueueFailureReason.UnknownFacility, "Unknown production facility was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Unknown facility failure was not atomic.");

var invalidDestinationRequest = new ProductionRequest(
    "production-order-invalid-position",
    "produced-reinforcement-invalid-position",
    blueBarracks.BuildingId,
    "blue",
    "rifle-squad",
    "production-front",
    new WorldPoint(float.NaN, 20f));
var invalidDestinationResult = ProductionService.Enqueue(
    invalidDestinationRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!invalidDestinationResult.Success && invalidDestinationResult.FailureReason == ProductionEnqueueFailureReason.InvalidDestination, "Invalid production destination was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Invalid destination failure was not atomic.");

var unknownDestinationRequest = new ProductionRequest(
    "production-order-unknown-destination",
    "produced-reinforcement-unknown-destination",
    blueBarracks.BuildingId,
    "blue",
    "rifle-squad",
    "missing-sector",
    productionDestination);
var unknownDestinationResult = ProductionService.Enqueue(
    unknownDestinationRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!unknownDestinationResult.Success && unknownDestinationResult.FailureReason == ProductionEnqueueFailureReason.UnknownDestinationSector, "Unknown production destination sector was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Unknown destination failure was not atomic.");

var poorEconomy = new EconomyState("blue", 49);
var poorQueues = new List<ProductionQueue>();
var productionInsufficientResult = ProductionService.Enqueue(
    CreateProductionRequest("production-order-poor", "produced-reinforcement-poor"),
    productionTerritory,
    poorEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    poorQueues,
    readyAtSource);
Expect(!productionInsufficientResult.Success && productionInsufficientResult.FailureReason == ProductionEnqueueFailureReason.InsufficientFunds, "Insufficient production funds were not rejected.");
Expect(poorEconomy.Balance == 49 && poorQueues.Count == 0, "Insufficient funds failure was not atomic.");

var ghostProfile = new ProductionFacilityProfile("barracks", new[] { "rifle-squad", "ghost-squad" });
var unknownUnitRequest = new ProductionRequest(
    "production-order-ghost",
    "produced-reinforcement-ghost",
    blueBarracks.BuildingId,
    "blue",
    "ghost-squad",
    "production-front",
    productionDestination);
var unknownUnitResult = ProductionService.Enqueue(
    unknownUnitRequest,
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    new[] { ghostProfile, depotProduction },
    productionQueues,
    readyAtSource);
Expect(!unknownUnitResult.Success && unknownUnitResult.FailureReason == ProductionEnqueueFailureReason.UnknownUnitType, "Unknown unit definition was not rejected.");
Expect(productionEconomy.Balance == balanceBeforeFailure && productionQueues[0].Orders.Count == queueCountBeforeFailure, "Unknown unit failure was not atomic.");

var queue = productionQueues[0];
var partialFive = ProductionAdvanceService.Advance(queue, blueBarracks, 5f, readyAtSource);
Expect(partialFive.Success && partialFive.CompletedReinforcements.Count == 0, "Partial production advance completed too early.");
Expect(queue.CurrentOrder!.OrderId == "production-order-a" && queue.CurrentOrder.ProgressSeconds == 5f, "First order did not receive explicit progress.");
Expect(queue.Orders[1].ProgressSeconds == 0f && queue.Orders[2].ProgressSeconds == 0f, "FIFO advanced later orders before the first.");
var partialEleven = ProductionAdvanceService.Advance(queue, blueBarracks, 6f, readyAtSource);
Expect(partialEleven.Success && queue.CurrentOrder!.ProgressSeconds == 11f, "Eleven seconds of production progress were incorrect.");
Expect(readyAtSource.Count == 0, "Production completed before its full explicit duration.");
var overshootAdvance = ProductionAdvanceService.Advance(queue, blueBarracks, 6f, readyAtSource);
Expect(overshootAdvance.Success && overshootAdvance.CompletedReinforcements.Count == 1, "Overshoot did not complete the first order.");
Expect(queue.CurrentOrder!.OrderId == "production-order-b", "FIFO did not move to the second order.");
Expect(queue.CurrentOrder.ProgressSeconds == 5f, "Overshoot remainder was not applied to the second order.");
Expect(readyAtSource.Count == 1, "Completed production did not create one ready reinforcement.");
var readyA = readyAtSource[0];
Expect(readyA.ReinforcementId == "produced-reinforcement-a", "Ready reinforcement id changed after production.");
Expect(readyA.OrderId == "production-order-a", "Ready reinforcement lost its order id.");
Expect(readyA.UnitTypeId == "rifle-squad", "Ready reinforcement lost its unit type.");
Expect(readyA.SourceBuildingId == blueBarracks.BuildingId, "Ready source building was not the completed facility.");
Expect(readyA.SourceSectorId == blueBarracks.SectorId, "Ready source sector was not derived from BuildingState.");
Expect(readyA.SourcePosition.X == blueBarracks.Position.X && readyA.SourcePosition.Z == blueBarracks.Position.Z, "Ready source position was not the actual BuildingState position.");
Expect(readyA.DestinationSectorId == "production-front", "Ready destination sector was not retained.");
Expect(readyA.DestinationPosition.X == productionDestination.X, "Ready destination position was not retained.");
Expect(readyA.BaseMovementSpeed == rifleSquadProduction.BaseMovementSpeed, "Ready movement speed was not derived from its definition.");
Expect(readyAtSource.All(item => item.ReinforcementId != "arrived"), "Production completion created a front arrival marker.");

var completeSecond = ProductionAdvanceService.Advance(queue, blueBarracks, 7f, readyAtSource);
Expect(completeSecond.Success && completeSecond.CompletedReinforcements.Count == 1, "Second FIFO order did not complete after its remaining time.");
Expect(queue.CurrentOrder!.OrderId == "production-order-c" && queue.CurrentOrder.ProgressSeconds == 0f, "Third order did not remain untouched after exact completion.");
var readyB = readyAtSource.Single(item => item.OrderId == "production-order-b");

var ownershipQueueEconomy = new EconomyState("blue", 100);
var ownershipQueues = new List<ProductionQueue>();
var ownershipReady = new List<ReadyReinforcement>();
var ownershipEnqueue = ProductionService.Enqueue(
    CreateProductionRequest("production-order-owner", "produced-reinforcement-owner"),
    productionTerritory,
    ownershipQueueEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    ownershipQueues,
    ownershipReady);
Expect(ownershipEnqueue.Success, "Ownership-conflict fixture did not enqueue.");
blueBarracks.TransferOwnershipTo("red");
var ownershipAdvance = ProductionAdvanceService.Advance(ownershipQueues[0], blueBarracks, 12f, ownershipReady);
Expect(!ownershipAdvance.Success && ownershipAdvance.FailureReason == ProductionAdvanceFailureReason.OwnershipConflict, "Captured facility did not block production safely.");
Expect(ownershipQueues[0].Orders.Count == 1 && ownershipQueues[0].CurrentOrder!.ProgressSeconds == 0f, "Ownership conflict changed or deleted the queue.");
Expect(ownershipReady.Count == 0, "Captured queue auto-converted into new-owner troops.");
blueBarracks.TransferOwnershipTo("blue");

var productionRoads = new RoadNetwork();
productionRoads.Register(new RoadSegment(
    "production-road-a",
    barracksPosition,
    productionJunction,
    4f,
    new[] { "production-rear", "production-middle" }));
productionRoads.Register(new RoadSegment(
    "production-road-b",
    productionJunction,
    productionDestination,
    4f,
    new[] { "production-middle", "production-front" }));
var productionTerrain = new RectangularTerrainMovementResolver(new TerrainMovementProfile("Open", 1f));
var productionRoadPlanner = new ReinforcementRoutePlanner(
    productionRoads,
    new RoadMovementProfile(1.4f),
    productionTerrain);
var productionSupplySource = new SupplySourceDefinition(
    "production-rear-source",
    "blue",
    "production-rear");
var productionSuppliedSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
var producedTransits = new List<ReinforcementTransit>();
var readyCountBeforeRoadDispatch = readyAtSource.Count;
var integratedRoadDispatch = ProductionReinforcementIntegrationService.Dispatch(
    readyA,
    productionBuildings,
    productionTerritory,
    productionSuppliedSnapshot,
    productionRoadPlanner,
    readyAtSource,
    producedTransits);
Expect(integratedRoadDispatch.Success, "Ready reinforcement did not integrate with 03B dispatch.");
Expect(readyAtSource.Count == readyCountBeforeRoadDispatch - 1, "Successful dispatch did not leave Ready At Source exactly once.");
Expect(producedTransits.Count == 1, "Successful production dispatch did not create one transit.");
var producedRoadTransit = integratedRoadDispatch.Transit!;
Expect(producedRoadTransit.ReinforcementId == readyA.ReinforcementId, "Integration changed the reinforcement id.");
Expect(producedRoadTransit.State == ReinforcementState.EnRoute, "Integrated reinforcement did not begin EnRoute.");
Expect(producedRoadTransit.Route.Source.X == blueBarracks.Position.X && producedRoadTransit.Route.Source.Z == blueBarracks.Position.Z, "Physical route did not start at the actual Building position.");
Expect(producedRoadTransit.Route.Legs.All(item => item.Surface == ReinforcementRouteSurface.Road), "Connected Building road did not receive Road Preferred routing.");
Expect(producedRoadTransit.CurrentPosition.X == blueBarracks.Position.X, "Dispatch started away from the production facility.");
producedRoadTransit.Advance(1f);
Expect(producedRoadTransit.State == ReinforcementState.EnRoute, "Produced transit arrived without traversing its route.");
Expect(producedRoadTransit.CurrentPosition.X > blueBarracks.Position.X, "Produced transit did not move from the Building position.");
producedRoadTransit.Advance(20f);
Expect(producedRoadTransit.State == ReinforcementState.Arrived, "Produced reinforcement did not arrive after full physical movement.");
Expect(producedRoadTransit.CurrentPosition.X == productionDestination.X, "Produced reinforcement arrived at the wrong position.");

var productionOffroadPlanner = new ReinforcementRoutePlanner(
    new RoadNetwork(),
    new RoadMovementProfile(1.4f),
    new RectangularTerrainMovementResolver(new TerrainMovementProfile("Forest", 0.7f)));
var readyCountBeforeOffroadDispatch = readyAtSource.Count;
var integratedOffroadDispatch = ProductionReinforcementIntegrationService.Dispatch(
    readyB,
    productionBuildings,
    productionTerritory,
    productionSuppliedSnapshot,
    productionOffroadPlanner,
    readyAtSource,
    producedTransits);
Expect(integratedOffroadDispatch.Success, "Ready reinforcement did not use Offroad fallback.");
Expect(readyAtSource.Count == readyCountBeforeOffroadDispatch - 1, "Offroad dispatch did not remove one ready reinforcement.");
var producedOffroadTransit = integratedOffroadDispatch.Transit!;
Expect(producedOffroadTransit.Route.Legs.Count == 1, "Offroad fallback did not create a direct route.");
Expect(producedOffroadTransit.Route.Legs[0].Surface == ReinforcementRouteSurface.Offroad, "Offroad fallback used a Road leg.");
Expect(producedOffroadTransit.Route.Legs[0].MovementMultiplier == 0.7f, "Offroad fallback did not retain Terrain movement.");
producedOffroadTransit.Advance(1f);
var producedOffroadPosition = producedOffroadTransit.CurrentPosition;
producedOffroadTransit.MarkDestroyedEnRoute();
Expect(producedOffroadTransit.State == ReinforcementState.DestroyedEnRoute, "Produced reinforcement was not destroyed en route.");
producedOffroadTransit.Advance(100f);
Expect(producedOffroadTransit.State == ReinforcementState.DestroyedEnRoute, "Destroyed produced reinforcement later arrived.");
Expect(producedOffroadTransit.CurrentPosition.X == producedOffroadPosition.X, "Destroyed produced reinforcement kept moving.");
Expect(productionSuppliedSnapshot.GetStatus("production-front") == SectorSupplyStatus.Supplied, "Physical production interdiction changed Strategic Supply.");

var completeThird = ProductionAdvanceService.Advance(queue, blueBarracks, 12f, readyAtSource);
Expect(completeThird.Success && completeThird.CompletedReinforcements.Count == 1, "Third production order did not complete.");
Expect(queue.Orders.Count == 0, "Completed FIFO queue was not empty.");
var readyC = readyAtSource.Single(item => item.OrderId == "production-order-c");
var completedOrderDuplicate = ProductionService.Enqueue(
    CreateProductionRequest("production-order-a", "produced-reinforcement-after-complete"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!completedOrderDuplicate.Success && completedOrderDuplicate.FailureReason == ProductionEnqueueFailureReason.DuplicateOrderId, "Completed production order id was reusable.");
var completedReinforcementDuplicate = ProductionService.Enqueue(
    CreateProductionRequest("production-order-after-complete", "produced-reinforcement-a"),
    productionTerritory,
    productionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    productionQueues,
    readyAtSource);
Expect(!completedReinforcementDuplicate.Success && completedReinforcementDuplicate.FailureReason == ProductionEnqueueFailureReason.DuplicateReinforcementId, "Completed reinforcement id was reusable.");
productionMiddleSector.TransferOwnershipTo("red");
var productionCutOffSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
Expect(productionCutOffSnapshot.GetStatus("production-front") == SectorSupplyStatus.CutOff, "Production Front was not Cut Off after corridor loss.");
var readyCountBeforeBlockedDispatch = readyAtSource.Count;
var transitCountBeforeBlockedDispatch = producedTransits.Count;
var blockedReadyDispatch = ProductionReinforcementIntegrationService.Dispatch(
    readyC,
    productionBuildings,
    productionTerritory,
    productionCutOffSnapshot,
    productionRoadPlanner,
    readyAtSource,
    producedTransits);
Expect(!blockedReadyDispatch.Success, "CutOff accepted a produced reinforcement dispatch.");
Expect(blockedReadyDispatch.FailureReason == ProductionReinforcementIntegrationFailureReason.DispatchRejected, "CutOff integration failure reason was incorrect.");
Expect(blockedReadyDispatch.DispatchFailureReason == ReinforcementDispatchFailureReason.DestinationCutOff, "CutOff did not preserve the 03B dispatch reason.");
Expect(readyAtSource.Count == readyCountBeforeBlockedDispatch && readyAtSource.Contains(readyC), "CutOff deleted the Ready At Source reinforcement.");
Expect(producedTransits.Count == transitCountBeforeBlockedDispatch, "CutOff created a transit despite dispatch rejection.");

var cutOffProductionEconomy = new EconomyState("blue", 100);
var cutOffProductionQueues = new List<ProductionQueue>();
var cutOffReady = new List<ReadyReinforcement>();
var cutOffProductionEnqueue = ProductionService.Enqueue(
    CreateProductionRequest("production-order-cutoff", "produced-reinforcement-cutoff"),
    productionTerritory,
    cutOffProductionEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    cutOffProductionQueues,
    cutOffReady);
Expect(cutOffProductionEnqueue.Success, "Strategic CutOff incorrectly blocked the prototype production queue.");
var cutOffProductionAdvance = ProductionAdvanceService.Advance(
    cutOffProductionQueues[0],
    blueBarracks,
    12f,
    cutOffReady);
Expect(cutOffProductionAdvance.Success && cutOffReady.Count == 1, "CutOff production did not reach Ready At Source.");
Expect(cutOffReady[0].SourcePosition.X == blueBarracks.Position.X, "CutOff-produced reinforcement lost its Building position.");
var cutOffProducedDispatch = ProductionReinforcementIntegrationService.Dispatch(
    cutOffReady[0],
    productionBuildings,
    productionTerritory,
    productionCutOffSnapshot,
    productionRoadPlanner,
    cutOffReady,
    producedTransits);
Expect(!cutOffProducedDispatch.Success && cutOffReady.Count == 1, "CutOff-produced Ready reinforcement was not retained.");

productionMiddleSector.TransferOwnershipTo("blue");
var productionRestoredSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
var restoredReadyDispatch = ProductionReinforcementIntegrationService.Dispatch(
    readyC,
    productionBuildings,
    productionTerritory,
    productionRestoredSnapshot,
    productionRoadPlanner,
    readyAtSource,
    producedTransits);
Expect(restoredReadyDispatch.Success, "Restored Supply did not dispatch the same Ready reinforcement.");
Expect(restoredReadyDispatch.Transit!.ReinforcementId == readyC.ReinforcementId, "Restore dispatch created a different reinforcement id.");
Expect(!readyAtSource.Contains(readyC), "Restored dispatch left a duplicate Ready reinforcement.");
Expect(restoredReadyDispatch.Transit.State == ReinforcementState.EnRoute, "Restored Ready reinforcement did not become EnRoute.");

var cutOffRestoredDispatch = ProductionReinforcementIntegrationService.Dispatch(
    cutOffReady[0],
    productionBuildings,
    productionTerritory,
    productionRestoredSnapshot,
    productionRoadPlanner,
    cutOffReady,
    producedTransits);
Expect(cutOffRestoredDispatch.Success && cutOffReady.Count == 0, "Supply restore did not release the CutOff-produced reinforcement.");

var capturedReadyEconomy = new EconomyState("blue", 100);
var capturedReadyQueues = new List<ProductionQueue>();
var capturedReady = new List<ReadyReinforcement>();
var capturedReadyEnqueue = ProductionService.Enqueue(
    CreateProductionRequest("production-order-ready-capture", "produced-reinforcement-ready-capture"),
    productionTerritory,
    capturedReadyEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    capturedReadyQueues,
    capturedReady);
Expect(capturedReadyEnqueue.Success, "Ready capture fixture did not enqueue.");
ProductionAdvanceService.Advance(capturedReadyQueues[0], blueBarracks, 12f, capturedReady);
Expect(capturedReady.Count == 1, "Ready capture fixture did not complete production.");
blueBarracks.TransferOwnershipTo("red");
var capturedSourceDispatch = ProductionReinforcementIntegrationService.Dispatch(
    capturedReady[0],
    productionBuildings,
    productionTerritory,
    productionRestoredSnapshot,
    productionRoadPlanner,
    capturedReady,
    producedTransits);
Expect(!capturedSourceDispatch.Success && capturedSourceDispatch.FailureReason == ProductionReinforcementIntegrationFailureReason.SourceOwnershipConflict, "Captured source did not block Ready dispatch safely.");
Expect(capturedReady.Count == 1, "Captured source deleted or transferred the Ready reinforcement.");
blueBarracks.TransferOwnershipTo("blue");

productionFrontSector.TransferOwnershipTo("red");
var productionDestinationLostSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
var lostDestinationDispatch = ProductionReinforcementIntegrationService.Dispatch(
    capturedReady[0],
    productionBuildings,
    productionTerritory,
    productionDestinationLostSnapshot,
    productionRoadPlanner,
    capturedReady,
    producedTransits);
Expect(!lostDestinationDispatch.Success && lostDestinationDispatch.DispatchFailureReason == ReinforcementDispatchFailureReason.DestinationNotOwned, "Lost destination did not reject Ready dispatch.");
Expect(capturedReady.Count == 1, "Lost destination deleted the Ready reinforcement.");
productionFrontSector.TransferOwnershipTo("blue");

var productionIncomeProfiles = new[]
{
    new SectorIncomeProfile("production-rear", 10),
    new SectorIncomeProfile("production-middle", 20),
    new SectorIncomeProfile("production-front", 30)
};
var balanceBeforeProductionIncome = productionEconomy.Balance;
var productionIncome = SectorIncomeCollector.Collect(
    productionTerritory,
    productionIncomeProfiles,
    productionEconomy);
Expect(productionIncome.CollectedAmount == 60, "Production integration changed ownership-based Sector Income.");
Expect(productionEconomy.Balance == balanceBeforeProductionIncome + 60, "Production cost state did not remain compatible with income collection.");

// RTS-CORE-03D: Route threat changes route preference, never strategic connectivity.
var mobileThreat = new RouteThreatSource(
    "mobile-threat-north",
    "red",
    new WorldPoint(50f, 4f),
    10f,
    RouteThreatKind.MobileUnit,
    1f);
var fortifiedThreat = new RouteThreatSource(
    "fortified-threat-north",
    "red",
    new WorldPoint(50f, 4f),
    10f,
    RouteThreatKind.FortifiedPosition,
    2.5f);
Expect(mobileThreat.ThreatSourceId == "mobile-threat-north", "Mobile threat id was not retained.");
Expect(mobileThreat.FactionId == "red", "Mobile threat faction was not retained.");
Expect(mobileThreat.Position.X == 50f && mobileThreat.Position.Z == 4f, "Mobile threat position was not retained.");
Expect(mobileThreat.ThreatRadius == 10f, "Mobile threat radius was not retained.");
Expect(mobileThreat.ThreatKind == RouteThreatKind.MobileUnit, "Mobile threat kind was not retained.");
Expect(mobileThreat.ThreatWeight == 1f, "Mobile threat weight was not retained.");
Expect(fortifiedThreat.ThreatKind == RouteThreatKind.FortifiedPosition, "Fortified threat kind was not retained.");
Expect(fortifiedThreat.ThreatWeight > mobileThreat.ThreatWeight, "Fortified fixture threat was not stronger than mobile fixture threat.");
ExpectThrows<ArgumentException>(
    () => new RouteThreatSource(" ", "red", new WorldPoint(0f, 0f), 1f, RouteThreatKind.MobileUnit, 1f),
    "Blank threat source id was accepted.");
ExpectThrows<ArgumentException>(
    () => new RouteThreatSource("blank-faction", " ", new WorldPoint(0f, 0f), 1f, RouteThreatKind.MobileUnit, 1f),
    "Blank threat faction was accepted.");
ExpectThrows<ArgumentException>(
    () => new RouteThreatSource("nan-position", "red", new WorldPoint(float.NaN, 0f), 1f, RouteThreatKind.MobileUnit, 1f),
    "NaN threat position was accepted.");
ExpectThrows<ArgumentException>(
    () => new RouteThreatSource("infinite-position", "red", new WorldPoint(0f, float.PositiveInfinity), 1f, RouteThreatKind.MobileUnit, 1f),
    "Infinite threat position was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("zero-radius", "red", new WorldPoint(0f, 0f), 0f, RouteThreatKind.MobileUnit, 1f),
    "Zero threat radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("negative-radius", "red", new WorldPoint(0f, 0f), -1f, RouteThreatKind.MobileUnit, 1f),
    "Negative threat radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("nan-radius", "red", new WorldPoint(0f, 0f), float.NaN, RouteThreatKind.MobileUnit, 1f),
    "NaN threat radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("infinite-radius", "red", new WorldPoint(0f, 0f), float.PositiveInfinity, RouteThreatKind.MobileUnit, 1f),
    "Infinite threat radius was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("negative-weight", "red", new WorldPoint(0f, 0f), 1f, RouteThreatKind.MobileUnit, -1f),
    "Negative threat weight was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("nan-weight", "red", new WorldPoint(0f, 0f), 1f, RouteThreatKind.MobileUnit, float.NaN),
    "NaN threat weight was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("infinite-weight", "red", new WorldPoint(0f, 0f), 1f, RouteThreatKind.MobileUnit, float.PositiveInfinity),
    "Infinite threat weight was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RouteThreatSource("invalid-kind", "red", new WorldPoint(0f, 0f), 1f, (RouteThreatKind)99, 1f),
    "Unknown threat kind was accepted.");

var balancedRouteProfile = new RoutePlanningProfile(1d, 15d);
Expect(balancedRouteProfile.TravelTimeWeight == 1d, "Travel-time planning weight was not retained.");
Expect(balancedRouteProfile.ThreatWeight == 15d, "Threat planning weight was not retained.");
Expect(balancedRouteProfile.Score(10d, 1d) == 25d, "Route planning score was incorrect.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoutePlanningProfile(double.NaN, 1d),
    "NaN travel-time weight was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoutePlanningProfile(1d, double.PositiveInfinity),
    "Infinite threat planning weight was accepted.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new RoutePlanningProfile(-1d, 1d),
    "Negative travel-time weight was accepted.");
ExpectThrows<ArgumentException>(
    () => new RoutePlanningProfile(0d, 0d),
    "Zero-only route planning profile was accepted.");

var routeThreatSource = new WorldPoint(0f, 0f);
var routeThreatNorthJunction = new WorldPoint(40f, 0f);
var routeThreatSouthStart = new WorldPoint(0f, 50f);
var routeThreatSouthEnd = new WorldPoint(100f, 50f);
var routeThreatDestination = new WorldPoint(100f, 0f);
var alternateThreatRoads = new RoadNetwork();
alternateThreatRoads.Register(new RoadSegment(
    "threat-north-a",
    routeThreatSource,
    routeThreatNorthJunction,
    4f,
    new[] { "production-rear", "production-middle" }));
alternateThreatRoads.Register(new RoadSegment(
    "threat-north-b",
    routeThreatNorthJunction,
    routeThreatDestination,
    4f,
    new[] { "production-middle", "production-front" }));
alternateThreatRoads.Register(new RoadSegment(
    "threat-south-a",
    routeThreatSource,
    routeThreatSouthStart,
    4f,
    new[] { "production-rear", "production-middle" }));
alternateThreatRoads.Register(new RoadSegment(
    "threat-south-b",
    routeThreatSouthStart,
    routeThreatSouthEnd,
    4f,
    new[] { "production-middle" }));
alternateThreatRoads.Register(new RoadSegment(
    "threat-south-c",
    routeThreatSouthEnd,
    routeThreatDestination,
    4f,
    new[] { "production-middle", "production-front" }));
var alternatePaths = alternateThreatRoads.FindPaths(routeThreatSource, routeThreatDestination);
Expect(alternatePaths.Count == 2, "Road network did not expose both route candidates.");
Expect(alternatePaths[0].Count == 2, "Shortest North route was not the first candidate.");
Expect(alternatePaths[1].Count == 3, "Longer South route was not retained as an alternate.");
var alternateThreatPlanner = new ReinforcementRoutePlanner(
    alternateThreatRoads,
    new RoadMovementProfile(1f),
    new RectangularTerrainMovementResolver(new TerrainMovementProfile("Open", 1f)));
var routeThreatRequest = new ReinforcementDispatchRequest(
    "route-threat-request",
    "blue",
    "production-rear",
    routeThreatSource,
    "production-front",
    routeThreatDestination,
    10f);
var defaultThreatRoute = alternateThreatPlanner.Plan(routeThreatRequest);
Expect(defaultThreatRoute.TotalDistance == 100f, "Threat-free default did not preserve the shortest Road route.");
var noThreatAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    Array.Empty<RouteThreatSource>());
Expect(noThreatAssessment.TotalThreat == 0d, "Empty threat input produced threat score.");
Expect(!noThreatAssessment.HasThreat, "Empty threat input reported a threat.");
Expect(noThreatAssessment.ThreatSourceCount == 0, "Empty threat input reported source count.");
Expect(!noThreatAssessment.HasFortifiedThreat, "Empty threat input reported fortified threat.");

var friendlyThreat = new RouteThreatSource(
    "friendly-threat",
    "blue",
    new WorldPoint(50f, 0f),
    10f,
    RouteThreatKind.MobileUnit,
    5f);
var friendlyAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { friendlyThreat });
Expect(!friendlyAssessment.HasThreat && friendlyAssessment.TotalThreat == 0d, "Friendly route source counted as a threat.");
var outsideThreat = new RouteThreatSource(
    "outside-threat",
    "red",
    new WorldPoint(50f, 11f),
    10f,
    RouteThreatKind.MobileUnit,
    1f);
var outsideAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { outsideThreat });
Expect(!outsideAssessment.HasThreat, "Source outside its threat radius affected the route.");
var boundaryThreat = new RouteThreatSource(
    "boundary-threat",
    "red",
    new WorldPoint(50f, 10f),
    10f,
    RouteThreatKind.MobileUnit,
    1f);
var boundaryAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { boundaryThreat });
Expect(boundaryAssessment.HasThreat, "Source on the threat radius boundary was ignored.");
var mobileAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { mobileThreat });
Expect(mobileAssessment.HasThreat, "Enemy mobile source did not threaten the route.");
Expect(mobileAssessment.TotalThreat == 1d, "Enemy mobile threat weight was incorrect.");
Expect(mobileAssessment.ThreatSourceCount == 1, "Enemy mobile threat count was incorrect.");
Expect(!mobileAssessment.HasFortifiedThreat, "Mobile threat was reported as fortified.");
var fortifiedAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { fortifiedThreat });
Expect(fortifiedAssessment.HasFortifiedThreat, "Fortified route threat was not identified.");
Expect(fortifiedAssessment.TotalThreat == 2.5d, "Fortified route threat weight was incorrect.");
var secondMobileThreat = new RouteThreatSource(
    "mobile-threat-north-b",
    "red",
    new WorldPoint(70f, -3f),
    10f,
    RouteThreatKind.MobileUnit,
    0.75f);
var multipleAssessment = RouteThreatResolver.Resolve(
    "blue",
    defaultThreatRoute,
    new[] { mobileThreat, secondMobileThreat });
Expect(multipleAssessment.ThreatSourceCount == 2, "Multiple threats did not accumulate source count.");
Expect(multipleAssessment.TotalThreat == 1.75d, "Multiple threats did not accumulate score.");
ExpectThrows<ArgumentException>(
    () => RouteThreatResolver.Resolve("blue", defaultThreatRoute, new[] { mobileThreat, mobileThreat }),
    "Duplicate threat source ids were accepted.");

var safeAlternateRoute = alternateThreatPlanner.Plan(
    routeThreatRequest,
    new[] { mobileThreat },
    balancedRouteProfile,
    out var safeAlternateAssessment);
Expect(safeAlternateRoute.TotalDistance == 200f, "Safe South alternate was not selected over threatened North.");
Expect(!safeAlternateAssessment.HasThreat, "Selected safe alternate retained North threat.");
Expect(safeAlternateRoute.Legs.Any(leg => leg.Start.Z == 50f || leg.End.Z == 50f), "Selected alternate did not traverse the South route.");
var weakThreat = new RouteThreatSource(
    "weak-mobile-threat",
    "red",
    new WorldPoint(50f, 4f),
    10f,
    RouteThreatKind.MobileUnit,
    0.1f);
var fastThreatenedRoute = alternateThreatPlanner.Plan(
    routeThreatRequest,
    new[] { weakThreat },
    balancedRouteProfile,
    out var fastThreatenedAssessment);
Expect(fastThreatenedRoute.TotalDistance == 100f, "Weak threat hard-coded an unnecessary detour.");
Expect(fastThreatenedAssessment.HasThreat, "Weak threat disappeared from selected route assessment.");
var strongAlternateRoute = alternateThreatPlanner.Plan(
    routeThreatRequest,
    new[] { fortifiedThreat },
    balancedRouteProfile,
    out var strongAlternateAssessment);
Expect(strongAlternateRoute.TotalDistance == 200f, "Safe South route was not selected over fortified North.");
Expect(!strongAlternateAssessment.HasThreat, "Fortified North threat leaked into South assessment.");

var southThreat = new RouteThreatSource(
    "mobile-threat-south",
    "red",
    new WorldPoint(50f, 50f),
    10f,
    RouteThreatKind.MobileUnit,
    0.5f);
var allThreatenedRoute = alternateThreatPlanner.Plan(
    routeThreatRequest,
    new[] { mobileThreat, southThreat },
    balancedRouteProfile,
    out var allThreatenedAssessment);
Expect(allThreatenedRoute.TotalDistance == 100f, "All-threatened route selection did not choose the best score.");
Expect(allThreatenedAssessment.HasThreat, "All-threatened selection lost its threat assessment.");

var northOnlyRoads = new RoadNetwork();
northOnlyRoads.Register(new RoadSegment(
    "only-north-a",
    routeThreatSource,
    routeThreatNorthJunction,
    4f,
    new[] { "production-rear", "production-middle" }));
northOnlyRoads.Register(new RoadSegment(
    "only-north-b",
    routeThreatNorthJunction,
    routeThreatDestination,
    4f,
    new[] { "production-middle", "production-front" }));
var northOnlyPlanner = new ReinforcementRoutePlanner(
    northOnlyRoads,
    new RoadMovementProfile(1f),
    new RectangularTerrainMovementResolver(new TerrainMovementProfile("Open", 1f)));
var threatDispatches = new List<ReinforcementTransit>();
var onlyThreatenedDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest(
        "only-threatened-dispatch",
        "blue",
        "production-rear",
        routeThreatSource,
        "production-front",
        routeThreatDestination,
        10f),
    productionTerritory,
    productionSuppliedSnapshot,
    northOnlyPlanner,
    new[] { mobileThreat },
    balancedRouteProfile,
    threatDispatches);
Expect(onlyThreatenedDispatch.Success, "Only threatened route was treated as impassable.");
Expect(onlyThreatenedDispatch.Transit!.State == ReinforcementState.EnRoute, "Threatened route did not create an EnRoute transit.");
Expect(onlyThreatenedDispatch.Transit.RouteThreatAssessment.HasThreat, "Transit did not preserve its route threat snapshot.");
Expect(onlyThreatenedDispatch.Transit.Route.TotalDistance == 100f, "Only threatened route selected another path.");
var onlyFortifiedDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest(
        "only-fortified-dispatch",
        "blue",
        "production-rear",
        routeThreatSource,
        "production-front",
        routeThreatDestination,
        10f),
    productionTerritory,
    productionSuppliedSnapshot,
    northOnlyPlanner,
    new[] { fortifiedThreat },
    balancedRouteProfile,
    threatDispatches);
Expect(onlyFortifiedDispatch.Success, "Only fortified route was treated as impassable.");
Expect(onlyFortifiedDispatch.Transit!.RouteThreatAssessment.HasFortifiedThreat, "Fortified dispatch lost its strong-threat snapshot.");
Expect(productionSuppliedSnapshot.GetStatus("production-front") == SectorSupplyStatus.Supplied, "Route threat changed Strategic Supply status.");
Expect(productionMiddleSector.OwnerId == "blue", "Enemy presence changed Sector ownership without Capture.");

var noRoadThreatPlanner = new ReinforcementRoutePlanner(
    new RoadNetwork(),
    new RoadMovementProfile(1f),
    new RectangularTerrainMovementResolver(new TerrainMovementProfile("Open", 0.8f)));
var noRoadThreatDispatch = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest(
        "no-road-threat-dispatch",
        "blue",
        "production-rear",
        routeThreatSource,
        "production-front",
        routeThreatDestination,
        10f),
    productionTerritory,
    productionSuppliedSnapshot,
    noRoadThreatPlanner,
    new[] { mobileThreat },
    balancedRouteProfile,
    threatDispatches);
Expect(noRoadThreatDispatch.Success, "Missing Road was treated as Strategic Isolation.");
Expect(noRoadThreatDispatch.Transit!.Route.Legs.Count == 1, "Offroad fallback did not produce one direct leg.");
Expect(noRoadThreatDispatch.Transit.Route.Legs[0].Surface == ReinforcementRouteSurface.Offroad, "Missing Road did not use Offroad fallback.");
Expect(noRoadThreatDispatch.Transit.RouteThreatAssessment.HasThreat, "Threatened Offroad route lost its assessment.");

var threatReadyEconomy = new EconomyState("blue", 100);
var threatReadyQueues = new List<ProductionQueue>();
var threatReady = new List<ReadyReinforcement>();
var threatReadyEnqueue = ProductionService.Enqueue(
    CreateProductionRequest("production-order-threat", "produced-reinforcement-threat"),
    productionTerritory,
    threatReadyEconomy,
    productionBuildings,
    productionSites,
    productionDefinitions,
    productionProfiles,
    threatReadyQueues,
    threatReady);
Expect(threatReadyEnqueue.Success, "Threat-aware Ready fixture did not enqueue.");
ProductionAdvanceService.Advance(threatReadyQueues[0], blueBarracks, 12f, threatReady);
Expect(threatReady.Count == 1, "Threat-aware Ready fixture did not complete.");
var threatReadyId = threatReady[0].ReinforcementId;
var productionRouteThreat = new RouteThreatSource(
    "production-route-threat",
    "red",
    new WorldPoint(60f, 24f),
    12f,
    RouteThreatKind.MobileUnit,
    1f);
productionMiddleSector.TransferOwnershipTo("red");
var productionIsolationSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
Expect(productionIsolationSnapshot.GetStatus("production-front") == SectorSupplyStatus.CutOff, "Missing owned Territory path did not produce technical CutOff.");
var isolatedThreatReadyDispatch = ProductionReinforcementIntegrationService.Dispatch(
    threatReady[0],
    productionBuildings,
    productionTerritory,
    productionIsolationSnapshot,
    productionRoadPlanner,
    new[] { productionRouteThreat },
    balancedRouteProfile,
    threatReady,
    producedTransits);
Expect(!isolatedThreatReadyDispatch.Success, "Strategically isolated destination accepted a new Dispatch.");
Expect(isolatedThreatReadyDispatch.DispatchFailureReason == ReinforcementDispatchFailureReason.DestinationCutOff, "Strategic Isolation returned the wrong dispatch failure.");
Expect(threatReady.Count == 1 && threatReady[0].ReinforcementId == threatReadyId, "Strategic Isolation removed or replaced Ready reinforcement.");
productionMiddleSector.TransferOwnershipTo("blue");
var productionThreatRestoredSnapshot = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
var mutableProductionThreats = new List<RouteThreatSource> { productionRouteThreat };
var restoredThreatReadyDispatch = ProductionReinforcementIntegrationService.Dispatch(
    threatReady[0],
    productionBuildings,
    productionTerritory,
    productionThreatRestoredSnapshot,
    productionRoadPlanner,
    mutableProductionThreats,
    balancedRouteProfile,
    threatReady,
    producedTransits);
Expect(restoredThreatReadyDispatch.Success, "Restored Strategic connection did not dispatch threatened Ready reinforcement.");
Expect(restoredThreatReadyDispatch.Transit!.ReinforcementId == threatReadyId, "Restored threat-aware Dispatch changed Ready id.");
Expect(threatReady.Count == 0, "Successful threat-aware Dispatch left Ready reinforcement behind.");
Expect(restoredThreatReadyDispatch.Transit.RouteThreatAssessment.HasThreat, "Production integration lost route threat assessment.");
mutableProductionThreats.Clear();
Expect(restoredThreatReadyDispatch.Transit.RouteThreatAssessment.HasThreat, "Past Dispatch threat snapshot changed after source collection mutation.");
var routeBeforeIsolation = restoredThreatReadyDispatch.Transit.Route;
productionMiddleSector.TransferOwnershipTo("red");
_ = StrategicSupplyResolver.Resolve(productionTerritory, "blue", new[] { productionSupplySource });
Expect(restoredThreatReadyDispatch.Transit.State == ReinforcementState.EnRoute, "Strategic Isolation deleted or stopped an existing transit.");
Expect(ReferenceEquals(routeBeforeIsolation, restoredThreatReadyDispatch.Transit.Route), "Existing transit dynamically rerouted after Dispatch.");
productionMiddleSector.TransferOwnershipTo("blue");
restoredThreatReadyDispatch.Transit.MarkDestroyedEnRoute();
Expect(restoredThreatReadyDispatch.Transit.State == ReinforcementState.DestroyedEnRoute, "Physical interdiction did not retain DestroyedEnRoute meaning.");
var postInterdictionSupply = StrategicSupplyResolver.Resolve(
    productionTerritory,
    "blue",
    new[] { productionSupplySource });
Expect(postInterdictionSupply.GetStatus("production-front") == SectorSupplyStatus.Supplied, "DestroyedEnRoute changed Strategic Supply connectivity.");

var captureQueueTopology = new SectorTopology();
captureQueueTopology.RegisterSector("capture-blue-sector");
captureQueueTopology.RegisterSector("capture-red-sector");
captureQueueTopology.RegisterSector("capture-other-sector");
captureQueueTopology.AddBidirectionalAdjacency("capture-blue-sector", "capture-red-sector");
captureQueueTopology.AddBidirectionalAdjacency("capture-red-sector", "capture-other-sector");
var captureBlueSector = new SectorState("capture-blue-sector", "blue");
var captureRedSector = new SectorState("capture-red-sector", "red");
var captureOtherSector = new SectorState("capture-other-sector", "red");
var captureQueueTerritory = new TerritoryGraph(
    captureQueueTopology,
    new[] { captureBlueSector, captureRedSector, captureOtherSector },
    new[]
    {
        new SectorControlAnchor("capture-blue-anchor", "capture-blue-sector"),
        new SectorControlAnchor("capture-red-anchor", "capture-red-sector"),
        new SectorControlAnchor("capture-other-anchor", "capture-other-sector")
    });
var captureBarracksA = new BuildingState(
    "capture-barracks-a",
    "Barracks",
    "red",
    "capture-red-sector",
    new WorldPoint(10f, 0f),
    2f);
var captureBarracksB = new BuildingState(
    "capture-barracks-b",
    "Barracks",
    "red",
    "capture-red-sector",
    new WorldPoint(12f, 2f),
    2f);
var captureEmptyBarracks = new BuildingState(
    "capture-barracks-empty",
    "Barracks",
    "red",
    "capture-red-sector",
    new WorldPoint(14f, -2f),
    2f);
var captureDepot = new BuildingState(
    "capture-depot",
    "Depot",
    "red",
    "capture-red-sector",
    new WorldPoint(16f, 0f),
    2f);
var captureOtherBarracks = new BuildingState(
    "capture-other-barracks",
    "Barracks",
    "red",
    "capture-other-sector",
    new WorldPoint(30f, 0f),
    2f);
var captureBuildings = new List<BuildingState>
{
    captureBarracksA,
    captureBarracksB,
    captureEmptyBarracks,
    captureDepot,
    captureOtherBarracks
};
var captureDefinition = new ProductionDefinition("RifleSquad", 40, 12f, 2f);
var captureProfile = new ProductionFacilityProfile("Barracks", new[] { "RifleSquad" });
var captureDefinitions = new[] { captureDefinition };
var captureProfiles = new[] { captureProfile };
var captureSites = Array.Empty<ConstructionSite>();
var captureQueues = new List<ProductionQueue>();
var captureReady = new List<ReadyReinforcement>();
var redCaptureEconomy = new EconomyState("red", 1000);

ProductionEnqueueResult EnqueueCaptureOrder(
    string orderId,
    string reinforcementId,
    string facilityId,
    string factionId,
    EconomyState economy,
    string destinationSectorId,
    WorldPoint destinationPosition,
    ICollection<ProductionQueue> queues,
    IEnumerable<BuildingState> buildings,
    IEnumerable<ReadyReinforcement> ready)
{
    return ProductionService.Enqueue(
        new ProductionRequest(
            orderId,
            reinforcementId,
            facilityId,
            factionId,
            "RifleSquad",
            destinationSectorId,
            destinationPosition),
        captureQueueTerritory,
        economy,
        buildings,
        captureSites,
        captureDefinitions,
        captureProfiles,
        queues,
        ready);
}

var readyBeforeCaptureEnqueue = EnqueueCaptureOrder(
    "capture-ready-order",
    "capture-ready-r1",
    captureBarracksA.BuildingId,
    "red",
    redCaptureEconomy,
    "capture-other-sector",
    new WorldPoint(30f, 0f),
    captureQueues,
    captureBuildings,
    captureReady);
Expect(readyBeforeCaptureEnqueue.Success, "03E Ready fixture did not enqueue.");
var captureQueueA = captureQueues[0];
var readyBeforeCaptureAdvance = ProductionAdvanceService.Advance(
    captureQueueA,
    captureBarracksA,
    12f,
    captureReady);
Expect(readyBeforeCaptureAdvance.Success, "03E Ready fixture did not advance.");
Expect(captureReady.Count == 1, "03E Ready fixture did not complete.");
var preservedReady = captureReady[0];

var captureRoutePlanner = new ReinforcementRoutePlanner(
    new RoadNetwork(),
    new RoadMovementProfile(1.4f),
    new RectangularTerrainMovementResolver(new TerrainMovementProfile("Open", 1f)));
var redCaptureSupply = StrategicSupplyResolver.Resolve(
    captureQueueTerritory,
    "red",
    new[] { new SupplySourceDefinition("capture-red-source", "red", "capture-red-sector") });
var captureTransits = new List<ReinforcementTransit>();
var transitBeforeCapture = ReinforcementDispatchService.Dispatch(
    new ReinforcementDispatchRequest(
        "capture-transit-r2",
        "red",
        "capture-red-sector",
        captureBarracksA.Position,
        "capture-other-sector",
        new WorldPoint(30f, 0f),
        2f),
    captureQueueTerritory,
    redCaptureSupply,
    captureRoutePlanner,
    captureTransits);
Expect(transitBeforeCapture.Success, "03E EnRoute fixture did not dispatch.");
Expect(transitBeforeCapture.Transit!.State == ReinforcementState.EnRoute, "03E EnRoute fixture was not moving.");

Expect(EnqueueCaptureOrder("capture-order-a", "capture-reinforcement-a", captureBarracksA.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(30f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E active order did not enqueue.");
Expect(EnqueueCaptureOrder("capture-order-b", "capture-reinforcement-b", captureBarracksA.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(30f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E waiting order B did not enqueue.");
Expect(EnqueueCaptureOrder("capture-order-c", "capture-reinforcement-c", captureBarracksA.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(30f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E waiting order C did not enqueue.");
var partialAdvance = ProductionAdvanceService.Advance(captureQueueA, captureBarracksA, 11f, captureReady);
Expect(partialAdvance.Success, "03E nearly-complete active order did not advance.");
Expect(captureQueueA.CurrentOrder!.ProgressSeconds == 11f, "03E active order progress fixture was incorrect.");
Expect(EnqueueCaptureOrder("capture-order-d", "capture-reinforcement-d", captureBarracksB.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(30f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E second facility order D did not enqueue.");
Expect(EnqueueCaptureOrder("capture-order-e", "capture-reinforcement-e", captureBarracksB.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(30f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E second facility order E did not enqueue.");
Expect(EnqueueCaptureOrder("capture-other-order", "capture-other-reinforcement", captureOtherBarracks.BuildingId, "red", redCaptureEconomy, "capture-other-sector", new WorldPoint(32f, 0f), captureQueues, captureBuildings, captureReady).Success, "03E other-sector order did not enqueue.");
var captureQueueB = captureQueues[1];
var captureOtherQueue = captureQueues[2];
var redBalanceBeforeCapture = redCaptureEconomy.Balance;
Expect(redBalanceBeforeCapture == 720, "03E Red economy fixture balance was incorrect.");

var captureTransfer = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    captureQueueTerritory,
    "capture-red-anchor",
    "blue",
    captureBuildings);
Expect(captureTransfer.CaptureChanged, "03E Sector capture did not change ownership.");
Expect(captureTransfer.TransferredBuildingCount == 4, "03E capture did not transfer every target-sector building.");
Expect(captureBarracksA.OwnerId == "blue", "03E first Barracks owner did not transfer.");
Expect(captureBarracksB.OwnerId == "blue", "03E second Barracks owner did not transfer.");
Expect(captureEmptyBarracks.OwnerId == "blue", "03E empty Barracks owner did not transfer.");
Expect(captureDepot.OwnerId == "blue", "03E non-production Building owner did not transfer.");
Expect(captureOtherBarracks.OwnerId == "red", "03E capture changed another Sector building.");

var captureResolution = ProductionFacilityCaptureResolutionService.Resolve(
    captureTransfer,
    captureBuildings,
    captureProfiles,
    captureQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(captureResolution.Success, "03E queue capture resolution failed.");
Expect(captureResolution.CaptureChanged, "03E resolution lost the capture change.");
Expect(captureResolution.PreviousOwnerId == "red", "03E resolution lost the previous owner.");
Expect(captureResolution.NewOwnerId == "blue", "03E resolution lost the new owner.");
Expect(captureResolution.Facilities.Count == 4, "03E resolution did not report every transferred building.");
Expect(captureResolution.ProductionFacilityCount == 3, "03E production facility detection count was incorrect.");
Expect(captureResolution.CancelledOrderCount == 5, "03E did not cancel active and waiting orders across facilities.");
Expect(captureQueueA.Orders.Count == 0, "03E first facility queue was not emptied atomically.");
Expect(captureQueueB.Orders.Count == 0, "03E second facility queue was not emptied.");
Expect(captureOtherQueue.Orders.Count == 1, "03E changed another Sector queue.");
Expect(captureOtherQueue.CurrentOrder!.OrderId == "capture-other-order", "03E replaced another Sector order.");
Expect(redCaptureEconomy.Balance == redBalanceBeforeCapture, "03E capture refunded the previous owner.");

CapturedFacilityQueueResolution? barracksAResolution = null;
CapturedFacilityQueueResolution? barracksBResolution = null;
CapturedFacilityQueueResolution? emptyBarracksResolution = null;
CapturedFacilityQueueResolution? depotResolution = null;
foreach (var facilityResolution in captureResolution.Facilities)
{
    if (facilityResolution.CapturedFacilityId == captureBarracksA.BuildingId)
    {
        barracksAResolution = facilityResolution;
    }
    else if (facilityResolution.CapturedFacilityId == captureBarracksB.BuildingId)
    {
        barracksBResolution = facilityResolution;
    }
    else if (facilityResolution.CapturedFacilityId == captureEmptyBarracks.BuildingId)
    {
        emptyBarracksResolution = facilityResolution;
    }
    else if (facilityResolution.CapturedFacilityId == captureDepot.BuildingId)
    {
        depotResolution = facilityResolution;
    }
}

Expect(barracksAResolution != null, "03E first Barracks resolution was missing.");
Expect(barracksAResolution!.WasProductionFacility, "03E first Barracks was not detected as production-capable.");
Expect(barracksAResolution.ResolutionApplied, "03E first Barracks policy was not applied.");
Expect(barracksAResolution.CancelledOrderCount == 3, "03E first Barracks did not cancel FIFO A/B/C.");
Expect(barracksAResolution.CancelledOrderIds[0] == "capture-order-a", "03E active order id was not reported first.");
Expect(barracksAResolution.CancelledOrderIds[1] == "capture-order-b", "03E waiting order B id was not reported.");
Expect(barracksAResolution.CancelledOrderIds[2] == "capture-order-c", "03E waiting order C id was not reported.");
Expect(barracksBResolution != null && barracksBResolution.CancelledOrderCount == 2, "03E second Barracks queue was not independently resolved.");
Expect(emptyBarracksResolution != null && emptyBarracksResolution.WasProductionFacility, "03E empty production facility was not recognized.");
Expect(emptyBarracksResolution!.CancelledOrderCount == 0, "03E empty production facility reported cancelled orders.");
Expect(depotResolution != null && !depotResolution.WasProductionFacility, "03E Depot was treated as a production facility.");
Expect(!depotResolution!.ResolutionApplied, "03E applied queue policy to a non-production Building.");
Expect(depotResolution.CancelledOrderCount == 0, "03E non-production Building cancelled an order.");

var readyCountAfterCapture = captureReady.Count;
var advanceAfterCapture = ProductionAdvanceService.Advance(captureQueueA, captureBarracksA, 100f, captureReady);
Expect(advanceAfterCapture.Success, "03E empty captured queue failed a later Advance.");
Expect(advanceAfterCapture.AppliedSeconds == 0f, "03E cancelled progress survived capture.");
Expect(captureReady.Count == readyCountAfterCapture, "03E cancelled nearly-complete order created a Ready reinforcement.");
Expect(captureReady.Count == 1, "03E capture deleted or created Ready reinforcement unexpectedly.");
Expect(ReferenceEquals(captureReady[0], preservedReady), "03E capture replaced the Ready object.");
Expect(captureReady[0].ReinforcementId == "capture-ready-r1", "03E capture changed the Ready id.");
Expect(captureReady[0].FactionId == "red", "03E capture transferred Ready ownership.");
Expect(transitBeforeCapture.Transit.State == ReinforcementState.EnRoute, "03E capture deleted an existing transit.");
Expect(transitBeforeCapture.Transit.FactionId == "red", "03E capture transferred an existing transit.");
Expect(captureTransits.Count == 1, "03E capture changed the transit collection.");

var blueCaptureSupply = StrategicSupplyResolver.Resolve(
    captureQueueTerritory,
    "blue",
    new[] { new SupplySourceDefinition("capture-blue-source", "blue", "capture-red-sector") });
var blockedPreservedReadyDispatch = ProductionReinforcementIntegrationService.Dispatch(
    preservedReady,
    captureBuildings,
    captureQueueTerritory,
    blueCaptureSupply,
    captureRoutePlanner,
    captureReady,
    captureTransits);
Expect(!blockedPreservedReadyDispatch.Success, "03E old-owner Ready dispatched from a captured facility.");
Expect(blockedPreservedReadyDispatch.FailureReason == ProductionReinforcementIntegrationFailureReason.SourceOwnershipConflict, "03E captured-source Ready returned the wrong failure.");
Expect(captureReady.Count == 1 && ReferenceEquals(captureReady[0], preservedReady), "03E failed Ready dispatch did not preserve the object.");

var blueCaptureEconomy = new EconomyState("blue", 200);
var blueBalanceBeforeReuseChecks = blueCaptureEconomy.Balance;
var reusedCancelledOrderId = EnqueueCaptureOrder(
    "capture-order-a",
    "capture-blue-reinforcement-reuse-check",
    captureBarracksA.BuildingId,
    "blue",
    blueCaptureEconomy,
    "capture-blue-sector",
    new WorldPoint(0f, 0f),
    captureQueues,
    captureBuildings,
    Array.Empty<ReadyReinforcement>());
Expect(!reusedCancelledOrderId.Success, "03E changed the existing Queue order-id reuse policy.");
Expect(reusedCancelledOrderId.FailureReason == ProductionEnqueueFailureReason.DuplicateOrderId, "03E cancelled order-id reuse returned the wrong failure.");
var reusedCancelledReinforcementId = EnqueueCaptureOrder(
    "capture-blue-order-reuse-check",
    "capture-reinforcement-a",
    captureBarracksA.BuildingId,
    "blue",
    blueCaptureEconomy,
    "capture-blue-sector",
    new WorldPoint(0f, 0f),
    captureQueues,
    captureBuildings,
    Array.Empty<ReadyReinforcement>());
Expect(!reusedCancelledReinforcementId.Success, "03E changed the existing Queue reinforcement-id reuse policy.");
Expect(reusedCancelledReinforcementId.FailureReason == ProductionEnqueueFailureReason.DuplicateReinforcementId, "03E cancelled reinforcement-id reuse returned the wrong failure.");
Expect(blueCaptureEconomy.Balance == blueBalanceBeforeReuseChecks, "03E id reuse validation changed the new owner economy.");
Expect(captureQueueA.Orders.Count == 0, "03E id reuse validation repopulated the cancelled queue.");
var blueFreshProduction = EnqueueCaptureOrder(
    "capture-blue-order",
    "capture-blue-reinforcement",
    captureBarracksA.BuildingId,
    "blue",
    blueCaptureEconomy,
    "capture-blue-sector",
    new WorldPoint(0f, 0f),
    captureQueues,
    captureBuildings,
    Array.Empty<ReadyReinforcement>());
Expect(blueFreshProduction.Success, "03E new owner could not start fresh production.");
Expect(blueCaptureEconomy.Balance == 160, "03E new owner production did not charge the normal cost.");
Expect(captureQueueA.Orders.Count == 1, "03E new owner did not receive a fresh queue order.");
Expect(captureQueueA.CurrentOrder!.RequestedByFactionId == "blue", "03E fresh queue owner meaning was incorrect.");
Expect(captureQueueA.CurrentOrder.OrderId == "capture-blue-order", "03E old Queue transferred into the fresh queue.");
var blueReadyAfterCapture = new List<ReadyReinforcement>();
var blueAdvanceAfterCapture = ProductionAdvanceService.Advance(
    captureQueueA,
    captureBarracksA,
    12f,
    blueReadyAfterCapture);
Expect(blueAdvanceAfterCapture.Success, "03E new owner production did not advance.");
Expect(blueReadyAfterCapture.Count == 1, "03E new owner production did not create Ready reinforcement.");
Expect(blueReadyAfterCapture[0].FactionId == "blue", "03E new owner production created the wrong faction.");
Expect(blueReadyAfterCapture[0].UnitTypeId == "RifleSquad", "03E new owner production changed the unit type.");

var blueThreat = new RouteThreatSource(
    "capture-blue-route-threat",
    "red",
    new WorldPoint(5f, 0f),
    3f,
    RouteThreatKind.MobileUnit,
    1f);
var bluePostCaptureDispatch = ProductionReinforcementIntegrationService.Dispatch(
    blueReadyAfterCapture[0],
    captureBuildings,
    captureQueueTerritory,
    blueCaptureSupply,
    captureRoutePlanner,
    new[] { blueThreat },
    new RoutePlanningProfile(1f, 1f),
    blueReadyAfterCapture,
    captureTransits);
Expect(bluePostCaptureDispatch.Success, "03E fresh production did not integrate with 03C/03D dispatch.");
Expect(bluePostCaptureDispatch.Transit!.FactionId == "blue", "03E integrated transit had the wrong faction.");
Expect(bluePostCaptureDispatch.Transit.State == ReinforcementState.EnRoute, "03E integrated reinforcement did not enter EnRoute.");
Expect(bluePostCaptureDispatch.Transit.RouteThreatAssessment.HasThreat, "03E integrated dispatch bypassed 03D threat assessment.");
Expect(blueReadyAfterCapture.Count == 0, "03E successful integrated dispatch left Ready behind.");

var sameOwnerQueueOrder = EnqueueCaptureOrder(
    "capture-blue-hold-order",
    "capture-blue-hold-reinforcement",
    captureBarracksA.BuildingId,
    "blue",
    blueCaptureEconomy,
    "capture-blue-sector",
    new WorldPoint(0f, 0f),
    captureQueues,
    captureBuildings,
    Array.Empty<ReadyReinforcement>());
Expect(sameOwnerQueueOrder.Success, "03E same-owner no-op fixture did not enqueue.");
var queueCountBeforeSameOwnerCapture = captureQueueA.Orders.Count;
var sameOwnerTransfer03e = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    captureQueueTerritory,
    "capture-red-anchor",
    "blue",
    captureBuildings);
var sameOwnerResolution = ProductionFacilityCaptureResolutionService.Resolve(
    sameOwnerTransfer03e,
    captureBuildings,
    captureProfiles,
    captureQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(!sameOwnerTransfer03e.CaptureChanged, "03E same-owner capture unexpectedly changed ownership.");
Expect(sameOwnerResolution.Success && !sameOwnerResolution.CaptureChanged, "03E unchanged capture did not return a successful no-op.");
Expect(sameOwnerResolution.Facilities.Count == 0, "03E unchanged capture reported facility resolutions.");
Expect(captureQueueA.Orders.Count == queueCountBeforeSameOwnerCapture, "03E unchanged capture modified the queue.");

var inconsistentCapture = new SectorBuildingTransferResult(
    "capture-red-sector",
    "red",
    "green",
    true,
    new[] { captureBarracksA.BuildingId });
var queueCountBeforeInconsistentCapture = captureQueueA.Orders.Count;
var inconsistentResolution = ProductionFacilityCaptureResolutionService.Resolve(
    inconsistentCapture,
    captureBuildings,
    captureProfiles,
    captureQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(!inconsistentResolution.Success, "03E accepted an inconsistent capture result.");
Expect(inconsistentResolution.FailureReason == ProductionFacilityCaptureResolutionFailureReason.InconsistentCaptureResult, "03E inconsistent capture returned the wrong failure.");
Expect(captureQueueA.Orders.Count == queueCountBeforeInconsistentCapture, "03E inconsistent capture modified a queue.");

var mixedOwnerCapture = new SectorBuildingTransferResult(
    "capture-red-sector",
    "red",
    "blue",
    true,
    new[] { captureBarracksA.BuildingId });
var mixedOwnerResolution = ProductionFacilityCaptureResolutionService.Resolve(
    mixedOwnerCapture,
    captureBuildings,
    captureProfiles,
    captureQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(!mixedOwnerResolution.Success, "03E accepted a contradictory queue owner.");
Expect(mixedOwnerResolution.FailureReason == ProductionFacilityCaptureResolutionFailureReason.InconsistentQueueLinkage, "03E contradictory queue returned the wrong failure.");
Expect(captureQueueA.Orders.Count == queueCountBeforeInconsistentCapture, "03E contradictory queue linkage partially cancelled orders.");

var unsupportedPolicyResolution = ProductionFacilityCaptureResolutionService.Resolve(
    sameOwnerTransfer03e,
    captureBuildings,
    captureProfiles,
    captureQueues,
    (ProductionCaptureQueuePolicy)999);
Expect(!unsupportedPolicyResolution.Success, "03E accepted an unsupported capture queue policy.");
Expect(unsupportedPolicyResolution.FailureReason == ProductionFacilityCaptureResolutionFailureReason.UnsupportedPolicy, "03E unsupported policy returned the wrong failure.");
Expect(captureQueueA.Orders.Count == queueCountBeforeInconsistentCapture, "03E unsupported policy modified a queue.");

var atomicBarracksA = new BuildingState(
    "capture-atomic-a",
    "Barracks",
    "red",
    "capture-red-sector",
    new WorldPoint(18f, -3f),
    2f);
var atomicBarracksB = new BuildingState(
    "capture-atomic-b",
    "Barracks",
    "blue",
    "capture-red-sector",
    new WorldPoint(18f, 3f),
    2f);
var atomicBuildings = new[] { atomicBarracksA, atomicBarracksB };
var atomicQueues = new List<ProductionQueue>();
var atomicRedEconomy = new EconomyState("red", 100);
var atomicBlueEconomy = new EconomyState("blue", 100);
Expect(EnqueueCaptureOrder("capture-atomic-red-order", "capture-atomic-red-reinforcement", atomicBarracksA.BuildingId, "red", atomicRedEconomy, "capture-other-sector", new WorldPoint(30f, 0f), atomicQueues, atomicBuildings, Array.Empty<ReadyReinforcement>()).Success, "03E atomic valid queue fixture did not enqueue.");
Expect(EnqueueCaptureOrder("capture-atomic-blue-order", "capture-atomic-blue-reinforcement", atomicBarracksB.BuildingId, "blue", atomicBlueEconomy, "capture-blue-sector", new WorldPoint(0f, 0f), atomicQueues, atomicBuildings, Array.Empty<ReadyReinforcement>()).Success, "03E atomic contradictory queue fixture did not enqueue.");
atomicBarracksA.TransferOwnershipTo("blue");
var atomicCaptureResult = new SectorBuildingTransferResult(
    "capture-red-sector",
    "red",
    "blue",
    true,
    new[] { atomicBarracksA.BuildingId, atomicBarracksB.BuildingId });
var atomicFailure = ProductionFacilityCaptureResolutionService.Resolve(
    atomicCaptureResult,
    atomicBuildings,
    captureProfiles,
    atomicQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(!atomicFailure.Success, "03E multi-facility prevalidation accepted contradictory linkage.");
Expect(atomicFailure.FailureReason == ProductionFacilityCaptureResolutionFailureReason.InconsistentQueueLinkage, "03E multi-facility prevalidation returned the wrong failure.");
Expect(atomicQueues[0].Orders.Count == 1, "03E multi-facility prevalidation partially cancelled the valid first queue.");
Expect(atomicQueues[0].CurrentOrder!.OrderId == "capture-atomic-red-order", "03E multi-facility prevalidation replaced the valid first order.");
Expect(atomicQueues[1].Orders.Count == 1, "03E multi-facility prevalidation changed the contradictory second queue.");
ExpectThrows<ArgumentNullException>(
    () => ProductionFacilityCaptureResolutionService.Resolve(null!, captureBuildings, captureProfiles, captureQueues, ProductionCaptureQueuePolicy.CancelWithoutRefund),
    "03E accepted a null capture result.");
ExpectThrows<ArgumentNullException>(
    () => ProductionFacilityCaptureResolutionService.Resolve(captureTransfer, null!, captureProfiles, captureQueues, ProductionCaptureQueuePolicy.CancelWithoutRefund),
    "03E accepted null completed buildings.");
ExpectThrows<ArgumentNullException>(
    () => ProductionFacilityCaptureResolutionService.Resolve(captureTransfer, captureBuildings, null!, captureQueues, ProductionCaptureQueuePolicy.CancelWithoutRefund),
    "03E accepted null facility profiles.");
ExpectThrows<ArgumentNullException>(
    () => ProductionFacilityCaptureResolutionService.Resolve(captureTransfer, captureBuildings, captureProfiles, null!, ProductionCaptureQueuePolicy.CancelWithoutRefund),
    "03E accepted null production queues.");

var researchA = new ResearchDefinition(
    "basic-logistics",
    50,
    10f,
    Array.Empty<string>());
var researchB = new ResearchDefinition(
    "road-maintenance",
    70,
    12f,
    new[] { researchA.ResearchId });
var researchC = new ResearchDefinition(
    "reinforcement-organization",
    80,
    14f,
    new[] { researchA.ResearchId });
var researchD = new ResearchDefinition(
    "frontline-operations",
    120,
    18f,
    new[] { researchB.ResearchId, researchC.ResearchId });
Expect(researchA.ResearchId == "basic-logistics", "04A ResearchDefinition lost its id.");
Expect(researchA.PrototypeCost == 50, "04A ResearchDefinition lost its prototype cost.");
Expect(researchA.ResearchSeconds == 10f, "04A ResearchDefinition lost its duration.");
Expect(researchA.PrerequisiteResearchIds.Count == 0, "04A root research gained prerequisites.");
Expect(researchD.PrerequisiteResearchIds.Count == 2, "04A multi-prerequisite research lost a prerequisite.");
var mutablePrerequisites = new List<string> { researchA.ResearchId };
var immutableDefinition = new ResearchDefinition(
    "immutable-research",
    0,
    1f,
    mutablePrerequisites);
mutablePrerequisites.Clear();
Expect(immutableDefinition.PrerequisiteResearchIds.Count == 1, "04A ResearchDefinition prerequisites changed with the source collection.");
ExpectThrows<ArgumentException>(
    () => new ResearchDefinition("", 0, 1f, Array.Empty<string>()),
    "04A accepted a blank research id.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ResearchDefinition("negative-cost", -1, 1f, Array.Empty<string>()),
    "04A accepted a negative research cost.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ResearchDefinition("zero-seconds", 0, 0f, Array.Empty<string>()),
    "04A accepted zero research seconds.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ResearchDefinition("negative-seconds", 0, -1f, Array.Empty<string>()),
    "04A accepted negative research seconds.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ResearchDefinition("nan-seconds", 0, float.NaN, Array.Empty<string>()),
    "04A accepted NaN research seconds.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => new ResearchDefinition("infinite-seconds", 0, float.PositiveInfinity, Array.Empty<string>()),
    "04A accepted infinite research seconds.");
ExpectThrows<ArgumentNullException>(
    () => new ResearchDefinition("null-prerequisites", 0, 1f, null!),
    "04A accepted null prerequisites.");
ExpectThrows<ArgumentException>(
    () => new ResearchDefinition("blank-prerequisite", 0, 1f, new[] { " " }),
    "04A accepted a blank prerequisite id.");
ExpectThrows<ArgumentException>(
    () => new ResearchDefinition("duplicate-prerequisite", 0, 1f, new[] { "root", "root" }),
    "04A accepted a duplicate prerequisite id.");
ExpectThrows<ArgumentException>(
    () => new ResearchDefinition("self-prerequisite", 0, 1f, new[] { "self-prerequisite" }),
    "04A accepted a self prerequisite.");

var researchCatalog = new ResearchCatalog(new[] { researchD, researchB, researchA, researchC });
Expect(researchCatalog.Definitions.Count == 4, "04A ResearchCatalog lost a definition.");
Expect(researchCatalog.Definitions[0].ResearchId == "basic-logistics", "04A ResearchCatalog enumeration was not deterministic.");
Expect(researchCatalog.Contains(researchC.ResearchId), "04A ResearchCatalog did not contain a registered id.");
Expect(ReferenceEquals(researchCatalog.GetRequired(researchB.ResearchId), researchB), "04A ResearchCatalog lookup replaced a definition.");
Expect(researchCatalog.TryGet(researchD.ResearchId, out var lookedUpResearch) && ReferenceEquals(lookedUpResearch, researchD), "04A ResearchCatalog TryGet failed for a registered id.");
Expect(!researchCatalog.TryGet("unknown-research", out var missingResearch) && missingResearch == null, "04A ResearchCatalog found an unknown id.");
ExpectThrows<KeyNotFoundException>(
    () => researchCatalog.GetRequired("unknown-research"),
    "04A ResearchCatalog GetRequired accepted an unknown id.");
ExpectThrows<ArgumentException>(
    () => new ResearchCatalog(new[] { researchA, researchA }),
    "04A ResearchCatalog accepted duplicate research ids.");
ExpectThrows<ArgumentException>(
    () => new ResearchCatalog(new[]
    {
        new ResearchDefinition("unknown-prerequisite-owner", 0, 1f, new[] { "missing-prerequisite" })
    }),
    "04A ResearchCatalog accepted an unknown prerequisite.");
ExpectThrows<ArgumentException>(
    () => new ResearchCatalog(new[]
    {
        new ResearchDefinition("cycle-two-a", 0, 1f, new[] { "cycle-two-b" }),
        new ResearchDefinition("cycle-two-b", 0, 1f, new[] { "cycle-two-a" })
    }),
    "04A ResearchCatalog accepted a two-node cycle.");
ExpectThrows<ArgumentException>(
    () => new ResearchCatalog(new[]
    {
        new ResearchDefinition("cycle-three-a", 0, 1f, new[] { "cycle-three-b" }),
        new ResearchDefinition("cycle-three-b", 0, 1f, new[] { "cycle-three-c" }),
        new ResearchDefinition("cycle-three-c", 0, 1f, new[] { "cycle-three-a" })
    }),
    "04A ResearchCatalog accepted a three-node cycle.");
var emptyResearchCatalog = new ResearchCatalog(Array.Empty<ResearchDefinition>());
Expect(emptyResearchCatalog.Definitions.Count == 0, "04A empty ResearchCatalog was not empty.");

var blueResearchState = new FactionResearchState("blue");
var redResearchState = new FactionResearchState("red");
Expect(blueResearchState.FactionId == "blue", "04A FactionResearchState lost the faction id.");
Expect(blueResearchState.CompletedResearchIds.Count == 0, "04A new research state contained completed research.");
Expect(blueResearchState.ActiveResearch == null, "04A new research state contained active research.");
Expect(!blueResearchState.IsCompleted(researchA.ResearchId), "04A new research state reported a completed research.");
ExpectThrows<ArgumentException>(
    () => new FactionResearchState(" "),
    "04A accepted a blank research faction id.");
ExpectThrows<ArgumentException>(
    () => blueResearchState.IsCompleted(""),
    "04A IsCompleted accepted a blank research id.");

Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchA.ResearchId) == ResearchAvailability.Available, "04A root research was not available.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchB.ResearchId) == ResearchAvailability.Locked, "04A unmet branch research was not locked.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchC.ResearchId) == ResearchAvailability.Locked, "04A second unmet branch research was not locked.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchD.ResearchId) == ResearchAvailability.Locked, "04A multi-prerequisite research was not locked.");
ExpectThrows<KeyNotFoundException>(
    () => ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, "unknown-research"),
    "04A availability accepted an unknown research.");

var blueResearchEconomy = new EconomyState("blue", 500);
var lockedStartBalance = blueResearchEconomy.Balance;
var lockedResearchStart = ResearchService.Start(
    researchB.ResearchId,
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(!lockedResearchStart.Success, "04A started research with an unmet prerequisite.");
Expect(lockedResearchStart.FailureReason == ResearchStartFailureReason.PrerequisiteMissing, "04A locked research returned the wrong start failure.");
Expect(blueResearchEconomy.Balance == lockedStartBalance, "04A locked research changed economy balance.");
Expect(blueResearchState.ActiveResearch == null, "04A locked research changed faction state.");

var insufficientResearchState = new FactionResearchState("blue");
var insufficientResearchEconomy = new EconomyState("blue", 49);
var insufficientResearchStart = ResearchService.Start(
    researchA.ResearchId,
    researchCatalog,
    insufficientResearchState,
    insufficientResearchEconomy);
Expect(!insufficientResearchStart.Success, "04A started research without sufficient funds.");
Expect(insufficientResearchStart.FailureReason == ResearchStartFailureReason.InsufficientFunds, "04A insufficient funds returned the wrong failure.");
Expect(insufficientResearchEconomy.Balance == 49, "04A insufficient funds changed economy balance.");
Expect(insufficientResearchState.ActiveResearch == null, "04A insufficient funds changed research state.");

var mismatchResearchEconomy = new EconomyState("red", 500);
var mismatchResearchStart = ResearchService.Start(
    researchA.ResearchId,
    researchCatalog,
    blueResearchState,
    mismatchResearchEconomy);
Expect(!mismatchResearchStart.Success, "04A accepted mismatched research and economy factions.");
Expect(mismatchResearchStart.FailureReason == ResearchStartFailureReason.FactionMismatch, "04A faction mismatch returned the wrong failure.");
Expect(mismatchResearchEconomy.Balance == 500 && blueResearchState.ActiveResearch == null, "04A faction mismatch changed state.");

var unknownResearchBalance = blueResearchEconomy.Balance;
var unknownResearchStart = ResearchService.Start(
    "unknown-research",
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(!unknownResearchStart.Success, "04A started unknown research.");
Expect(unknownResearchStart.FailureReason == ResearchStartFailureReason.UnknownResearch, "04A unknown research returned the wrong failure.");
Expect(blueResearchEconomy.Balance == unknownResearchBalance && blueResearchState.ActiveResearch == null, "04A unknown research changed state.");

var startResearchA = ResearchService.Start(
    researchA.ResearchId,
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(startResearchA.Success, "04A root research did not start.");
Expect(startResearchA.PreviousBalance == 500 && startResearchA.NewBalance == 450, "04A research start balance result was incorrect.");
Expect(blueResearchEconomy.Balance == 450, "04A research start did not charge the prototype cost.");
Expect(ReferenceEquals(startResearchA.ActiveResearch, blueResearchState.ActiveResearch), "04A research start returned another progress object.");
Expect(blueResearchState.ActiveResearch!.ResearchId == researchA.ResearchId, "04A active research id was incorrect.");
Expect(blueResearchState.ActiveResearch.ProgressSeconds == 0f, "04A active research began with progress.");
Expect(blueResearchState.ActiveResearch.RequiredSeconds == 10f, "04A active research lost its required duration.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchA.ResearchId) == ResearchAvailability.Active, "04A active research availability was incorrect.");

var balanceBeforeDuplicateStart = blueResearchEconomy.Balance;
var duplicateActiveStart = ResearchService.Start(
    researchA.ResearchId,
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(!duplicateActiveStart.Success, "04A started the same active research twice.");
Expect(duplicateActiveStart.FailureReason == ResearchStartFailureReason.AlreadyActive, "04A duplicate active start returned the wrong failure.");
Expect(blueResearchEconomy.Balance == balanceBeforeDuplicateStart, "04A duplicate active start charged twice.");
Expect(blueResearchState.ActiveResearch!.ProgressSeconds == 0f, "04A duplicate active start changed progress.");

var anotherActiveStart = ResearchService.Start(
    researchB.ResearchId,
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(!anotherActiveStart.Success, "04A allowed a second simultaneous research.");
Expect(anotherActiveStart.FailureReason == ResearchStartFailureReason.AnotherResearchActive, "04A one-active trial rule returned the wrong failure.");
Expect(blueResearchEconomy.Balance == balanceBeforeDuplicateStart, "04A second simultaneous research changed balance.");
Expect(blueResearchState.ActiveResearch!.ResearchId == researchA.ResearchId, "04A second simultaneous research replaced the active research.");

ExpectThrows<ArgumentOutOfRangeException>(
    () => ResearchService.Advance(blueResearchState, -1f),
    "04A accepted negative research delta time.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => ResearchService.Advance(blueResearchState, float.NaN),
    "04A accepted NaN research delta time.");
ExpectThrows<ArgumentOutOfRangeException>(
    () => ResearchService.Advance(blueResearchState, float.PositiveInfinity),
    "04A accepted infinite research delta time.");
var zeroResearchAdvance = ResearchService.Advance(blueResearchState, 0f);
Expect(zeroResearchAdvance.Success && zeroResearchAdvance.AppliedSeconds == 0f, "04A zero research advance was not a no-op success.");
var researchAdvanceFour = ResearchService.Advance(blueResearchState, 4f);
Expect(researchAdvanceFour.Success && !researchAdvanceFour.Completed, "04A partial research advance completed too early.");
Expect(researchAdvanceFour.AppliedSeconds == 4f && researchAdvanceFour.UnusedSeconds == 0f, "04A partial research advance accounting was incorrect.");
Expect(blueResearchState.ActiveResearch!.ProgressSeconds == 4f, "04A first partial research progress was incorrect.");
var researchAdvanceFive = ResearchService.Advance(blueResearchState, 5f);
Expect(researchAdvanceFive.Success && !researchAdvanceFive.Completed, "04A second partial research advance completed too early.");
Expect(blueResearchState.ActiveResearch!.ProgressSeconds == 9f, "04A accumulated research progress was incorrect.");
var researchAdvanceOne = ResearchService.Advance(blueResearchState, 1f);
Expect(researchAdvanceOne.Success && researchAdvanceOne.Completed, "04A exact research duration did not complete.");
Expect(researchAdvanceOne.CompletedResearchId == researchA.ResearchId, "04A completion returned the wrong research id.");
Expect(blueResearchState.ActiveResearch == null, "04A completion did not clear active research.");
Expect(blueResearchState.IsCompleted(researchA.ResearchId), "04A completion was not recorded.");
Expect(blueResearchState.CompletedResearchIds.Count == 1, "04A completion was recorded more than once.");

var noActiveAdvance = ResearchService.Advance(blueResearchState, 3f);
Expect(!noActiveAdvance.Success, "04A advance without active research succeeded.");
Expect(noActiveAdvance.FailureReason == ResearchAdvanceFailureReason.NoActiveResearch, "04A no-active advance returned the wrong failure.");
Expect(noActiveAdvance.AppliedSeconds == 0f && noActiveAdvance.UnusedSeconds == 3f, "04A no-active advance accounting was incorrect.");
var balanceBeforeCompletedRestart = blueResearchEconomy.Balance;
var completedRestart = ResearchService.Start(
    researchA.ResearchId,
    researchCatalog,
    blueResearchState,
    blueResearchEconomy);
Expect(!completedRestart.Success, "04A restarted completed research.");
Expect(completedRestart.FailureReason == ResearchStartFailureReason.AlreadyCompleted, "04A completed restart returned the wrong failure.");
Expect(blueResearchEconomy.Balance == balanceBeforeCompletedRestart, "04A completed restart charged another cost.");

Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchA.ResearchId) == ResearchAvailability.Completed, "04A completed research availability was incorrect.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchB.ResearchId) == ResearchAvailability.Available, "04A first branch did not unlock.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchC.ResearchId) == ResearchAvailability.Available, "04A second branch did not unlock independently.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchD.ResearchId) == ResearchAvailability.Locked, "04A multi-prerequisite research unlocked too early.");

var startResearchB = ResearchService.Start(researchB.ResearchId, researchCatalog, blueResearchState, blueResearchEconomy);
Expect(startResearchB.Success, "04A first branch research did not start.");
var completeResearchB = ResearchService.Advance(blueResearchState, 20f);
Expect(completeResearchB.Success && completeResearchB.CompletedResearchId == researchB.ResearchId, "04A first branch research did not complete.");
Expect(completeResearchB.AppliedSeconds == 12f && completeResearchB.UnusedSeconds == 8f, "04A excess research time was not discarded correctly.");
Expect(blueResearchState.ActiveResearch == null, "04A excess time automatically started another research.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchC.ResearchId) == ResearchAvailability.Available, "04A completing one branch locked the other branch.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchD.ResearchId) == ResearchAvailability.Locked, "04A multi-prerequisite research unlocked with one prerequisite.");

var startResearchC = ResearchService.Start(researchC.ResearchId, researchCatalog, blueResearchState, blueResearchEconomy);
Expect(startResearchC.Success, "04A second branch research did not start.");
var partialResearchC = ResearchService.Advance(blueResearchState, 7f);
Expect(partialResearchC.Success && !partialResearchC.Completed, "04A second branch partial advance completed early.");
var dDuringCStart = ResearchService.Start(researchD.ResearchId, researchCatalog, blueResearchState, blueResearchEconomy);
Expect(!dDuringCStart.Success && dDuringCStart.FailureReason == ResearchStartFailureReason.AnotherResearchActive, "04A allowed multi-prerequisite research during another active research.");
Expect(blueResearchState.ActiveResearch!.ProgressSeconds == 7f, "04A rejected concurrent start changed active progress.");
var completeResearchC = ResearchService.Advance(blueResearchState, 7f);
Expect(completeResearchC.CompletedResearchId == researchC.ResearchId, "04A second branch research did not complete.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, blueResearchState, researchD.ResearchId) == ResearchAvailability.Available, "04A multi-prerequisite research did not unlock after both prerequisites.");

var startResearchD = ResearchService.Start(researchD.ResearchId, researchCatalog, blueResearchState, blueResearchEconomy);
Expect(startResearchD.Success, "04A multi-prerequisite research did not start.");
var completeResearchD = ResearchService.Advance(blueResearchState, 18f);
Expect(completeResearchD.CompletedResearchId == researchD.ResearchId, "04A multi-prerequisite research did not complete.");
Expect(blueResearchState.CompletedResearchIds.Count == 4, "04A completed research set did not contain A/B/C/D exactly once.");
Expect(blueResearchState.IsCompleted(researchA.ResearchId) && blueResearchState.IsCompleted(researchB.ResearchId) && blueResearchState.IsCompleted(researchC.ResearchId) && blueResearchState.IsCompleted(researchD.ResearchId), "04A integration flow did not complete every research.");

Expect(redResearchState.CompletedResearchIds.Count == 0 && redResearchState.ActiveResearch == null, "04A Blue research changed Red state.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, redResearchState, researchA.ResearchId) == ResearchAvailability.Available, "04A Red root availability was not independent.");
Expect(ResearchAvailabilityResolver.Resolve(researchCatalog, redResearchState, researchB.ResearchId) == ResearchAvailability.Locked, "04A Blue completion unlocked Red branch research.");
var redResearchEconomy = new EconomyState("red", 100);
Expect(ResearchService.Start(researchA.ResearchId, researchCatalog, redResearchState, redResearchEconomy).Success, "04A Red root research did not start independently.");
Expect(ResearchService.Advance(redResearchState, 10f).CompletedResearchId == researchA.ResearchId, "04A Red root research did not complete independently.");
Expect(redResearchState.IsCompleted(researchA.ResearchId), "04A Red completion was not recorded.");
Expect(blueResearchState.CompletedResearchIds.Count == 4, "04A Red research changed Blue completed research.");

var regressionResearchState = new FactionResearchState("blue");
var regressionEconomy = new EconomyState("blue", 300);
var supplyBeforeResearch = StrategicSupplyResolver.Resolve(
    captureQueueTerritory,
    "blue",
    new[] { new SupplySourceDefinition("research-regression-source", "blue", "capture-red-sector") });
var otherQueueCountBeforeResearch = captureOtherQueue.Orders.Count;
var productionCostBeforeResearch = captureDefinition.PrototypeCost;
Expect(ResearchService.Start(researchA.ResearchId, researchCatalog, regressionResearchState, regressionEconomy).Success, "04A regression research did not start.");
Expect(ResearchService.Advance(regressionResearchState, 10f).Completed, "04A regression research did not complete.");
var supplyAfterResearch = StrategicSupplyResolver.Resolve(
    captureQueueTerritory,
    "blue",
    new[] { new SupplySourceDefinition("research-regression-source", "blue", "capture-red-sector") });
Expect(supplyAfterResearch.GetStatus("capture-blue-sector") == supplyBeforeResearch.GetStatus("capture-blue-sector"), "04A research changed Strategic Supply.");
Expect(captureOtherQueue.Orders.Count == otherQueueCountBeforeResearch, "04A research changed an existing ProductionQueue.");
Expect(captureDefinition.PrototypeCost == productionCostBeforeResearch, "04A research changed ProductionDefinition.");
var incomeAfterResearch = SectorIncomeCollector.Collect(
    captureQueueTerritory,
    new[]
    {
        new SectorIncomeProfile("capture-blue-sector", 10),
        new SectorIncomeProfile("capture-red-sector", 20),
        new SectorIncomeProfile("capture-other-sector", 30)
    },
    regressionEconomy);
Expect(incomeAfterResearch.CollectedAmount == 30, "04A research changed Sector income resolution.");
Expect(regressionEconomy.Balance == 280, "04A research and Sector income balance integration was incorrect.");
var regressionProductionQueues = new List<ProductionQueue>();
var regressionProduction = EnqueueCaptureOrder(
    "research-regression-production-order",
    "research-regression-production-reinforcement",
    captureBarracksA.BuildingId,
    "blue",
    regressionEconomy,
    "capture-blue-sector",
    new WorldPoint(0f, 0f),
    regressionProductionQueues,
    captureBuildings,
    Array.Empty<ReadyReinforcement>());
Expect(regressionProduction.Success, "04A research broke existing Production enqueue.");
Expect(regressionEconomy.Balance == 240, "04A research changed the Production cost path.");

var researchCaptureBuilding = new BuildingState(
    "research-capture-barracks",
    "Barracks",
    "red",
    "capture-other-sector",
    new WorldPoint(34f, 0f),
    2f);
var researchCaptureBuildings = new List<BuildingState> { researchCaptureBuilding };
var researchCaptureQueues = new List<ProductionQueue>();
var researchCaptureEconomy = new EconomyState("red", 100);
Expect(EnqueueCaptureOrder("research-capture-order", "research-capture-reinforcement", researchCaptureBuilding.BuildingId, "red", researchCaptureEconomy, "capture-other-sector", new WorldPoint(32f, 0f), researchCaptureQueues, researchCaptureBuildings, Array.Empty<ReadyReinforcement>()).Success, "04A capture regression order did not enqueue.");
ProductionAdvanceService.Advance(researchCaptureQueues[0], researchCaptureBuilding, 11f, new List<ReadyReinforcement>());
var researchCaptureBalance = researchCaptureEconomy.Balance;
var researchCaptureTransfer = TerritoryBuildingOwnershipService.CaptureAndTransfer(
    captureQueueTerritory,
    "capture-other-anchor",
    "blue",
    researchCaptureBuildings);
var researchCaptureResolution = ProductionFacilityCaptureResolutionService.Resolve(
    researchCaptureTransfer,
    researchCaptureBuildings,
    captureProfiles,
    researchCaptureQueues,
    ProductionCaptureQueuePolicy.CancelWithoutRefund);
Expect(researchCaptureResolution.Success, "04A broke the confirmed production capture resolution.");
Expect(researchCaptureBuilding.OwnerId == "blue", "04A broke captured production facility ownership transfer.");
Expect(researchCaptureQueues[0].Orders.Count == 0, "04A broke confirmed captured Queue cancellation.");
Expect(researchCaptureResolution.CancelledOrderCount == 1, "04A capture regression did not report the cancelled order.");
Expect(researchCaptureEconomy.Balance == researchCaptureBalance, "04A broke confirmed no-refund capture behavior.");
Expect(regressionResearchState.IsCompleted(researchA.ResearchId), "04A production facility capture changed faction research state.");

ExpectThrows<ArgumentNullException>(
    () => new ResearchCatalog(null!),
    "04A accepted null ResearchCatalog definitions.");
ExpectThrows<ArgumentException>(
    () => new ResearchCatalog(new ResearchDefinition[] { researchA, null! }),
    "04A ResearchCatalog accepted a null definition.");
ExpectThrows<ArgumentNullException>(
    () => ResearchAvailabilityResolver.Resolve(null!, blueResearchState, researchA.ResearchId),
    "04A availability accepted a null catalog.");
ExpectThrows<ArgumentNullException>(
    () => ResearchAvailabilityResolver.Resolve(researchCatalog, null!, researchA.ResearchId),
    "04A availability accepted a null faction state.");
ExpectThrows<ArgumentNullException>(
    () => ResearchService.Start(researchA.ResearchId, null!, blueResearchState, blueResearchEconomy),
    "04A start accepted a null catalog.");
ExpectThrows<ArgumentNullException>(
    () => ResearchService.Start(researchA.ResearchId, researchCatalog, null!, blueResearchEconomy),
    "04A start accepted a null faction state.");
ExpectThrows<ArgumentNullException>(
    () => ResearchService.Start(researchA.ResearchId, researchCatalog, blueResearchState, null!),
    "04A start accepted a null economy.");
ExpectThrows<ArgumentNullException>(
    () => ResearchService.Advance(null!, 1f),
    "04A advance accepted a null faction state.");

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

sealed class RectangularTerrainMovementResolver : ITerrainMovementResolver
{
    private readonly TerrainMovementProfile fallback;
    private readonly List<Area> areas = new();

    public RectangularTerrainMovementResolver(TerrainMovementProfile fallback)
    {
        this.fallback = fallback;
    }

    public RectangularTerrainMovementResolver Add(
        TerrainMovementProfile profile,
        float minimumX,
        float maximumX,
        float minimumZ,
        float maximumZ)
    {
        areas.Add(new Area(profile, minimumX, maximumX, minimumZ, maximumZ));
        return this;
    }

    public TerrainMovementProfile Resolve(WorldPoint point)
    {
        foreach (var area in areas)
        {
            if (point.X >= area.MinimumX
                && point.X <= area.MaximumX
                && point.Z >= area.MinimumZ
                && point.Z <= area.MaximumZ)
            {
                return area.Profile;
            }
        }

        return fallback;
    }

    private readonly record struct Area(
        TerrainMovementProfile Profile,
        float MinimumX,
        float MaximumX,
        float MinimumZ,
        float MaximumZ);
}

sealed class ConfigurableRoadPlacementAreaResolver : IRoadPlacementAreaResolver
{
    private IReadOnlyCollection<string> sectorIds = Array.Empty<string>();

    public void Set(params string[] traversedSectorIds)
    {
        sectorIds = Array.AsReadOnly((string[])traversedSectorIds.Clone());
    }

    public IReadOnlyCollection<string> ResolveTraversedSectors(
        WorldPoint start,
        WorldPoint end,
        float width)
    {
        return sectorIds;
    }
}
