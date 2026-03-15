using Application.Contracts;
using Application.Services.Auth;
using Application.Services.Auth.DTOs;
using Base.Contracts;
using Domain.Identity;
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
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_identityMock.Object, _unitOfWorkMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AppUser MakeUser(string email = "test@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = email
    };

    private static AppRefreshToken MakeRefreshToken(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        RefreshToken = "refresh-token-" + Guid.NewGuid()
    };

    // -------------------------------------------------------------------------
    // Register tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_HappyPath_ReturnsOkWithUserDetails()
    {
        var user = MakeUser("test@example.com");

        _identityMock.Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result<AppUser>.Ok(user));
        _identityMock.Setup(x => x.AddToRoleAsync(user, "Player"))
            .ReturnsAsync(Result<bool>.Ok(true));
        _identityMock.Setup(x => x.GetRolesAsync(user))
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
            .ReturnsAsync(Result<AppUser>.Fail("Email already taken."));

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
        var refreshToken = MakeRefreshToken(user.Id);

        _identityMock.Setup(x => x.GetByEmailAsync("login@example.com"))
            .ReturnsAsync(Result<AppUser>.Ok(user));
        _identityMock.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Ok(true));
        _identityMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(Result<IList<string>>.Ok(new List<string> { "Player" }));
        _identityMock.Setup(x => x.GenerateJwtAsync(user, It.IsAny<DateTime>()))
            .ReturnsAsync(Result<string>.Ok("jwt-token"));
        _identityMock.Setup(x => x.CreateRefreshTokenAsync(user.Id))
            .ReturnsAsync(Result<AppRefreshToken>.Ok(refreshToken));

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
            .ReturnsAsync(Result<AppUser>.Ok(user));
        _identityMock.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
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
        var user = MakeUser(email);
        var oldRefreshToken = MakeRefreshToken(user.Id);
        oldRefreshToken.User = user;
        var newRefreshToken = MakeRefreshToken(user.Id);

        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken.RefreshToken))
            .ReturnsAsync(Result<AppRefreshToken>.Ok(oldRefreshToken));
        _identityMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(Result<IList<string>>.Ok(new List<string> { "Player" }));
        _identityMock.Setup(x => x.GenerateJwtAsync(user, It.IsAny<DateTime>()))
            .ReturnsAsync(Result<string>.Ok("new-jwt-token"));
        _identityMock.Setup(x => x.RotateRefreshTokenAsync(oldRefreshToken))
            .ReturnsAsync(Result<AppRefreshToken>.Ok(newRefreshToken));

        var result = await _sut.RefreshAsync(oldRefreshToken.RefreshToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ReturnsFail()
    {
        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<AppRefreshToken>.Fail("Invalid refresh token"));

        var result = await _sut.RefreshAsync("invalid-refresh");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFail()
    {
        _identityMock.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<AppRefreshToken>.Fail("Refresh token expired"));

        var result = await _sut.RefreshAsync("expired-refresh");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("Refresh token expired");
    }
}
