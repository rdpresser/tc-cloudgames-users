using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserList
{
    public class UserListResponseTests
    {
        [Fact]
        public void UserListResponse_Properties_ShouldBeSetCorrectly()
        {
            var id = Guid.NewGuid();
            var response = new UserListResponse
            {
                Id = id,
                Name = "Test Name",
                Username = "testuser",
                Email = "test@example.com",
                Role = "Admin"
            };

            response.Id.ShouldBe(id);
            response.Name.ShouldBe("Test Name");
            response.Username.ShouldBe("testuser");
            response.Email.ShouldBe("test@example.com");
            response.Role.ShouldBe("Admin");
        }
    }
}
