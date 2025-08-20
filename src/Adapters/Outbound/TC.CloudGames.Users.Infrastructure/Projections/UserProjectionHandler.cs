using static TC.CloudGames.Users.Domain.Aggregates.UserAggregate;

namespace TC.CloudGames.Users.Infrastructure.Projections
{
    public class UserProjectionHandler : EventProjection
    {
        public static void Project(UserCreatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                Name = @event.Name,
                Email = @event.Email,
                Username = @event.Username,
                PasswordHash = @event.Password,
                Role = @event.Role,
                CreatedAt = @event.OccurredOn,
                IsActive = true
            };
            operations.Store(projection);
        }

        public static void Project(UserUpdatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                Name = @event.Name,
                Email = @event.Email,
                Username = @event.Username,
                UpdatedAt = @event.OccurredOn,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public static void Project(UserPasswordChangedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                PasswordHash = @event.NewPassword,
                UpdatedAt = @event.OccurredOn,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public static void Project(UserRoleChangedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                Role = @event.NewRole,
                UpdatedAt = @event.OccurredOn,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public static void Project(UserActivatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                IsActive = true,
                UpdatedAt = @event.OccurredOn
            };
            operations.Store(projection);
        }

        public static void Project(UserDeactivatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.AggregateId,
                IsActive = false,
                UpdatedAt = @event.OccurredOn
            };
            operations.Store(projection);
        }
    }
}
