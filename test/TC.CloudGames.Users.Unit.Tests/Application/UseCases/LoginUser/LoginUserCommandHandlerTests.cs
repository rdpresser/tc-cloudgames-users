namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.LoginUser;

/// <summary>
/// Unit tests for LoginUserCommandHandler
/// These are pure unit tests that test the handler in isolation using mocks
/// </summary>
public class LoginUserCommandHandlerTests : BaseTest
{
    private readonly IFixture _fixture;

    public LoginUserCommandHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ShouldReturnSuccessWithJwtToken()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithValidCredentials_ShouldReturnSuccessWithJwtToken));
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = _fixture.Build<LoginUserCommand>()
            .With(x => x.Email, "valid@email.com")
            .With(x => x.Password, "ValidPass123!")
            .Create();

        var userTokenInfo = _fixture.Build<UserTokenProvider>()
            .With(x => x.Email, command.Email)
            .With(x => x.Role, "User")
            .Create();

        var expectedJwtToken = "valid-jwt-token-123";

        // Setup mocks
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .Returns(userTokenInfo);
        A.CallTo(() => tokenProvider.Create(userTokenInfo))
            .Returns(expectedJwtToken);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.JwtToken.ShouldBe(expectedJwtToken);
        result.Value.Email.ShouldBe(command.Email);
        result.ValidationErrors.ShouldBeEmpty();
        (result.Errors == null || !result.Errors.Any()).ShouldBeTrue();

        // Verify all expected calls were made
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => tokenProvider.Create(userTokenInfo))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCredentials_ShouldReturnFailureWithValidationError()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithInvalidCredentials_ShouldReturnFailureWithValidationError));
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = _fixture.Build<LoginUserCommand>()
            .With(x => x.Email, "invalid@email.com")
            .With(x => x.Password, "WrongPassword!")
            .Create();

        // Setup repo to return null (invalid credentials)
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .Returns((UserTokenProvider?)null);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();

        // Check for validation errors in the expected format
        var hasExpectedValidationError = result.ValidationErrors.Any(e =>
            e.Identifier == "User|Password" &&
            e.ErrorMessage == "Email or password provided are invalid.");

        var hasExpectedGeneralError = result.Errors?.Any(e =>
            e.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("User|Password", StringComparison.OrdinalIgnoreCase)) == true;

        (hasExpectedValidationError || hasExpectedGeneralError).ShouldBeTrue(
            $"Expected validation error not found. ValidationErrors: [{string.Join(", ", result.ValidationErrors.Select(e => $"{e.Identifier}:{e.ErrorMessage}"))}], " +
            $"Errors: [{string.Join(", ", result.Errors ?? new List<string>())}]");

        // Verify repository was called but token provider was not
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => tokenProvider.Create(A<UserTokenProvider>.Ignored))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData("", "ValidPass123!")]
    [InlineData("invalid-email", "ValidPass123!")]
    [InlineData("valid@email.com", "")]
    [InlineData("valid@email.com", "123")] // Too short password
    public async Task ExecuteAsync_WithInvalidInput_ShouldStillProcessAndReturnNotFound(string email, string password)
    {
        // Arrange
        LogTestStart($"{nameof(ExecuteAsync_WithInvalidInput_ShouldStillProcessAndReturnNotFound)}: {email}|{password}");
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = new LoginUserCommand(email, password);

        // Setup repo to return null (no user found with these credentials)
        A.CallTo(() => repo.GetUserTokenInfoAsync(email, password, A<CancellationToken>.Ignored))
            .Returns((UserTokenProvider?)null);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.NotFound);

        // Should have the "Invalid credentials" error message
        (result.Errors?.Any(e => e.Contains("invalid", StringComparison.OrdinalIgnoreCase)) == true).ShouldBeTrue(
            $"Expected 'invalid credentials' error not found. Errors: [{string.Join(", ", result.Errors ?? new List<string>())}]");

        // Repository should be called even with invalid input (no input validation in this handler)
        A.CallTo(() => repo.GetUserTokenInfoAsync(email, password, A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => tokenProvider.Create(A<UserTokenProvider>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WhenRepositoryThrows_ShouldPropagateException));
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = _fixture.Create<LoginUserCommand>();
        var expectedException = new InvalidOperationException("Database connection failed");

        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .Throws(expectedException);

        // Act & Assert
        var actualException = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(command, CancellationToken.None));

        actualException.Message.ShouldBe("Database connection failed");

        // Verify token provider was not called
        A.CallTo(() => tokenProvider.Create(A<UserTokenProvider>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenProviderThrows_ShouldPropagateException()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WhenTokenProviderThrows_ShouldPropagateException));
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = _fixture.Create<LoginUserCommand>();
        var userTokenInfo = _fixture.Build<UserTokenProvider>()
            .With(x => x.Email, command.Email)
            .Create();
        var expectedException = new InvalidOperationException("Token generation failed");

        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .Returns(userTokenInfo);
        A.CallTo(() => tokenProvider.Create(userTokenInfo))
            .Throws(expectedException);

        // Act & Assert
        var actualException = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(command, CancellationToken.None));

        actualException.Message.ShouldBe("Token generation failed");

        // Verify repository was called
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithCancellationToken_ShouldPassTokenToRepository));
        Factory.RegisterTestServices(_ => { });

        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);

        var command = _fixture.Create<LoginUserCommand>();
        var userTokenInfo = _fixture.Build<UserTokenProvider>()
            .With(x => x.Email, command.Email)
            .Create();
        var cancellationToken = new CancellationToken();

        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, cancellationToken))
            .Returns(userTokenInfo);
        A.CallTo(() => tokenProvider.Create(userTokenInfo))
            .Returns("token");

        // Act
        var result = await handler.ExecuteAsync(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify the specific cancellation token was passed
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}
