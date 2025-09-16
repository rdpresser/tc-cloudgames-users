using TC.CloudGames.Users.Application.UseCases.UpdateUser;

namespace TC.CloudGames.Users.Api.Endpoints.User
{
    public sealed class UpdateUserEndpoint : BaseApiEndpoint<UpdateUserCommand, UpdateUserResponse>
    {
        public override void Configure()
        {
            // PUT /api/user/{Id}
            Put("user/{Id:guid}");
            Roles(AppConstants.UserRole, AppConstants.AdminRole);
            PostProcessor<LoggingCommandPostProcessorBehavior<UpdateUserCommand, UpdateUserResponse>>();

            Description(x => x
                .Produces<UpdateUserResponse>(200)
                .ProducesProblemDetails()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Update an existing user.";
                s.Description = "Updates an existing user by Id with the provided name, email and username. Accessible only to authorized roles.";
                s.ExampleRequest = new UpdateUserCommand(Guid.NewGuid(), "John Smith", "john.smith@gmail.com", "john.smith");
                s.ResponseExamples[200] = new UpdateUserResponse(Guid.NewGuid(), "John Smith", "john.smith@gmail.com", "john.smith", "Admin");
                s.Responses[200] = "Returned when the user is successfully updated.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[404] = "Returned when no user is found with the provided Id.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[401] = "Returned when authentication is missing or invalid.";
            });
        }

        public override async Task HandleAsync(UpdateUserCommand req, CancellationToken ct)
        {
            var result = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(result, ct).ConfigureAwait(false);
        }
    }
}