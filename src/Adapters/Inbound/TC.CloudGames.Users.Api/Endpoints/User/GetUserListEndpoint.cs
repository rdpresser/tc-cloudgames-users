using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Api.Endpoints.User
{
    public sealed class GetUserListEndpoint : BaseApiEndpoint<GetUserListQuery, IReadOnlyList<UserListResponse>>
    {
        private static readonly string[] items = [AppConstants.AdminRole, AppConstants.UserRole];

        public override void Configure()
        {
            Get("user");
            Roles(AppConstants.AdminRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetUserListQuery, IReadOnlyList<UserListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetUserListQuery, IReadOnlyList<UserListResponse>>>();

            Description(
                x => x.Produces<UserListResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<UserListResponse> userList = [];
            for (int i = 0; i < 5; i++)
            {
                userList.Add(new UserListResponse
                {
                    Id = Guid.NewGuid(),
                    Name = faker.Name.FullName(),
                    Username = faker.Person.UserName,
                    Email = faker.Internet.Email(),
                    Role = faker.PickRandom(items)
                });
            }

            Summary(s =>
            {
                s.Summary = "Retrieves a paginated list of users based on the provided filters.";
                s.ExampleRequest = new GetUserListQuery(PageNumber: 1, PageSize: 10, SortBy: "id", SortDirection: "asc", Filter: "<any value/field>");
                s.ResponseExamples[200] = userList;
                s.Responses[200] = "Returned when the user list is successfully retrieved using the specified filters.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[403] = "Returned when the logged-in user lacks the required role to access this endpoint.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[404] = "Returned when no users are found matching the specified filters.";
            });
        }

        public override async Task HandleAsync(GetUserListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            // Use the MatchResultAsync method from the base class
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
