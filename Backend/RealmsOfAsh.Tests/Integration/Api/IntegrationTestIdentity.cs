using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Services.Auth.DTOs;
using Base;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RealmsOfAsh.Tests.Integration.Api;

[Collection("Database tests")]
public class IntegrationTestIdentity : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;


    public IntegrationTestIdentity(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("First.Last.1", "first@last.com")]
    public async Task Registration_Flow(string dataPassword, string dataEmail)
    {
        // Arrange
        var data = new RegisterRequest()
        {
            Password = dataPassword,
            Email = dataEmail,
        };

        // Act
        var response = await _client.PostAsync(
            "/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(data, JsonHelpers.JsonSerializerOptionsCamelCase), Encoding.UTF8,
                "application/json")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var registerResponse =
            JsonSerializer.Deserialize<RegisterResponse>(responseString, JsonHelpers.JsonSerializerOptionsCamelCase);
        Assert.NotNull(registerResponse);
    }

    [Theory]
    [InlineData("First.Last.1", "login@last.com")]
    public async Task Login_Flow(string dataPassword, string dataEmail)
    {
        // Arrange
        var registerData = new RegisterRequest()
        {
            Password = dataPassword,
            Email = dataEmail,
        };

        var response = await _client.PostAsync(
            "/api/v1/auth/register",
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerData, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json")
        );
        response.EnsureSuccessStatusCode();


        var loginData = new LoginRequest()
        {
            Email = dataEmail,
            Password = dataPassword,
        };


        // Act

        var loginResponse = await _client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(loginData, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json")
        );


        // Assert
        loginResponse.EnsureSuccessStatusCode();

        var responseString = await loginResponse.Content.ReadAsStringAsync();
        var jwtResponse =
            JsonSerializer.Deserialize<LoginResponse>(responseString, JsonHelpers.JsonSerializerOptionsCamelCase);
        Assert.NotNull(jwtResponse);
    }
}
