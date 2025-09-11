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
                UpdatedAt = null,
                IsActive = true
            };

            operations.Store(projection);
        }

        public static async Task Project(UserUpdatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = await operations.LoadAsync<UserProjection>(@event.AggregateId).ConfigureAwait(false);
            if (projection == null) return;

            projection.Name = @event.Name;
            projection.Email = @event.Email;
            projection.Username = @event.Username;
            projection.UpdatedAt = @event.OccurredOn;

            operations.Store(projection);
        }

        public static async Task Project(UserPasswordChangedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = await operations.LoadAsync<UserProjection>(@event.AggregateId).ConfigureAwait(false);
            if (projection == null) return;

            projection.PasswordHash = @event.NewPassword;
            projection.UpdatedAt = @event.OccurredOn;

            operations.Store(projection);
        }

        public static async Task Project(UserRoleChangedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = await operations.LoadAsync<UserProjection>(@event.AggregateId).ConfigureAwait(false);
            if (projection == null) return;

            projection.Role = @event.NewRole;
            projection.UpdatedAt = @event.OccurredOn;

            operations.Store(projection);
        }

        public static async Task Project(UserActivatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = await operations.LoadAsync<UserProjection>(@event.AggregateId).ConfigureAwait(false);
            if (projection == null) return;

            projection.IsActive = true;
            projection.UpdatedAt = @event.OccurredOn;

            operations.Store(projection);
        }

        public static async Task Project(UserDeactivatedDomainEvent @event, IDocumentOperations operations)
        {
            var projection = await operations.LoadAsync<UserProjection>(@event.AggregateId).ConfigureAwait(false);
            if (projection == null) return;

            projection.IsActive = false;
            projection.UpdatedAt = @event.OccurredOn;

            operations.Store(projection);
        }
    }
}
