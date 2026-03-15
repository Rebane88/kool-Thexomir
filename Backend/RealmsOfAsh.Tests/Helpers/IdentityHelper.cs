using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Application.Services.Auth.DTOs;
using Base;
using Xunit;

namespace RealmsOfAsh.Tests.Helpers;

public static class IdentityHelper
{
    public static async Task<RegisterResponse> SetupUserAsync(HttpClient httpClient, string firstName, string lastName, string password, string email)
    {
        var data = new RegisterRequest()
        {
            Password = password,
            Email = email,
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/v1/auth/register",
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(data, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json")
        );

        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        var registerResponse = System.Text.Json.JsonSerializer.Deserialize<RegisterResponse>(responseString, JsonHelpers.JsonSerializerOptionsCamelCase);

        Assert.NotNull(registerResponse);

        return registerResponse;
    }
}
