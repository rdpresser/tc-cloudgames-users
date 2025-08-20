namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.CreateUser;

public class CreateUserMapperTests
{
    [Fact]
    public void ToEntity_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = new CreateUserCommand("Test User", "test@example.com", "testuser", "TestPassword123!", "User");

        // Act
        var result = CreateUserMapper.ToAggregate(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<UserAggregate>();
    }

    [Fact]
    public void ToEntity_WithInvalidCommand_ShouldReturnInvalidResult()
    {
        // Arrange
        var command = new CreateUserCommand("", "", "", "", "");

        // Act
        var result = CreateUserMapper.ToAggregate(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeEmpty();
    }

    [Fact]
    public void FromEntity_ShouldMapAggregateToResponse()
    {
        // Arrange
        var command = new CreateUserCommand("Test User", "test@example.com", "testuser", "TestPassword123!", "User");
        var aggregateResult = CreateUserMapper.ToAggregate(command);
        var aggregate = aggregateResult.Value;

        // Act
        var response = CreateUserMapper.FromAggregate(aggregate);

        // Assert
        response.Name.ShouldBe(command.Name);
        response.Email.ShouldBe(command.Email);
        response.Username.ShouldBe(command.Username);
        response.Role.ShouldBe(command.Role);
        response.Id.ShouldBe(aggregate.Id);
    }
}
