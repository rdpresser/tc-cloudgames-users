using Wolverine.Marten;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.CreateUser;

public class CreateUserCommandHandlerTests : BaseTest
{
    [Fact]
    public async Task ExecuteAsync_WithValidCommand_ShouldReturnSuccessResponse()
    {
        // Arrange
        Factory.RegisterTestServices(_ => { });
        
        var repo = A.Fake<IUserRepository>();
        var outbox = A.Fake<IMartenOutbox>();
        var logger = A.Fake<ILogger<CreateUserCommandHandler>>();
        var userContext = A.Fake<IUserContext>();

        // Setup user context
        A.CallTo(() => userContext.Id).Returns(Guid.NewGuid());
        A.CallTo(() => userContext.Name).Returns("Test User");
        A.CallTo(() => userContext.Email).Returns("test@example.com");
        A.CallTo(() => userContext.Username).Returns("testuser");
        A.CallTo(() => userContext.Role).Returns("Admin");
        A.CallTo(() => userContext.IsAuthenticated).Returns(true);

        var command = new CreateUserCommand("Test User", "test@example.com", "testuser", "TestPassword123!", "User");
        var handler = new CreateUserCommandHandler(repo, userContext, outbox, logger);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(command.Name);
        result.Value.Email.ShouldBe(command.Email);
        result.Value.Username.ShouldBe(command.Username);
        result.Value.Role.ShouldBe(command.Role);

        // Verify interactions - handler calls SaveAsync and CommitAsync
        A.CallTo(() => repo.SaveAsync(A<UserAggregate>.That.IsNotNull(), A<CancellationToken>.That.IsEqualTo(CancellationToken.None)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => repo.CommitAsync(A<UserAggregate>.That.IsNotNull(), A<CancellationToken>.That.IsEqualTo(CancellationToken.None)))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("", "test@example.com", "testuser", "TestPassword123!", "User", "Name.Required")]
    [InlineData("Test User", "", "testuser", "TestPassword123!", "User", "Email.Required")]
    [InlineData("Test User", "test@example.com", "", "TestPassword123!", "User", "Username.Required")]
    [InlineData("Test User", "test@example.com", "testuser", "", "User", "Password.Required")]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "InvalidRole", "Role.Invalid")]
    public async Task ExecuteAsync_WithInvalidCommand_ShouldReturnInvalidResult(
        string name, string email, string username, string password, string role, string expectedErrorPrefix)
    {
        // Arrange
        Factory.RegisterTestServices(_ => { });
        
        var repo = A.Fake<IUserRepository>();
        var outbox = A.Fake<IMartenOutbox>();
        var logger = A.Fake<ILogger<CreateUserCommandHandler>>();
        var userContext = A.Fake<IUserContext>();

        // Setup user context
        A.CallTo(() => userContext.Id).Returns(Guid.NewGuid());
        A.CallTo(() => userContext.Name).Returns("Test User");
        A.CallTo(() => userContext.Email).Returns("test@example.com");
        A.CallTo(() => userContext.Username).Returns("testuser");
        A.CallTo(() => userContext.Role).Returns("Admin");
        A.CallTo(() => userContext.IsAuthenticated).Returns(true);

        var command = new CreateUserCommand(name, email, username, password, role);
        var handler = new CreateUserCommandHandler(repo, userContext, outbox, logger);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeEmpty();
        result.ValidationErrors.Any(e => e.Identifier.StartsWith(expectedErrorPrefix)).ShouldBeTrue();

        // Verify no persistence happened for invalid commands
        A.CallTo(repo).Where(call => call.Method.Name == "SaveAsync").MustNotHaveHappened();
        A.CallTo(repo).Where(call => call.Method.Name == "CommitAsync").MustNotHaveHappened();
        A.CallTo(outbox).Where(call => call.Method.Name == "PublishAsync").MustNotHaveHappened();
    }
}