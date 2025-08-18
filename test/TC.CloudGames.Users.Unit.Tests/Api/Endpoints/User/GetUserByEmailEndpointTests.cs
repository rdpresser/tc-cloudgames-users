namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.User
{
    public class GetUserByEmailEndpointTests(App App) : TestBase<App>
    {
        [Fact]
        public async Task GetUserByEmail_ValidEmail_ReturnsUser()
        {
            var httpContext = App.GetValidUserContextAccessor();
            var userContext = App.GetValidLoggedUser();

            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: "user@user.com");
            var getUserRes = new UserByEmailResponse
            {
                Id = userContext.Id,
                Name = "Regular User",
                Username = "regularuser",
                Email = getUserReq.Email,
                Role = "User"
            };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.Success(getUserRes)));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(getUserReq, TestContext.Current.CancellationToken);

            ep.Response.Id.ShouldBe(getUserRes.Id);
            ep.Response.Name.ShouldBe(getUserRes.Name);
            ep.Response.Username.ShouldBe(getUserRes.Username);
            ep.Response.Email.ShouldBe(getUserRes.Email);
            ep.Response.Role.ShouldBe(getUserRes.Role);

            var result = await fakeHandler.ExecuteAsync(getUserReq, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.IsInvalid().ShouldBeFalse();
            result.ValidationErrors.Count().ShouldBe(0);
            result.Value.ShouldNotBeNull();
            result.Errors.Count().ShouldBe(0);
        }

        [Fact]
        public async Task GetUserByEmail_ValidEmail_ReturnsNotFound()
        {
            var httpContext = App.GetValidUserContextAccessor("User");

            var ep = Factory.Create<GetUserByEmailEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var getUserReq = new GetUserByEmailQuery(Email: "fake.doe@test.com");

            var listError = new List<ValidationError>
            {
                new()
                {
                    Identifier = "Email",
                    ErrorMessage = $"User with email '{getUserReq.Email}' not found.",
                    ErrorCode = UserDomainErrors.NotFound.ErrorCode,
                    Severity = ValidationSeverity.Error
                }
            };

            var fakeHandler = A.Fake<BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<GetUserByEmailQuery>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<UserByEmailResponse>.NotFound([.. listError.Select(x => x.ErrorMessage)])));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(getUserReq, TestContext.Current.CancellationToken);

            ep.Response.Id.ShouldBe(Guid.Empty);
            ep.Response.Name.ShouldBeNull();
            ep.Response.Username.ShouldBeNull();
            ep.Response.Email.ShouldBeNull();
            ep.Response.Role.ShouldBeNull();

            var result = await fakeHandler.ExecuteAsync(getUserReq, CancellationToken.None);
            result.IsSuccess.ShouldBeFalse();
            result.IsInvalid().ShouldBeFalse();
            result.IsNotFound().ShouldBeTrue();
            result.ValidationErrors.Count().ShouldBe(0);
            result.Value.ShouldBeNull();
            result.Errors.Count().ShouldBe(1);
        }
    }
}
