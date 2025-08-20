using TC.CloudGames.Users.Unit.Tests.Fakes;
using Wolverine;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.CreateUser;

public class CreateUserCommandHandlerTests(App App) : TestBase<App>
{
    [Theory, AutoFakeItEasyValidUserData]
    public async Task ExecuteAsync_WithValidCommand_ShouldReturnSuccessResponse(CreateUserCommand command)
    {
        // Arrange
        Factory.RegisterTestServices(_ => { });
        var repo = new FakeUserRepository();
        var userContext = App.GetValidLoggedUser();

        var bus = A.Fake<IMessageBus>();
        var looger = A.Fake<ILogger<CreateUserCommandHandler>>();

        command = new CreateUserCommand("Test User", "test@example.com", "testuser", "TestPassword123!", "User");
        var handler = new CreateUserCommandHandler(repo, userContext, bus, looger);

        // Act
        var result = await handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(command.Name);
        result.Value.Email.ShouldBe(command.Email);
        result.Value.Username.ShouldBe(command.Username);
        result.Value.Role.ShouldBe(command.Role);
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
        Factory.RegisterTestServices(_ => { });
        var repo = new FakeUserRepository();
        var userContext = App.GetValidLoggedUser();
        var bus = A.Fake<IMessageBus>();
        var looger = A.Fake<ILogger<CreateUserCommandHandler>>();
        var command = new CreateUserCommand(name, email, username, password, role);
        var handler = new CreateUserCommandHandler(repo, userContext, bus, looger);

        // Act
        var result = await handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

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
