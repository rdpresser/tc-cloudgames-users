using TC.CloudGames.Users.Domain.Aggregates;
using TC.CloudGames.Users.Domain.ValueObjects;
using Ardalis.Result;
using Shouldly;
using Xunit;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateTests
{
    [Fact]
    public void CreateFromResult_WithAllSuccessResults_ShouldCreateUserAggregate()
    {
        // Arrange
        var name = "Test User";
        var emailResult = Email.Create("test@example.com");
        var username = "testuser";
        var passwordResult = Password.Create("TestPassword123!");
        var roleResult = Role.Create("User");

        // Act
        var result = UserAggregate.CreateFromResult(name, emailResult, username, passwordResult, roleResult);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(name);
        result.Value.Email.Value.ShouldBe("test@example.com");
        result.Value.Username.ShouldBe(username);
        result.Value.Role.Value.ShouldBe("User");
    }

    [Fact]
    public void CreateFromResult_WithAnyFailure_ShouldReturnFailure()
    {
        // Arrange
        var name = "Test User";
        var emailResult = Email.Create(""); // Invalid
        var username = "testuser";
        var passwordResult = Password.Create("TestPassword123!");
        var roleResult = Role.Create("User");

        // Act
        var result = UserAggregate.CreateFromResult(name, emailResult, username, passwordResult, roleResult);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeEmpty();
    }
}
