using System.Net;
using System.Net.Http.Json;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — Wave 0 scaffolded integration tests for MVCI18N-03.
///
/// MVCI18N-03: Backend entity names (terrain, faction, building, army type) returned
/// through the game state flow follow the current request culture. Estonian culture
/// yields Estonian names; English culture yields English names.
///
/// IMPORTANT — Pitfall 3 from 39-RESEARCH.md (_HexMap.cshtml TerrainKey split):
/// The SVG hex map computes its fill color from the terrain identity, which today
/// reads `TerrainName`. The moment `TerrainName` becomes localized, a naive
/// consumer that still keys colors by name will produce the wrong fill when the
/// culture is `et`. The fix is to introduce a culture-invariant `TerrainKey`
/// alongside the localized `TerrainName` and migrate color lookups to `TerrainKey`.
/// `HexMapColors_StillCorrect_WhenTerrainNameTranslated` is the regression guard
/// for exactly that bug — it asserts the SVG fill for a known terrain type matches
/// the expected English-keyed color regardless of the active culture.
///
/// Coverage map:
///   MVCI18N-03 (terrain names)     → GameStateSnapshot_WithEtCulture_ReturnsEstonianTerrainNames
///   MVCI18N-03 (faction names)     → GameStateSnapshot_WithEtCulture_ReturnsEstonianFactionNames
///   MVCI18N-03 (building names)    → GameStateSnapshot_WithEtCulture_ReturnsEstonianBuildingNames
///   MVCI18N-03 (army type names)   → GameStateSnapshot_WithEtCulture_ReturnsEstonianArmyTypeNames
///   MVCI18N-03 (hex color guard)   → HexMapColors_StillCorrect_WhenTerrainNameTranslated
///
/// All facts are Wave 0 stubs marked [Fact(Skip = ...)] — bodies are filled in
/// by plan 39-04 once plan 39-01 ships the TerrainKey split and the other plans
/// finish wiring localized entity names.
/// </summary>
public class PublicGameStateLocalizationTests : IntegrationTestBase
{
    public PublicGameStateLocalizationTests(DatabaseFixture fixture) : base(fixture) { }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianTerrainNames()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianFactionNames()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianBuildingNames()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianArmyTypeNames()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task HexMapColors_StillCorrect_WhenTerrainNameTranslated()
    {
        await Task.CompletedTask;
    }
}
