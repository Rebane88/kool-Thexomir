using Application.Contracts;
using Application.Contracts.Identity;
using Application.Services.Auth;
using Application.Services.Auth.DTOs.V1;
using Base.Contracts;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// AuthService unit tests — fully mocked, no database dependency.
/// Covers: register happy path, duplicate email,
///         login happy path, wrong password,
///         refresh happy path, invalid token, expired token.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IIdentityService> _identityMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_identityMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AppUserInfo MakeUser(string email = "test@example.com") =>
        new(Guid.NewGuid(), email);

    private static RefreshTokenInfo MakeRefreshToken(Guid userId, string email = "test@example.com") =>
        new(userId, email, "refresh-token-" + Guid.NewGuid());

    // -------------------------------------------------------------------------
    // Register tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_HappyPath_ReturnsOkWithUserDetails()
    {
        var user = MakeUser("test@example.com");

        _identityMock.Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result<AppUserInfo>.Ok(user));
        _identityMock.Setup(x => x.AddToRoleAsync(user.Id, "Player"))
            .ReturnsAsync(Result<bool>.Ok(true));
        _identityMock.Setup(x => x.GetRolesAsync(user.Id))
            .ReturnsAsync(Result<IList<string>>.Ok(new List<string> { "Player" }));

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test.Password1"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Email.ShouldBe("test@example.com");
        result.Value.Roles.ShouldContain("Player");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
    {
        _identityMock.Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result<AppUserInfo>.Fail("Email already taken."));

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            Email = "dup@example.com",
            Password = "Test.Password1"
        });

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    // -------------------------------------------------------------------------
    // Login tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_HappyPath_ReturnsTokens()
    {
        var user = MakeUser("login@example.com");
        var refreshToken = MakeRefreshToken(user.Id, user.Email);

        _identityMock.Setup(x => x.GetByEmailAsync("login@example.com"))
            .ReturnsAsync(Result<AppUserInfo>.Ok(user));
        _identityMock.Setup(x => x.CheckPasswordAsync(user.Id, It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Ok(true));
        _identityMock.Setup(x => x.GetRolesAsync(user.Id))
            .ReturnsAsync(Result<IList<string>>.Ok(new List<string> { "Player" }));
        _identityMock.Setup(x => x.GenerateJwtAsync(user.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(Result<string>.Ok("jwt-token"));
        _identityMock.Setup(x => x.CreateRefreshTokenAsync(user.Id))
            .ReturnsAsync(Result<RefreshTokenInfo>.Ok(refreshToken));

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "Test.Password1"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFail()
    {
        // NOTE: We do NOT assert timing here — the random delay is an implementation
        // detail and would make the test slow and non-deterministic.
        var user = MakeUser("wp@example.com");

        _identityMock.Setup(x => x.GetByEmailAsync("wp@example.com"))
            .ReturnsAsync(Result<AppUserInfo>.Ok(user));
        _identityMock.Setup(x => x.CheckPasswordAsync(user.Id, It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Fail("Wrong password."));

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "wp@example.com",
            Password = "Wrong.Password1"
        });

        result.IsSuccess.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Refresh tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_HappyPath_ReturnsNewTokens()
    {
        const string email = "refresh@example.com";
        var userId = Guid.NewGuid();
        var oldRefreshToken = MakeRefreshToken(userId, email);
        var newRefreshToken = MakeRefreshToken(userId, email);

        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken.Token))
            .ReturnsAsync(Result<RefreshTokenInfo>.Ok(oldRefreshToken));
        _identityMock.Setup(x => x.GetRolesAsync(userId))
            .ReturnsAsync(Result<IList<string>>.Ok(new List<string> { "Player" }));
        _identityMock.Setup(x => x.GenerateJwtAsync(userId, It.IsAny<DateTime>()))
            .ReturnsAsync(Result<string>.Ok("new-jwt-token"));
        _identityMock.Setup(x => x.RotateRefreshTokenAsync(oldRefreshToken.Token))
            .ReturnsAsync(Result<RefreshTokenInfo>.Ok(newRefreshToken));

        var result = await _sut.RefreshAsync(oldRefreshToken.Token);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ReturnsFail()
    {
        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<RefreshTokenInfo>.Fail("Invalid refresh token"));

        var result = await _sut.RefreshAsync("invalid-refresh");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFail()
    {
        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<RefreshTokenInfo>.Fail("Refresh token expired"));

        var result = await _sut.RefreshAsync("expired-refresh");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("Refresh token expired");
    }
}
