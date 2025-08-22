using TC.CloudGames.SharedKernel.Application.Handlers;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.Auth
{
    public class CreateUserEndpointTests() : TestBase<App>
    {
        [Fact]
        public async Task CreateUser_ValidRequest_ReturnsCreated()
        {
            var ep = Factory.Create<CreateUserEndpoint>();
            var req = new CreateUserCommand("John", "Smith", "john.smith@gmail.com", "Password123!", "Admin");
            var res = new CreateUserResponse(Guid.NewGuid(), "John", "Smith", "john.smith@gmail.com", "Admin");

            var fakeHandler = A.Fake<BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<CreateUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<CreateUserResponse>.Success(res)));

            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);
            ep.Response.Id.ShouldBe(res.Id);
            ep.Response.Name.ShouldBe(res.Name);
            ep.Response.Email.ShouldBe(res.Email);
            ep.Response.Username.ShouldBe(res.Username);
            ep.Response.Role.ShouldBe(res.Role);
        }

        [Fact]
        public async Task CreateUser_InvalidRequest_ReturnsBadRequest()
        {
            var ep = Factory.Create<CreateUserEndpoint>();
            var req = new CreateUserCommand("", "", "invalid", "", "InvalidRole");

            var listError = new List<ValidationError>
            {
                new() {
                    Identifier = "Password",
                    ErrorMessage = "Password must be at least 8 characters long.",
                    ErrorCode = "Password.MinimumLength"
                },
                new() {
                    Identifier = "Role",
                    ErrorMessage = "Invalid role specified.",
                    ErrorCode = "Role.InvalidRole"
                },
                new() {
                    Identifier = "Email",
                    ErrorMessage = "Invalid email format.",
                    ErrorCode = "Email.InvalidFormat"
                }
            };

            var fakeHandler = A.Fake<BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<CreateUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<CreateUserResponse>.Invalid(listError)));

            fakeHandler.RegisterForTesting();

            await ep.HandleAsync(req, TestContext.Current.CancellationToken);
            // Assert
            ep.Response.Id.ShouldBe(Guid.Empty);
            ep.Response.Name.ShouldBeNull();
            ep.Response.Email.ShouldBeNull();
            ep.Response.Username.ShouldBeNull();
            ep.Response.Role.ShouldBeNull();
        }
    }
}
