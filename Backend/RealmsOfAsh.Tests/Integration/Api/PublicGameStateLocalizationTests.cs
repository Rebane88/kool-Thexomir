using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.Army;
using Application.Services.Building;
using Application.Services.Building.DTOs.V1;
using Application.Services.GameInitialization;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs.V1;
using Domain.Game;
using Domain.Map;
using Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — integration tests for MVCI18N-03
/// (backend entity names flow through the game state in the active culture,
/// plus the hex map color regression guard from 39-RESEARCH.md Pitfall 3).
///
/// Strategy: tests look up expected Estonian strings dynamically from the live
/// seed data (DB query) so they remain green across seed-data changes. Each test
/// renders /Public/Game/Index/{id} under a request culture cookie and asserts the
/// HTML contains the expected translated entity name.
///
/// The hex color regression guard issues the same game page request under `en`
/// and `et`, extracts all polygon `fill` attributes, and asserts they are
/// byte-identical — proving that the culture swap does not affect SVG colors
/// (because _HexMap.cshtml keys TerrainColor off `TerrainKey`, not `TerrainName`).
/// </summary>
public class PublicGameStateLocalizationTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicGameStateLocalizationTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Terrain names (et)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianTerrainNames()
    {
        var seed = await SeedActiveGameAsync();

        // Pick a deterministic tile to select — any non-castle owned tile works.
        var snapshot = await GetGameStateAsync(seed.gameId);
        var tile = snapshot.Tiles.First();

        // Resolve the expected Estonian terrain name from the DB (culture-agnostic).
        var expectedEt = await GetTerrainEtNameAsync(tile.TerrainTypeId);
        expectedEt.ShouldNotBeNullOrEmpty("Seed data must provide an Estonian translation for every TerrainType");

        // Render the game page under et with the tile selected so _ActionPanel renders
        // the terrain name via @tile.TerrainName (which flows from BuildGameStateSnapshotAsync
        // through the request culture's .Translate()).
        var url = $"/Public/Game/Index/{seed.gameId}?selectedTileId={tile.Id}";
        var html = await GetPageHtmlAsync(url, seed.hostCookie, culture: "et");

        html.ShouldContain(expectedEt!);
    }

    // -------------------------------------------------------------------------
    // Faction names (et)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianFactionNames()
    {
        var seed = await SeedActiveGameAsync();

        // Resolve the expected Estonian faction name for the host's kingdom.
        // (The StandingsPanel renders @k.FactionName for every active kingdom —
        // populated from the DTO's .Translate() in BuildGameStateSnapshotAsync.)
        var snapshot = await GetGameStateAsync(seed.gameId);
        var hostKingdom = snapshot.Kingdoms.First(k => k.UserId == seed.hostUserId);
        hostKingdom.FactionTypeId.ShouldNotBeNull("Host kingdom must have a faction selected");
        var expectedEt = await GetFactionEtNameAsync(hostKingdom.FactionTypeId!.Value);
        expectedEt.ShouldNotBeNullOrEmpty();

        var html = await GetPageHtmlAsync(
            $"/Public/Game/Index/{seed.gameId}", seed.hostCookie, culture: "et");

        html.ShouldContain(expectedEt!);
    }

    // -------------------------------------------------------------------------
    // Building names (et)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianBuildingNames()
    {
        var seed = await SeedActiveGameAsync();

        // Select an empty owned tile so the BuildingPanel lists the available
        // BuildingTypes (each with its translated @bt.Name).
        var tileId = await GetFirstEmptyOwnedTileIdAsync(seed.gameId, seed.hostUserId);

        // Resolve the expected Estonian name for a Tier 1 building type that
        // appears in the BuildingPanel list (Farm → Talu, Barracks → Kasarmud, etc.).
        var expectedEt = await GetAnyTier1BuildingEtNameAsync();
        expectedEt.ShouldNotBeNullOrEmpty();

        var url = $"/Public/Game/Index/{seed.gameId}?selectedTileId={tileId}";
        var html = await GetPageHtmlAsync(url, seed.hostCookie, culture: "et");

        html.ShouldContain(expectedEt!);
    }

    // -------------------------------------------------------------------------
    // Army type names (et)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameStateSnapshot_WithEtCulture_ReturnsEstonianArmyTypeNames()
    {
        var seed = await SeedActiveGameAsync();

        // Place a Tier 1 military building and train an army so the ArmyRoster
        // has at least one row — the row renders @armyType.Name which flows from
        // GetArmyTypesAsync (translates via LangStr.Translate).
        var (tileId, _, armyTypeId) = await SeedMilitaryBuildingAndTrainAsync(seed.gameId, seed.hostUserId);

        var expectedEt = await GetArmyTypeEtNameAsync(armyTypeId);
        expectedEt.ShouldNotBeNullOrEmpty();

        var url = $"/Public/Game/Index/{seed.gameId}?selectedTileId={tileId}";
        var html = await GetPageHtmlAsync(url, seed.hostCookie, culture: "et");

        html.ShouldContain(expectedEt!);
    }

    // -------------------------------------------------------------------------
    // Hex map color regression guard — MVCI18N-03 Pitfall 3
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HexMapColors_StillCorrect_WhenTerrainNameTranslated()
    {
        var seed = await SeedActiveGameAsync();
        var url = $"/Public/Game/Index/{seed.gameId}";

        var htmlEn = await GetPageHtmlAsync(url, seed.hostCookie, culture: "en");
        var htmlEt = await GetPageHtmlAsync(url, seed.hostCookie, culture: "et");

        var fillsEn = ExtractPolygonFills(htmlEn);
        var fillsEt = ExtractPolygonFills(htmlEt);

        fillsEn.Count.ShouldBeGreaterThan(0, "Hex map should render at least one polygon");
        fillsEn.Count.ShouldBe(fillsEt.Count,
            "Hex map must render the same number of polygons regardless of culture");
        fillsEn.ShouldBe(fillsEt,
            "Hex map polygon fill colors must be byte-identical between en and et cultures " +
            "(_HexMap.cshtml keys TerrainColor off the culture-invariant TerrainKey).");
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    /// <summary>
    /// GETs a URL with the given auth cookie and culture cookie, then returns the
    /// HTML-decoded body so tests can match against Unicode strings directly.
    /// Uses a dedicated HttpClient with `HandleCookies = false` so the shared
    /// IntegrationTestBase.Client's cookie jar (which picks up every registered
    /// user's .Thexomir.Public cookie during seeding) cannot shadow the explicit
    /// hostCookie header — mirrors the MVCGAME_12 / MVCGAME_13 pattern.
    /// </summary>
    private async Task<string> GetPageHtmlAsync(string url, string authCookie, string culture)
    {
        using var cleanClient = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddCookies(request, authCookie, culture);
        var response = await cleanClient.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"GET {url} should return 200 (got {response.StatusCode})");
        var raw = await response.Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(raw);
    }

    private static void AddCookies(HttpRequestMessage request, string authCookie, string culture)
    {
        if (!string.IsNullOrEmpty(authCookie))
        {
            request.Headers.Add("Cookie", authCookie);
        }
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        request.Headers.Add(
            "Cookie",
            $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");
    }

    /// <summary>
    /// Extracts every polygon fill color (raw attribute value) from an SVG hex map
    /// HTML response, preserving order so two renders can be compared element-wise.
    /// </summary>
    private static List<string> ExtractPolygonFills(string html)
    {
        var matches = Regex.Matches(html, @"<polygon\s+[^>]*fill=""([^""]+)""");
        return matches.Select(m => m.Groups[1].Value).ToList();
    }

    // =========================================================================
    // DB lookup helpers — resolve expected Estonian names from seed data
    // =========================================================================

    private async Task<string?> GetTerrainEtNameAsync(Guid terrainTypeId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var terrain = await db.TerrainTypes.AsNoTracking().FirstAsync(t => t.Id == terrainTypeId);
        return terrain.Name.Translate("et");
    }

    private async Task<string?> GetFactionEtNameAsync(Guid factionTypeId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var faction = await db.FactionTypes.AsNoTracking().FirstAsync(f => f.Id == factionTypeId);
        return faction.Name.Translate("et");
    }

    /// <summary>
    /// Returns the Estonian name of any Tier 1 BuildingType that would actually
    /// appear in the BuildingPanel list. "Tier 1" is the filter the _BuildingPanel
    /// uses by default, and every Tier 1 type has a seeded et translation.
    /// </summary>
    private async Task<string?> GetAnyTier1BuildingEtNameAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var building = await db.BuildingTypes.AsNoTracking().FirstAsync(b => b.Tier == 1);
        return building.Name.Translate("et");
    }

    private async Task<string?> GetArmyTypeEtNameAsync(Guid armyTypeId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var armyType = await db.ArmyTypes.AsNoTracking().FirstAsync(a => a.Id == armyTypeId);
        return armyType.Name.Translate("et");
    }

    // =========================================================================
    // Game/lobby seeding helpers (abridged copy of PublicGameTests patterns)
    // =========================================================================

    private sealed record GameSeed(Guid gameId, string hostCookie, Guid hostUserId, Guid player2UserId);

    private async Task<GameSeed> SeedActiveGameAsync()
    {
        var (hostCookie, hostUserId) = await RegisterAndGetUserAsync();
        var (_, player2UserId) = await RegisterAndGetUserAsync();

        using var scope = Factory.Services.CreateScope();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
        {
            MaxPlayers = 2,
            WinCondition = EWinCondition.Elimination
        });
        createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
        var lobbyId = createResult.Value!.LobbyId;

        (await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest { InviteCode = createResult.Value!.InviteCode }))
            .IsSuccess.ShouldBeTrue();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
        factions.Count.ShouldBeGreaterThanOrEqualTo(2);
        (await lobbyService.SelectFactionAsync(hostUserId, lobbyId, factions[0].Id)).IsSuccess.ShouldBeTrue();
        (await lobbyService.SelectFactionAsync(player2UserId, lobbyId, factions[1].Id)).IsSuccess.ShouldBeTrue();
        (await lobbyService.StartGameAsync(hostUserId, lobbyId)).IsSuccess.ShouldBeTrue();

        var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var state = await initService.InitializeGameAsync(lobbyId);
        state.CurrentPhase.ShouldBe("Action");
        state.Tiles.Count.ShouldBeGreaterThan(0);

        return new GameSeed(lobbyId, hostCookie, hostUserId, player2UserId);
    }

    private async Task<Application.Services.GameInitialization.DTOs.V1.GameStateDto> GetGameStateAsync(Guid gameId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        return await gameInit.BuildGameStateSnapshotAsync(gameId);
    }

    private async Task<Guid> GetFirstEmptyOwnedTileIdAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);
        var tile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);
        return tile.Id;
    }

    /// <summary>
    /// Places a Tier 1 military building on an empty owned tile, trains one army in it.
    /// Returns (tileId, buildingId, armyTypeId).
    /// </summary>
    private async Task<(Guid tileId, Guid buildingId, Guid armyTypeId)> SeedMilitaryBuildingAndTrainAsync(
        Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
        var armyService = scope.ServiceProvider.GetRequiredService<IArmyService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);
        var emptyOwnedTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);

        var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
        var militaryBuildingType = buildingTypes.First(bt => bt.ArmyCapacity > 0 && bt.Tier == 1);

        var placeResult = await buildingService.PlaceBuildingAsync(gameId, userId, new PlaceBuildingRequest
        {
            TileId = emptyOwnedTile.Id,
            BuildingTypeId = militaryBuildingType.Id
        });
        placeResult.IsSuccess.ShouldBeTrue($"PlaceBuildingAsync failed: {placeResult.Error}");
        var buildingId = placeResult.Value!.BuildingId;

        var armyTypes = (await armyService.GetArmyTypesAsync(gameId, userId)).ToList();
        var armyType = armyTypes.First(at => at.RequiredBuildingTypeId == militaryBuildingType.Id);

        var trainResult = await armyService.TrainArmyAsync(gameId, userId, new Application.Services.Army.DTOs.V1.TrainArmyRequest
        {
            BuildingId = buildingId,
            ArmyTypeId = armyType.Id
        });
        trainResult.IsSuccess.ShouldBeTrue($"TrainArmyAsync failed: {trainResult.Error}");

        return (emptyOwnedTile.Id, buildingId, armyType.Id);
    }

    // =========================================================================
    // Auth / registration helpers
    // =========================================================================

    private async Task<(string authCookie, Guid userId)> RegisterAndGetUserAsync()
    {
        var email = $"phase39-gs-{Guid.NewGuid():N}@test.local";
        const string password = "Player1!";

        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty();

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("ConfirmPassword", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register")
        {
            Content = formContent
        };
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var publicCookie = setCookies.First(c => c.Contains(PublicCookieName));
        var authCookie = publicCookie.Split(';')[0].Trim();

        var userId = await GetUserIdByEmailAsync(email);
        return (authCookie, userId);
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var result = await identity.GetByEmailAsync(email);
        result.IsSuccess.ShouldBeTrue();
        return result.Value!.Id;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;
        match = Regex.Match(html,
            @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
