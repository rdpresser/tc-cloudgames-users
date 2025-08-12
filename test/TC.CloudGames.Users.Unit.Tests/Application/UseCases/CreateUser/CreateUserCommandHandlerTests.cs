using FastEndpoints;
using TC.CloudGames.Users.Unit.Tests.Common;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.CreateUser;

public class CreateUserCommandHandlerTests : BaseTest
{
    [Theory, AutoFakeItEasyData]
    public async Task ExecuteAsync_WithValidCommand_ShouldReturnSuccessResponse()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithValidCommand_ShouldReturnSuccessResponse));
        Factory.RegisterTestServices(_ => { });
        var repo = A.Fake<IUserRepository>();
        var command = new CreateUserCommand("Test User", "test@example.com", "testuser", "TestPassword123!", "User");
        var handler = new CreateUserCommandHandler(repo);
        A.CallTo(() => repo.SaveAsync(A<UserAggregate>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.CompletedTask);

        // Act
        var result = await handler.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(command.Name);
        result.Value.Email.ShouldBe(command.Email);
        result.Value.Username.ShouldBe(command.Username);
        result.Value.Role.ShouldBe(command.Role);
        A.CallTo(() => repo.SaveAsync(A<UserAggregate>.That.Matches(u =>
            u.Name == command.Name &&
            u.Email.Value == command.Email &&
            u.Username == command.Username &&
            u.Role.Value == command.Role), A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("", "test@example.com", "testuser", "TestPassword123!", "User")]
    [InlineData("Test User", "", "testuser", "TestPassword123!", "User")]
    [InlineData("Test User", "test@example.com", "", "TestPassword123!", "User")]
    [InlineData("Test User", "test@example.com", "testuser", "", "User")]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "InvalidRole")]
    public async Task ExecuteAsync_WithInvalidCommand_ShouldReturnInvalidResult(
        string name, string email, string username, string password, string role)
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithInvalidCommand_ShouldReturnInvalidResult));
        Factory.RegisterTestServices(_ => { });
        var repo = A.Fake<IUserRepository>();
        var command = new CreateUserCommand(name, email, username, password, role);
        var handler = new CreateUserCommandHandler(repo);

        // Act
        var result = await handler.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeEmpty();

        if (string.IsNullOrWhiteSpace(name))
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required" && e.ErrorMessage == "Name is required.");
        if (string.IsNullOrWhiteSpace(email))
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Email.Required" && e.ErrorMessage == "Email is required.");
        if (string.IsNullOrWhiteSpace(username))
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.Required" && e.ErrorMessage == "Username is required.");
        if (string.IsNullOrWhiteSpace(password))
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.Required" && e.ErrorMessage == "Password is required.");
        if (!new[] { "User", "Admin", "Moderator" }.Contains(role))
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.InvalidRole" || e.Identifier == "Role.Invalid");
    }
}
