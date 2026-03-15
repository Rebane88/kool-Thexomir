using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Services.Auth.DTOs;
using Application.Services.Lobby.DTOs;
using Domain.Game;
using Infrastructure.Seeding.Seeders;
using Microsoft.AspNetCore.Mvc;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Lobby lifecycle integration tests — real HTTP requests through WebApplicationFactory
/// backed by a live PostgreSQL container (Testcontainers).
///
/// Tests the full create -> join -> select faction -> start game flow, plus error cases.
/// Isolation strategy: IntegrationTestBase rolls back the EF transaction after each test.
/// Each test uses unique emails to avoid inter-test collisions (Identity UserManager
/// commits outside the EF transaction scope).
/// </summary>
public class LobbyLifecycleTests : IntegrationTestBase
{
    public LobbyLifecycleTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<(string token, Guid userId)> RegisterAndLoginAsync(string email, string password)
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        login.ShouldNotBeNull();
        return (login.AccessToken, login.UserId);
    }

    private void SetAuth(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearAuth()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    // -------------------------------------------------------------------------
    // Test 1: Full happy-path flow
    // -------------------------------------------------------------------------

    /// <summary>
    /// Full happy-path: register host + player, create lobby, join, select factions, start.
    /// Mirrors FullAuthFlow_RegisterLoginRefreshLogout in coverage depth.
    /// </summary>
    [Fact]
    public async Task FullLobbyFlow_CreateJoinFactionStart()
    {
        var (hostToken, _) = await RegisterAndLoginAsync("host@lobbytest.com", "Host.Test1");
        var (playerToken, _) = await RegisterAndLoginAsync("player2@lobbytest.com", "Player.Test1");

        // --- Create lobby as host ---
        SetAuth(hostToken);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/lobby", new CreateLobbyRequest
        {
            MaxPlayers = 4,
            WinCondition = EWinCondition.Domination
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        created.ShouldNotBeNull();
        created.LobbyId.ShouldNotBe(Guid.Empty);
        created.InviteCode.ShouldNotBeNullOrEmpty();
        created.InviteCode.Length.ShouldBe(6);

        var lobbyId = created.LobbyId;
        var inviteCode = created.InviteCode;

        // --- Player2 joins ---
        SetAuth(playerToken);
        var joinResponse = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = inviteCode
        });
        joinResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var joinedLobby = await joinResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        joinedLobby.ShouldNotBeNull();
        joinedLobby.PlayerCount.ShouldBe(2);

        // --- Verify lobby state via GET ---
        SetAuth(hostToken);
        var getResponse = await Client.GetAsync($"/api/v1/lobby/{lobbyId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var lobbyState = await getResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        lobbyState.ShouldNotBeNull();
        lobbyState.PlayerCount.ShouldBe(2);
        lobbyState.Status.ShouldBe(EGameStatus.Lobby);

        // Obtain available faction IDs from the lobby response
        lobbyState.Factions.Count.ShouldBeGreaterThan(1);
        var factionId1 = lobbyState.Factions[0].FactionTypeId;
        var factionId2 = lobbyState.Factions[1].FactionTypeId;

        // --- Host selects faction ---
        SetAuth(hostToken);
        var hostFactionResponse = await Client.PostAsync(
            $"/api/v1/lobby/{lobbyId}/faction/{factionId1}", null);
        hostFactionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // --- Player2 selects a different faction ---
        SetAuth(playerToken);
        var playerFactionResponse = await Client.PostAsync(
            $"/api/v1/lobby/{lobbyId}/faction/{factionId2}", null);
        playerFactionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // --- Host starts the game ---
        SetAuth(hostToken);
        var startResponse = await Client.PostAsync($"/api/v1/lobby/{lobbyId}/start", null);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // --- Verify lobby is now InProgress ---
        var finalGetResponse = await Client.GetAsync($"/api/v1/lobby/{lobbyId}");
        finalGetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var finalState = await finalGetResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        finalState.ShouldNotBeNull();
        finalState.Status.ShouldBe(EGameStatus.InProgress);

        ClearAuth();
    }

    // -------------------------------------------------------------------------
    // Test 2: Joining a full lobby returns 400
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinLobby_FullLobby_Returns400()
    {
        var (hostToken, _) = await RegisterAndLoginAsync("fullhost@lobbytest.com", "Full.Host1");
        var (player2Token, _) = await RegisterAndLoginAsync("full2@lobbytest.com", "Full.P2T1");
        var (player3Token, _) = await RegisterAndLoginAsync("full3@lobbytest.com", "Full.P3T1");

        // Create a max-2-player lobby
        SetAuth(hostToken);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/lobby", new CreateLobbyRequest
        {
            MaxPlayers = 2,
            WinCondition = EWinCondition.Domination
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        created.ShouldNotBeNull();

        // Player2 joins successfully
        SetAuth(player2Token);
        var join2 = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = created.InviteCode
        });
        join2.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Player3 should be rejected (lobby full)
        SetAuth(player3Token);
        var join3 = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = created.InviteCode
        });
        join3.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await join3.Content.ReadFromJsonAsync<ProblemDetails>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(400);

        ClearAuth();
    }

    // -------------------------------------------------------------------------
    // Test 3: Host leaves — host transfers to remaining player
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LeaveLobby_HostLeaves_TransfersHost()
    {
        var (hostToken, hostUserId) = await RegisterAndLoginAsync("leavehost@lobbytest.com", "Leave.Host1");
        var (player2Token, player2UserId) = await RegisterAndLoginAsync("leave2@lobbytest.com", "Leave.P2T1");

        // Host creates lobby
        SetAuth(hostToken);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/lobby", new CreateLobbyRequest
        {
            MaxPlayers = 4,
            WinCondition = EWinCondition.Domination
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        created.ShouldNotBeNull();
        var lobbyId = created.LobbyId;

        // Player2 joins
        SetAuth(player2Token);
        var joinResponse = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = created.InviteCode
        });
        joinResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Host leaves
        SetAuth(hostToken);
        var leaveResponse = await Client.DeleteAsync($"/api/v1/lobby/{lobbyId}");
        leaveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Player2 checks lobby — should now be the host
        SetAuth(player2Token);
        var getResponse = await Client.GetAsync($"/api/v1/lobby/{lobbyId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var lobbyState = await getResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        lobbyState.ShouldNotBeNull();
        lobbyState.HostUserId.ShouldBe(player2UserId);

        ClearAuth();
    }

    // -------------------------------------------------------------------------
    // Test 4: Non-host cannot start the game
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartGame_NotHost_Returns400()
    {
        var (hostToken, _) = await RegisterAndLoginAsync("starthost@lobbytest.com", "Start.Host1");
        var (player2Token, _) = await RegisterAndLoginAsync("start2@lobbytest.com", "Start.P2T1");

        // Host creates lobby
        SetAuth(hostToken);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/lobby", new CreateLobbyRequest
        {
            MaxPlayers = 4,
            WinCondition = EWinCondition.Domination
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        created.ShouldNotBeNull();
        var lobbyId = created.LobbyId;

        // Player2 joins
        SetAuth(player2Token);
        var joinResponse = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = created.InviteCode
        });
        joinResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Get faction IDs to select
        SetAuth(hostToken);
        var getResponse = await Client.GetAsync($"/api/v1/lobby/{lobbyId}");
        var lobbyState = await getResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        lobbyState.ShouldNotBeNull();
        var factionId1 = lobbyState.Factions[0].FactionTypeId;
        var factionId2 = lobbyState.Factions[1].FactionTypeId;

        // Both players select factions
        SetAuth(hostToken);
        (await Client.PostAsync($"/api/v1/lobby/{lobbyId}/faction/{factionId1}", null))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        SetAuth(player2Token);
        (await Client.PostAsync($"/api/v1/lobby/{lobbyId}/faction/{factionId2}", null))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        // Player2 (non-host) tries to start — should be rejected
        var startResponse = await Client.PostAsync($"/api/v1/lobby/{lobbyId}/start", null);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await startResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(400);

        ClearAuth();
    }

    // -------------------------------------------------------------------------
    // Test 5: Selecting a faction already taken returns 400
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SelectFaction_DuplicateFaction_Returns400()
    {
        var (hostToken, _) = await RegisterAndLoginAsync("fachost@lobbytest.com", "Fac.Host1");
        var (player2Token, _) = await RegisterAndLoginAsync("fac2@lobbytest.com", "Fac.P2T1");

        // Host creates lobby
        SetAuth(hostToken);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/lobby", new CreateLobbyRequest
        {
            MaxPlayers = 4,
            WinCondition = EWinCondition.Domination
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        created.ShouldNotBeNull();
        var lobbyId = created.LobbyId;

        // Player2 joins
        SetAuth(player2Token);
        var joinResponse = await Client.PostAsJsonAsync("/api/v1/lobby/join", new JoinLobbyRequest
        {
            InviteCode = created.InviteCode
        });
        joinResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Get faction IDs
        SetAuth(hostToken);
        var getResponse = await Client.GetAsync($"/api/v1/lobby/{lobbyId}");
        var lobbyState = await getResponse.Content.ReadFromJsonAsync<LobbyResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        lobbyState.ShouldNotBeNull();
        var factionId1 = lobbyState.Factions[0].FactionTypeId;

        // Host selects faction 1
        (await Client.PostAsync($"/api/v1/lobby/{lobbyId}/faction/{factionId1}", null))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        // Player2 tries to select the same faction — should be rejected
        SetAuth(player2Token);
        var dupFactionResponse = await Client.PostAsync($"/api/v1/lobby/{lobbyId}/faction/{factionId1}", null);
        dupFactionResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await dupFactionResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(400);

        ClearAuth();
    }
}
