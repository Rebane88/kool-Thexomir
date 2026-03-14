using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.v1.Identity;
using Base;
using Xunit;

namespace WebApp.Tests.Helpers;

public static class IdentityHelper
{
    public static async Task<JWTResponse> SetupUserAsync(HttpClient httpClient, string firstName, string lastName, string password, string email)
    {
        var data = new Register()
        {
            Password = password,
            Email = email,
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/v1/account/register",
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(data, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json")
        );

        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        var jwtResponse = System.Text.Json.JsonSerializer.Deserialize<JWTResponse>(responseString, JsonHelpers.JsonSerializerOptionsCamelCase);

        Assert.NotNull(jwtResponse);

        return jwtResponse;
    }
}
