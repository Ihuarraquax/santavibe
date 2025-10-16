using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;

namespace SantaVibe.Tests.Features.Authentication.Login;

/// <summary>
/// Unit tests for LoginService using NSubstitute
/// </summary>
public class LoginServiceUnitTests
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IConfiguration configuration;
    private readonly ILogger<LoginService> logger;
    private readonly LoginService sut; // System Under Test

    public LoginServiceUnitTests()
    {
        // Create substitute for UserManager
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore,
            null, null, null, null, null, null, null, null);

        // Create substitute for SignInManager
        signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            userManager,
            Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);

        // Create substitute for Configuration with JWT settings
        configuration = Substitute.For<IConfiguration>();
        var jwtSection = Substitute.For<IConfigurationSection>();
        jwtSection["Secret"].Returns("test-secret-key-with-at-least-256-bits-length-for-jwt-token-signing");
        jwtSection["Issuer"].Returns("TestIssuer");
        jwtSection["Audience"].Returns("TestAudience");
        jwtSection["ExpirationInDays"].Returns("7");

        configuration.GetSection("Jwt").Returns(jwtSection);

        // Create substitute for Logger
        logger = Substitute.For<ILogger<LoginService>>();

        // Create system under test
        sut = new LoginService(userManager, signInManager, configuration, logger);
    }

    [Fact]
    public async Task LoginUserAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(user.FirstName, result.Value.FirstName);
        Assert.Equal(user.LastName, result.Value.LastName);
        Assert.NotNull(result.Value.Token);
        Assert.NotEmpty(result.Value.Token);
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow);

        await userManager.Received(1).FindByEmailAsync(request.Email);
        await signInManager.Received(1).CheckPasswordSignInAsync(user, request.Password, false);
        await userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task LoginUserAsync_WithNonExistentEmail_ReturnsGenericFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SecurePass123!"
        };

        userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidCredentials", result.Error);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Value);

        await userManager.Received(1).FindByEmailAsync(request.Email);
        await signInManager.DidNotReceive().CheckPasswordSignInAsync(
            Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task LoginUserAsync_WithSoftDeletedUser_ReturnsGenericFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "deleted@example.com",
            Password = "SecurePass123!"
        };

        var deletedUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Deleted",
            LastName = "User",
            IsDeleted = true // User is soft-deleted
        };

        userManager.FindByEmailAsync(request.Email).Returns(deletedUser);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidCredentials", result.Error);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Value);

        await userManager.Received(1).FindByEmailAsync(request.Email);
        await signInManager.DidNotReceive().CheckPasswordSignInAsync(
            Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task LoginUserAsync_WithInvalidPassword_ReturnsGenericFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidCredentials", result.Error);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Value);

        await userManager.Received(1).FindByEmailAsync(request.Email);
        await signInManager.Received(1).CheckPasswordSignInAsync(user, request.Password, false);
        await userManager.DidNotReceive().UpdateAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task LoginUserAsync_UpdatesLastLoginAt()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false,
            LastLoginAt = null // Initially null
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var beforeLogin = DateTimeOffset.UtcNow;
        var result = await sut.LoginUserAsync(request);
        var afterLogin = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(user.LastLoginAt);
        Assert.True(user.LastLoginAt >= beforeLogin);
        Assert.True(user.LastLoginAt <= afterLogin);

        await userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task LoginUserAsync_ContinuesOnUpdateFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Update fails (non-critical operation)
        var updateErrors = new[]
        {
            new IdentityError { Code = "UpdateFailed", Description = "Failed to update LastLoginAt" }
        };
        userManager.UpdateAsync(user).Returns(IdentityResult.Failed(updateErrors));

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        // Login should still succeed even if LastLoginAt update fails
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Email, result.Value.Email);

        await userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task LoginUserAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Token);
        Assert.NotEmpty(result.Value.Token);

        // JWT tokens have 3 parts separated by dots
        var tokenParts = result.Value.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }

    [Fact]
    public async Task LoginUserAsync_SetsCorrectTokenExpiration()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await sut.LoginUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var expectedExpiration = DateTimeOffset.UtcNow.AddDays(7);
        var timeDifference = Math.Abs((result.Value.ExpiresAt - expectedExpiration).TotalSeconds);

        // Allow 5 seconds tolerance for test execution time
        Assert.True(timeDifference < 5, $"Token expiration is off by {timeDifference} seconds");
    }

    [Fact]
    public async Task LoginUserAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        await sut.LoginUserAsync(request);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Login attempt")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Login successful")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LoginUserAsync_LogsWarningOnInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsDeleted = false
        };

        userManager.FindByEmailAsync(request.Email).Returns(user);
        signInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        await sut.LoginUserAsync(request);

        // Assert
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Login failed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LoginUserAsync_LogsWarningOnNonExistentUser()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SecurePass123!"
        };

        userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

        // Act
        await sut.LoginUserAsync(request);

        // Assert
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("User not found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LoginUserAsync_LogsWarningOnSoftDeletedUser()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "deleted@example.com",
            Password = "SecurePass123!"
        };

        var deletedUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Deleted",
            LastName = "User",
            IsDeleted = true
        };

        userManager.FindByEmailAsync(request.Email).Returns(deletedUser);

        // Act
        await sut.LoginUserAsync(request);

        // Assert
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("soft-deleted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
