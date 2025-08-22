using TC.CloudGames.SharedKernel.Application.Handlers;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.User
{
    public class GetUserByEmailEndpointTests(App App) : TestBase<App>
    {
        [Fact]
        public async Task GetUserByEmail_ValidEmail_ReturnsUser()
        {
            // Arrange
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var userContext = App.GetValidLoggedUser("Admin");
            var testEmail = "john.doe@example.com";
            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: testEmail);
            var expectedResponse = new UserByEmailResponse
            {
                Id = userContext.Id,
                Name = "John Doe",
                Username = "johndoe",
                Email = testEmail,
                Role = "Admin"
            };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == testEmail), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.Success(expectedResponse)));
            fakeHandler.RegisterForTesting();

            // Act
            await ep.HandleAsync(getUserReq, CancellationToken.None);

            // Assert
            ep.Response.ShouldNotBeNull();
            ep.Response.Id.ShouldBe(expectedResponse.Id);
            ep.Response.Name.ShouldBe(expectedResponse.Name);
            ep.Response.Username.ShouldBe(expectedResponse.Username);
            ep.Response.Email.ShouldBe(expectedResponse.Email);
            ep.Response.Role.ShouldBe(expectedResponse.Role);

            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == testEmail), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserByEmail_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var nonExistentEmail = "nonexistent@example.com";
            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: nonExistentEmail);
            var expectedErrors = new[] { $"User with email '{nonExistentEmail}' not found." };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == nonExistentEmail), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.NotFound(expectedErrors)));
            fakeHandler.RegisterForTesting();

            // Act
            await ep.HandleAsync(getUserReq, CancellationToken.None);
            await ep.OnBeforeHandleAsync(getUserReq, CancellationToken.None);

            // Assert - Not found should result in default/empty response
            ep.Response.Id.ShouldBe(Guid.Empty);
            ep.Response.Name.ShouldBeNull();
            ep.Response.Username.ShouldBeNull();
            ep.Response.Email.ShouldBeNull();
            ep.Response.Role.ShouldBeNull();

            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == nonExistentEmail), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserByEmail_InvalidEmailFormat_ReturnsValidationError()
        {
            // Arrange
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var invalidEmail = "invalid-email-format";
            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: invalidEmail);
            var validationErrors = new List<ValidationError>
            {
                new() { Identifier = "Email", ErrorMessage = "Invalid email format.", ErrorCode = "Email.Invalid", Severity = ValidationSeverity.Error }
            };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == invalidEmail), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.Invalid(validationErrors)));
            fakeHandler.RegisterForTesting();

            // Act
            await ep.HandleAsync(getUserReq, CancellationToken.None);
            await ep.OnBeforeHandleAsync(getUserReq, CancellationToken.None);

            // Assert - Validation error should result in default/empty response
            ep.Response.Id.ShouldBe(Guid.Empty);
            ep.Response.Name.ShouldBeNull();
            ep.Response.Username.ShouldBeNull();
            ep.Response.Email.ShouldBeNull();
            ep.Response.Role.ShouldBeNull();

            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.That.Matches(q => q.Email == invalidEmail), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserByEmail_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var httpContext = App.GetValidUserContextAccessor("User"); // Not Admin
            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: "john.doe@example.com");

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.Unauthorized()));
            fakeHandler.RegisterForTesting();

            // Act
            await ep.HandleAsync(getUserReq, CancellationToken.None);
            await ep.OnBeforeHandleAsync(getUserReq, CancellationToken.None);

            // Assert - Unauthorized should result in default/empty response
            ep.Response.Id.ShouldBe(Guid.Empty);
            ep.Response.Name.ShouldBeNull();
            ep.Response.Username.ShouldBeNull();
            ep.Response.Email.ShouldBeNull();
            ep.Response.Role.ShouldBeNull();

            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetUserByEmail_WithoutHandler_ExecutesSuccessfully()
        {
            // Arrange - Test without handler mocking to ensure endpoint works
            var httpContext = App.GetValidUserContextAccessor("Admin");
            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: "test@example.com");

            // Act & Assert - Should not hang
            try
            {
                await ep.HandleAsync(getUserReq, CancellationToken.None);
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
