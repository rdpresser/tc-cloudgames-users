using TC.CloudGames.Users.Application.UseCases.LoginUser;

namespace TC.CloudGames.Users.Api.Endpoints.Auth
{
    internal sealed class LoginEndpoint : BaseApiEndpoint<LoginUserCommand, LoginUserResponse>
    {
        public override void Configure()
        {
            Post("login");
            RoutePrefixOverride("auth");
            PostProcessor<LoggingCommandPostProcessorBehavior<LoginUserCommand, LoginUserResponse>>();

            AllowAnonymous();
            Description(
                x => x.Produces<LoginUserResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound));

            Summary(s =>
            {
                s.Summary = "Generates a new authentication token for a user.";
                s.Description = "This endpoint allows a user to log in by providing valid credentials and returns a JWT token upon successful authentication.";
                s.ExampleRequest = new LoginUserCommand("John.smith@gmail.com", "********");
                s.ResponseExamples[200] = new LoginUserResponse("<jwt-token>", "John.smith@gmail.com");
                s.Responses[200] = "Returned when the user is successfully authenticated and a token is generated.";
                s.Responses[400] = "Returned when the request is invalid or contains errors.";
                s.Responses[404] = "Returned when the user credentials are not found.";
            });
        }

        public override async Task HandleAsync(LoginUserCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
