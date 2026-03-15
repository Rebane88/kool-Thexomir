using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Services.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Auth endpoint integration tests — real HTTP requests through WebApplicationFactory
/// backed by a live PostgreSQL container (Testcontainers).
///
/// Isolation strategy: IntegrationTestBase rolls back the EF transaction after each test.
/// Each test also registers its own unique email to avoid inter-test collisions even if
/// Identity's UserManager commits outside the EF transaction scope.
/// </summary>
public class AuthTests : IntegrationTestBase
{
    public AuthTests(DatabaseFixture fixture) : base(fixture) { }

    /// <summary>
    /// Full happy-path flow: register -> login -> refresh -> logout.
    /// Verifies the complete auth pipeline end-to-end.
    /// </summary>
    [Fact]
    public async Task FullAuthFlow_RegisterLoginRefreshLogout()
    {
        const string email = "flow@authtest.com";
        const string password = "Flow.Test1";

        // --- Register ---
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password
        });

        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        registered.ShouldNotBeNull();
        registered.Email.ShouldBe(email);
        registered.UserId.ShouldNotBe(Guid.Empty);

        // --- Login ---
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        loggedIn.ShouldNotBeNull();
        loggedIn.AccessToken.ShouldNotBeNullOrEmpty();
        loggedIn.RefreshToken.ShouldNotBeNullOrEmpty();

        // --- Refresh ---
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
        {
            AccessToken = loggedIn.AccessToken,
            RefreshToken = loggedIn.RefreshToken
        });

        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        refreshed.ShouldNotBeNull();
        refreshed.AccessToken.ShouldNotBeNullOrEmpty();
        refreshed.RefreshToken.ShouldNotBeNullOrEmpty();

        // --- Logout (requires Bearer token) ---
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        var logoutResponse = await Client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest
        {
            RefreshToken = refreshed.RefreshToken
        });

        // Clear auth header so it doesn't bleed into other tests
        Client.DefaultRequestHeaders.Authorization = null;

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Registering with the same email twice returns 409 Conflict with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_Returns409WithProblemDetails()
    {
        const string email = "dup@authtest.com";
        const string password = "Dup.Test1";

        // First registration — must succeed
        var first = await Client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second registration with same email
        var second = await Client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password
        });

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(409);
        problem.Detail.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Logging in with the wrong password returns 401 Unauthorized with ProblemDetails.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        const string email = "wp@authtest.com";

        // Register first so the user exists
        var register = await Client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Right.Pass1"
        });
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Attempt login with wrong password
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Wrong.Pass1"
        });

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problem = await loginResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            Base.JsonHelpers.JsonSerializerOptionsCamelCase);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(401);
    }

    /// <summary>
    /// Sending garbage tokens to the refresh endpoint returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
        {
            AccessToken = "invalid.jwt.token",
            RefreshToken = "invalid-refresh-token"
        });

        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
