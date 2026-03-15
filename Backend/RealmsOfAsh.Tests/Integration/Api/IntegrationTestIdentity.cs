using System.Net.Http.Json;
using Application.Services.Auth.DTOs;
using Base;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

public class IntegrationTestIdentity : IntegrationTestBase
{
    public IntegrationTestIdentity(DatabaseFixture fixture) : base(fixture) { }

    [Theory]
    [InlineData("First.Last.1", "first@last.com")]
    public async Task Registration_Flow(string dataPassword, string dataEmail)
    {
        var data = new RegisterRequest
        {
            Password = dataPassword,
            Email = dataEmail,
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", data);

        response.EnsureSuccessStatusCode();

        var registerResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>(
            JsonHelpers.JsonSerializerOptionsCamelCase);
        registerResponse.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("First.Last.1", "login@last.com")]
    public async Task Login_Flow(string dataPassword, string dataEmail)
    {
        var registerData = new RegisterRequest
        {
            Password = dataPassword,
            Email = dataEmail,
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/v1/auth/register", registerData);
        registerResponse.EnsureSuccessStatusCode();

        var loginData = new LoginRequest
        {
            Email = dataEmail,
            Password = dataPassword,
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginData);

        loginResponse.EnsureSuccessStatusCode();

        var jwtResponse = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            JsonHelpers.JsonSerializerOptionsCamelCase);
        jwtResponse.ShouldNotBeNull();
    }
}
