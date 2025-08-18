namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserByEmail;

public class GetUserByEmailQueryHandlerTests : BaseTest
{
    [Fact]
    public async Task ExecuteAsync_WithExistingUser_ShouldReturnUserResponse()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithExistingUser_ShouldReturnUserResponse));
        Factory.RegisterTestServices(_ => { });
        var userContext = A.Fake<IUserContext>();
        var repo = A.Fake<IUserRepository>();
        var email = "test@example.com";
        var userResponse = new UserByEmailResponse
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Username = "testuser",
            Email = email,
            Role = "User"
        };
        var query = new GetUserByEmailQuery(email);
        var handler = new GetUserByEmailQueryHandler(repo, userContext);
        A.CallTo(() => repo.GetByEmailAsync(email, A<CancellationToken>.Ignored)).Returns(userResponse);

        // Act
        var result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Email.ShouldBe(email);
        result.Value.Name.ShouldBe("Test User");
        result.Value.Username.ShouldBe("testuser");
        result.Value.Role.ShouldBe("User");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistingUser_ShouldReturnNotFoundError()
    {
        // Arrange
        LogTestStart(nameof(ExecuteAsync_WithNonExistingUser_ShouldReturnNotFoundError));
        Factory.RegisterTestServices(_ => { });

        var userContext = A.Fake<IUserContext>();
        var repo = A.Fake<IUserRepository>();

        var email = "notfound@example.com";
        var query = new GetUserByEmailQuery(email);
        var handler = new GetUserByEmailQueryHandler(repo, userContext);
        A.CallTo(() => repo.GetByEmailAsync(email, A<CancellationToken>.Ignored)).Returns((UserByEmailResponse?)null);

        // Act
        var result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count().ShouldBe(1);
        result.Errors.Count(e => e == $"User with email '{email}' not found.").ShouldBe(1);
        result.Status.ShouldBe(ResultStatus.NotFound);
    }
}
