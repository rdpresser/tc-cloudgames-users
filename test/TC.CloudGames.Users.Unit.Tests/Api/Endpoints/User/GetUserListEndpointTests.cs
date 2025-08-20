using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.User
{
    public class GetUserListEndpointTests(App App) : TestBase<App>
    {
        [Fact]
        public async Task GetUserList_ValidRequest_ReturnsList()
        {
            var httpContext = App.GetValidUserContextAccessor();
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var res = new List<UserListResponse>
            {
                new UserListResponse { Id = Guid.NewGuid(), Name = "John Smith", Username = "johnsmith", Email = "john.smith@gmail.com", Role = "Admin" },
                new UserListResponse { Id = Guid.NewGuid(), Name = "Jane Doe", Username = "janedoe", Email = "jane.doe@gmail.com", Role = "User" }
            };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Success(res)));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(2);
            ep.Response[0].Name.ShouldBe("John Smith");
            ep.Response[1].Name.ShouldBe("Jane Doe");
        }

        [Fact]
        public async Task GetUserList_EmptyRequest_ReturnsEmptyList()
        {
            var httpContext = App.GetValidUserContextAccessor();
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var res = new List<UserListResponse>();

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Success(res)));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(0);
        }
    }
}
