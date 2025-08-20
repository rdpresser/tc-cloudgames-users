using TC.CloudGames.Contracts.Events.Users;
using static TC.CloudGames.Users.Domain.Aggregates.UserAggregate;

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
                Email: e.Email.Value,
                Role: e.Role.Value
            );
        }

        public static UserCreatedIntegrationEvent ToIntegrationEvent(UserCreatedDomainEvent domainEvent)
        => new(
            domainEvent.AggregateId,
            domainEvent.Name,
            domainEvent.Email.Value,
            domainEvent.Username,
            domainEvent.Role.ToString(),
            domainEvent.OccurredOn
        );
    }
}
