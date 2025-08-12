namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public static class CreateUserMapper
    {
        public static Result<UserAggregate> ToEntity(
            CreateUserCommand r,
            string? userId = null,
            string? correlationId = null,
            string? source = null)
        {
            //// Cria EventContext primeiro (sem aggregate ainda)
            //var eventContext = EventContext<UserCreatedEvent>.Create(
            //    data: null!, // Será preenchido depois
            //    eventType: "UserCreated",
            //    userId: userId,
            //    correlationId: correlationId,
            //    source: source ?? "UserRegistrationAPI"
            //);

            return UserAggregate.Create(
                r.Name,
                r.Email,
                r.Username,
                r.Password,
                r.Role);
            //eventContext);
        }

        public static CreateUserResponse FromEntity(UserAggregate e)
        {
            return new CreateUserResponse
            (
                Id: e.Id,
                Name: e.Name,
                Username: e.Username,
                Email: e.Email.Value,
                Role: e.Role.Value
            );
        }
    }
}
