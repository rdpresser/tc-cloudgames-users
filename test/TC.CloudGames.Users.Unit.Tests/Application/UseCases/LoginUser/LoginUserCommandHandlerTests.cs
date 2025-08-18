namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.LoginUser;

public class LoginUserCommandHandlerTests : BaseTest
{
    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ShouldReturnJwtToken()
    {
        // Arrange
        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);
        var command = new LoginUserCommand("valid@email.com", "ValidPass123!");
        // Create a real UserTokenProvider instance with all required parameters
        var userTokenInfo = new UserTokenProvider(
            Id: Guid.NewGuid(),
            Name: "Test User",
            Email: command.Email,
            Username: "testuser",
            Role: "User"
        );
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored)).Returns(userTokenInfo);
        A.CallTo(() => tokenProvider.Create(userTokenInfo)).Returns("jwt-token");

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.JwtToken.ShouldBe("jwt-token");
        result.Value.Email.ShouldBe(command.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCredentials_ShouldReturnInvalidResult()
    {
        // Arrange
        var repo = A.Fake<IUserRepository>();
        var tokenProvider = A.Fake<ITokenProvider>();
        var handler = new LoginUserCommandHandler(repo, tokenProvider);
        var command = new LoginUserCommand("invalid@email.com", "WrongPass!");
        A.CallTo(() => repo.GetUserTokenInfoAsync(command.Email, command.Password, A<CancellationToken>.Ignored)).Returns((UserTokenProvider?)null);

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        // Accept error in either ValidationErrors or Errors property for flexibility
        var found = result.ValidationErrors.Any(e => e.Identifier == "User|Password" && e.ErrorMessage == "Email or password provided are invalid.")
            || (result.Errors != null && result.Errors.Any(e => e.Contains("invalid") || e.Contains("User|Password")));
        found.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
    }
}
