using System.Net;
using TC.CloudGames.SharedKernel.Api.EndPoints;
using TC.CloudGames.SharedKernel.Application.Behaviors;
using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;

namespace TC.CloudGames.Users.Api.Endpoints.User;

public sealed class GetUserByEmailEndpoint : BaseApiEndpoint<GetUserByEmailQuery, UserByEmailResponse>
{
    public override void Configure()
    {
        Get("user/by-email/{Email}");
        /*
        Roles(AppConstants.UserRole, AppConstants.AdminRole);
        */
        AllowAnonymous();
        PreProcessor<QueryCachingPreProcessorBehavior<GetUserByEmailQuery, UserByEmailResponse>>();
        PostProcessor<QueryCachingPostProcessorBehavior<GetUserByEmailQuery, UserByEmailResponse>>();

        Description(x => x.Produces<UserByEmailResponse>(200)
            .ProducesProblemDetails()
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Unauthorized));

        Summary(s =>
        {
            s.Summary = "Retrieve user details by their email.";
            s.Description =
                "This endpoint retrieves detailed information about a user by their Email. Access is restricted to users with the appropriate role.";
            s.ExampleRequest = new GetUserByEmailQuery("John.smith@gmail.com");
            s.ResponseExamples[200] = new UserByEmailResponse
            {
                Id = Guid.NewGuid(),
                Name = "John Smith",
                Username = "johnsmith",
                Email = "John.smith@gmail.com",
                Role = "Admin"
            };
            s.Responses[200] = "Returned when user information is successfully retrieved.";
            s.Responses[400] = "Returned when the request is invalid.";
            s.Responses[404] = "Returned when no user is found with the provided Id.";
            s.Responses[403] = "Returned when the caller lacks the required role to access this endpoint.";
            s.Responses[401] = "Returned when the request is made without a valid user token.";
        });
    }

    public override async Task HandleAsync(GetUserByEmailQuery req, CancellationToken ct)
    {
        var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

        await MatchResultAsync(response, ct).ConfigureAwait(false);
    }
}