namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.CreateUser;

public class CreateUserCommandValidatorTests
{
    [Theory]
    [InlineData("", "test@example.com", "testuser", "TestPassword123!", "User", false)]
    [InlineData("Test User", "invalid-email", "testuser", "TestPassword123!", "User", false)]
    [InlineData("Test User", "test@example.com", "", "TestPassword123!", "User", false)]
    [InlineData("Test User", "test@example.com", "testuser", "weak", "User", false)]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "InvalidRole", false)]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "User", true)]
    public void Validate_ShouldReturnExpectedResult(string name, string email, string username, string password, string role, bool expectedIsValid)
    {
        var repo = A.Fake<IUserRepository>();
        var _validator = new CreateUserCommandValidator(repo);

        // Arrange
        var command = new CreateUserCommand(name, email, username, password, role);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBe(expectedIsValid);
    }
}
