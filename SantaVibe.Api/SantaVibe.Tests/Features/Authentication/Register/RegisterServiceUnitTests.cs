using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Register;

namespace SantaVibe.Tests.Features.Authentication.Register;

/// <summary>
/// Unit tests for RegisterService using NSubstitute
/// </summary>
public class RegisterServiceUnitTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterService> _logger;
    private readonly RegisterService _sut; // System Under Test

    public RegisterServiceUnitTests()
    {
        // Create substitute for UserManager
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore,
            null, null, null, null, null, null, null, null);

        // Create substitute for Configuration with JWT settings
        _configuration = Substitute.For<IConfiguration>();
        var jwtSection = Substitute.For<IConfigurationSection>();
        jwtSection["Secret"].Returns("test-secret-key-with-at-least-256-bits-length-for-jwt-token-signing");
        jwtSection["Issuer"].Returns("TestIssuer");
        jwtSection["Audience"].Returns("TestAudience");
        jwtSection["ExpirationInDays"].Returns("7");

        _configuration.GetSection("Jwt").Returns(jwtSection);

        // Create substitute for Logger
        _logger = Substitute.For<ILogger<RegisterService>>();

        // Create system under test
        _sut = new RegisterService(_userManager, _configuration, _logger);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return IdentityResult.Success;
            });

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.FirstName, result.Value.FirstName);
        Assert.Equal(request.LastName, result.Value.LastName);
        Assert.NotNull(result.Value.Token);
        Assert.NotEmpty(result.Value.Token);
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow);

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), request.Password);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Existing",
            LastName = "User"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(existingUser);

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("EmailAlreadyExists", result.Error);
        Assert.Equal("An account with this email address already exists", result.Message);
        Assert.Null(result.Value);

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterUserAsync_WhenUserCreationFails_ReturnsValidationFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "WeakPass",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must contain a digit" }
        };

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ValidationError", result.Error);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains("PasswordTooShort", result.ValidationErrors.Keys);
        Assert.Contains("PasswordRequiresDigit", result.ValidationErrors.Keys);

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), request.Password);
    }

    [Fact]
    public async Task RegisterUserAsync_CreatesUserWithCorrectProperties()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        ApplicationUser? capturedUser = null;

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(callInfo =>
            {
                capturedUser = callInfo.ArgAt<ApplicationUser>(0);
                capturedUser.Id = Guid.NewGuid().ToString();
                return IdentityResult.Success;
            });

        // Act
        await _sut.RegisterUserAsync(request);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.Equal(request.Email, capturedUser.Email);
        Assert.Equal(request.Email, capturedUser.UserName); // Username should be email
        Assert.Equal(request.FirstName, capturedUser.FirstName);
        Assert.Equal(request.LastName, capturedUser.LastName);
        Assert.False(capturedUser.EmailConfirmed); // MVP: no email verification
    }

    [Fact]
    public async Task RegisterUserAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return IdentityResult.Success;
            });

        // Act
        var result = await _sut.RegisterUserAsync(request);

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
    public async Task RegisterUserAsync_SetsCorrectTokenExpiration()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return IdentityResult.Success;
            });

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var expectedExpiration = DateTimeOffset.UtcNow.AddDays(7);
        var timeDifference = Math.Abs((result.Value.ExpiresAt - expectedExpiration).TotalSeconds);

        // Allow 5 seconds tolerance for test execution time
        Assert.True(timeDifference < 5, $"Token expiration is off by {timeDifference} seconds");
    }

    [Fact]
    public async Task RegisterUserAsync_LogsInformationMessages()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return IdentityResult.Success;
            });

        // Act
        await _sut.RegisterUserAsync(request);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Attempting to register user")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Registration completed successfully")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task RegisterUserAsync_LogsWarningOnDuplicateEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Existing",
            LastName = "User"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(existingUser);

        // Act
        await _sut.RegisterUserAsync(request);

        // Assert
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Email already exists")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
