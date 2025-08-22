using TC.CloudGames.SharedKernel.Application.Handlers;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.Auth
{
    public class LoginEndpointTests(App App) : TestBase<App>
    {
        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var ep = Factory.Create<LoginEndpoint>();
            var req = new LoginUserCommand("john.smith@gmail.com", "Password123!");
            var res = new LoginUserResponse("jwt-token", "john.smith@gmail.com");

            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.Success(res)));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);
            ep.Response.JwtToken.ShouldBe(res.JwtToken);
            ep.Response.Email.ShouldBe(res.Email);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsNotFound()
        {
            var httpContext = App.GetValidUserContextAccessor();
            var ep = Factory.Create<LoginEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new LoginUserCommand("notfound@email.com", "WrongPassword");

            var listError = new List<ValidationError>
            {
                new()
                {
                    Identifier = UserDomainErrors.InvalidCredentials.Property,
                    ErrorMessage = UserDomainErrors.InvalidCredentials.ErrorMessage,
                    ErrorCode = UserDomainErrors.InvalidCredentials.ErrorCode,
                    Severity = ValidationSeverity.Error
                }
            };

            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.NotFound([.. listError.Select(x => x.ErrorMessage)])));
            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert
            ep.Response.JwtToken.ShouldBe(null);
            ep.Response.Email.ShouldBe(null);
        }
    }
}
