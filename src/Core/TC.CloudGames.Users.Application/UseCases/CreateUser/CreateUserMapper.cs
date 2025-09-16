namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public static class CreateUserMapper
    {
        public static Result<UserAggregate> ToAggregate(
            CreateUserCommand r)
        {
            return CreateFromPrimitives(
                r.Name,
                r.Email,
                r.Username,
                r.Password,
                r.Role);
        }

        public static CreateUserResponse FromAggregate(UserAggregate e)
        {
            return new CreateUserResponse
            (
                Id: e.Id,
                Name: e.Name,
                Username: e.Username,
                Email: e.Email,
                Role: e.Role
            );
        }

        public static UserCreatedIntegrationEvent ToIntegrationEvent(UserCreatedDomainEvent domainEvent)
        => new(
            domainEvent.AggregateId,
            domainEvent.Name,
            domainEvent.Email,
            domainEvent.Username,
            domainEvent.Role,
            domainEvent.OccurredOn
        );
    }
}
