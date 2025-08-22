using TC.CloudGames.SharedKernel.Application.Handlers;

namespace TC.CloudGames.Users.Unit.Tests.Api.Endpoints.Auth
{
    /// <summary>
    /// Unit tests for LoginEndpoint.
    /// Tests the FastEndpoints endpoint behavior for user authentication.
    /// </summary>
    public class LoginEndpointTests(App App) : TestBase<App>
    {
        /// <summary>
        /// Tests successful user login.
        /// Verifies that when valid credentials are provided,
        /// the endpoint returns a JWT token.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange: Set up test context
            var ep = Factory.Create<LoginEndpoint>();
            var req = new LoginUserCommand("john.smith@gmail.com", "Password123!");
            var res = new LoginUserResponse("jwt-token", "john.smith@gmail.com");

            // Arrange: Mock the command handler
            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.Success(res)));

            // Register the fake handler for testing (required for FastEndpoints)
            fakeHandler.RegisterForTesting();

            // Act: Execute the endpoint
            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert: Verify response properties
            ep.Response.JwtToken.ShouldBe(res.JwtToken);
            ep.Response.Email.ShouldBe(res.Email);

            // Verify the handler was called correctly
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Tests invalid credentials scenario.
        /// Verifies that when invalid credentials are provided,
        /// the endpoint returns appropriate not found response.
        /// </summary>
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsNotFound()
        {
            // Arrange: Set up test context with invalid credentials
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

            // Arrange: Mock the command handler to return not found
            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.NotFound([.. listError.Select(x => x.ErrorMessage)])));

            // Register the fake handler for testing (required for FastEndpoints)
            fakeHandler.RegisterForTesting();

            // Act: Execute the endpoint
            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert: Verify not found response (should have null values)
            ep.Response.JwtToken.ShouldBe(null);
            ep.Response.Email.ShouldBe(null);

            // Verify the handler was called correctly
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Tests validation error handling.
        /// Verifies that when invalid format credentials are provided,
        /// the endpoint returns appropriate validation errors.
        /// </summary>
        [Fact]
        public async Task Login_InvalidEmailFormat_ReturnsValidationError()
        {
            // Arrange: Set up test context with invalid email format
            var ep = Factory.Create<LoginEndpoint>();
            var req = new LoginUserCommand("invalid-email-format", "Password123!");

            var validationErrors = new List<ValidationError>
            {
                new()
                {
                    Identifier = "Email",
                    ErrorMessage = "Invalid email format.",
                    ErrorCode = "Email.Invalid",
                    Severity = ValidationSeverity.Error
                }
            };

            // Arrange: Mock the command handler to return validation errors
            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.That.Matches(c => c.Email == "invalid-email-format"), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.Invalid(validationErrors)));

            // Register the fake handler for testing (required for FastEndpoints)
            fakeHandler.RegisterForTesting();

            // Act: Execute the endpoint
            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert: Verify validation error response
            ep.Response.JwtToken.ShouldBe(null);
            ep.Response.Email.ShouldBe(null);

            // Verify the handler was called correctly
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.That.Matches(c => c.Email == "invalid-email-format"), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Tests empty password scenario.
        /// Verifies that when empty password is provided,
        /// the endpoint returns appropriate validation errors.
        /// </summary>
        [Fact]
        public async Task Login_EmptyPassword_ReturnsValidationError()
        {
            // Arrange: Set up test context with empty password
            var ep = Factory.Create<LoginEndpoint>();
            var req = new LoginUserCommand("john.smith@gmail.com", "");

            var validationErrors = new List<ValidationError>
            {
                new()
                {
                    Identifier = "Password",
                    ErrorMessage = "Password is required.",
                    ErrorCode = "Password.Required",
                    Severity = ValidationSeverity.Error
                }
            };

            // Arrange: Mock the command handler to return validation errors
            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.That.Matches(c => string.IsNullOrEmpty(c.Password)), A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.Invalid(validationErrors)));

            // Register the fake handler for testing (required for FastEndpoints)
            fakeHandler.RegisterForTesting();

            // Act: Execute the endpoint
            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert: Verify validation error response
            ep.Response.JwtToken.ShouldBe(null);
            ep.Response.Email.ShouldBe(null);

            // Verify the handler was called correctly
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.That.Matches(c => string.IsNullOrEmpty(c.Password)), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Tests forbidden scenario.
        /// Verifies that when a forbidden login is attempted,
        /// the endpoint returns appropriate null token response.
        /// </summary>
        [Fact]
        public async Task Login_Forbidden_ReturnsNullToken()
        {
            // Arrange: Set up test context as a regular user (not admin)
            var httpContext = App.GetValidUserContextAccessor("User"); // Not Admin
            var ep = Factory.Create<LoginEndpoint>((httpContext.HttpContext as DefaultHttpContext)!);
            var req = new LoginUserCommand("john.smith@gmail.com", "Password123!");

            // Arrange: Mock the command handler to return unauthorized result
            var fakeHandler = A.Fake<BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>>();
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(Result<LoginUserResponse>.Unauthorized()));

            // Register the fake handler for testing (required for FastEndpoints)
            fakeHandler.RegisterForTesting();

            // Act: Execute the endpoint
            await ep.HandleAsync(req, TestContext.Current.CancellationToken);

            // Assert: Verify response properties (should have null token and email)
            ep.Response.JwtToken.ShouldBe(null);
            ep.Response.Email.ShouldBe(null);

            // Verify the handler was called correctly
            A.CallTo(() => fakeHandler.ExecuteAsync(A<LoginUserCommand>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }
}
