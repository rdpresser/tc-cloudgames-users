using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserList
{
    public class GetUserListQueryHandlerTests : BaseTest
    {
        [Fact]
        public async Task ExecuteAsync_WithUsers_ShouldReturnUserListResponse()
        {
            // Arrange
            LogTestStart(nameof(ExecuteAsync_WithUsers_ShouldReturnUserListResponse));
            Factory.RegisterTestServices(_ => { });

            var repo = A.Fake<IUserRepository>();
            var users = new List<UserListResponse>
            {
                new UserListResponse { Id = Guid.NewGuid(), Name = "John Smith", Username = "johnsmith", Email = "john.smith@gmail.com", Role = "Admin" },
                new UserListResponse { Id = Guid.NewGuid(), Name = "Jane Doe", Username = "janedoe", Email = "jane.doe@gmail.com", Role = "User" }
            };
            var query = new GetUserListQuery(1, 10, "id", "asc", "");
            A.CallTo(() => repo.GetUserListAsync(query, A<CancellationToken>.Ignored)).Returns(users);
            var handler = new GetUserListQueryHandler(repo);

            // Act
            var result = await handler.ExecuteAsync(query, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            result.Value.Count.ShouldBe(2);
            result.Value[0].Name.ShouldBe("John Smith");
            result.Value[1].Name.ShouldBe("Jane Doe");
        }

        [Fact]
        public async Task ExecuteAsync_WithNoUsers_ShouldReturnEmptyList()
        {
            // Arrange
            LogTestStart(nameof(ExecuteAsync_WithNoUsers_ShouldReturnEmptyList));
            Factory.RegisterTestServices(_ => { });

            var repo = A.Fake<IUserRepository>();
            var query = new GetUserListQuery(1, 10, "id", "asc", "");
            A.CallTo(() => repo.GetUserListAsync(query, A<CancellationToken>.Ignored)).Returns(new List<UserListResponse>());
            var handler = new GetUserListQueryHandler(repo);

            // Act
            var result = await handler.ExecuteAsync(query, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            result.Value.Count.ShouldBe(0);
        }
    }
}
