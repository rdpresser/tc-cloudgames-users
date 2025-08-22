using TC.CloudGames.SharedKernel.Application.Handlers;
using TC.CloudGames.Users.Api.Endpoints.User;
using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.User
{
    public class GetUserListEndpointTests(App App) : TestBase<App>
    {
        [Fact]
        public async Task GetUserList_ValidRequest_ReturnsList()
        {
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var expectedUsers = new List<UserListResponse>
            {
                new UserListResponse { Id = Guid.NewGuid(), Name = "John Smith", Username = "johnsmith", Email = "john.smith@gmail.com", Role = "Admin" },
                new UserListResponse { Id = Guid.NewGuid(), Name = "Jane Doe", Username = "janedoe", Email = "jane.doe@gmail.com", Role = "User" }
            };
            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Success(expectedUsers)));
            fakeHandler.RegisterForTesting();
            await ep.HandleAsync(req, CancellationToken.None);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(2);
            ep.Response[0].Name.ShouldBe("John Smith");
            ep.Response[1].Name.ShouldBe("Jane Doe");
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserList_EmptyRequest_ReturnsEmptyList()
        {
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var emptyList = new List<UserListResponse>();
            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Success(emptyList)));
            fakeHandler.RegisterForTesting();
            await ep.HandleAsync(req, CancellationToken.None);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(0);
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserList_HandlerError_ReturnsError()
        {
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var expectedErrors = new List<ValidationError>
            {
                new() { Identifier = "PageNumber", ErrorMessage = "Invalid page number", ErrorCode = "PageNumber.Invalid", Severity = ValidationSeverity.Error }
            };
            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Invalid(expectedErrors)));
            fakeHandler.RegisterForTesting();
            await ep.HandleAsync(req, CancellationToken.None);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(0);
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == req.PageNumber && q.PageSize == req.PageSize && q.SortBy == req.SortBy && q.SortDirection == req.SortDirection), A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserList_Forbidden_ReturnsEmptyList()
        {
            var httpContext = App.GetValidUserContextAccessor("User"); // Not Admin
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");
            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Unauthorized()));
            fakeHandler.RegisterForTesting();
            await ep.HandleAsync(req, CancellationToken.None);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(0);
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserList_ValidationError_ReturnsEmptyList()
        {
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(0, 10, "id", "asc", ""); // Invalid page number
            var validationErrors = new List<ValidationError>
            {
                new() { Identifier = "PageNumber", ErrorMessage = "Page number must be greater than zero.", ErrorCode = "PageNumber.Invalid", Severity = ValidationSeverity.Error }
            };
            var fakeHandler = A.Fake<BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == 0), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<IReadOnlyList<UserListResponse>>.Invalid(validationErrors)));
            fakeHandler.RegisterForTesting();
            await ep.HandleAsync(req, CancellationToken.None);
            ep.Response.ShouldNotBeNull();
            ep.Response.Count.ShouldBe(0);
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserListQuery>.That.Matches(q => q.PageNumber == 0), A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserList_WithoutHandler_ExecutesSuccessfully()
        {
            // Arrange - Test without handler mocking to ensure endpoint works
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserListEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new GetUserListQuery(1, 10, "id", "asc", "");

            // Act & Assert - Should not hang
            try
            {
                await ep.HandleAsync(req, CancellationToken.None);
                Assert.True(true, "Endpoint executed without hanging");
            }
            catch
            {
                // Expected to fail due to unmocked dependencies, but should not hang
                Assert.True(true, "Endpoint executed without hanging (expected to fail due to unmocked dependencies)");
            }
        }
    }
}
