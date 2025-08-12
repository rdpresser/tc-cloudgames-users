namespace TC.CloudGames.Users.Unit.Tests.Application;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Theory]
    [InlineData("", "test@example.com", "testuser", "TestPassword123!", "User", false)]
    [InlineData("Test User", "invalid-email", "testuser", "TestPassword123!", "User", false)]
    [InlineData("Test User", "test@example.com", "", "TestPassword123!", "User", false)]
    [InlineData("Test User", "test@example.com", "testuser", "weak", "User", false)]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "InvalidRole", false)]
    [InlineData("Test User", "test@example.com", "testuser", "TestPassword123!", "User", true)]
    public void Validate_ShouldReturnExpectedResult(string name, string email, string username, string password, string role, bool expectedIsValid)
    {
        // Arrange
        var command = new CreateUserCommand(name, email, username, password, role);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBe(expectedIsValid);
    }
}
