using Marten.Events.Projections;

namespace TC.CloudGames.Users.Infrastructure.Projections
{
    public class UserProjectionHandler : EventProjection
    {
        public void Project(UserCreatedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                Name = @event.Name,
                Email = @event.Email,
                Username = @event.Username,
                PasswordHash = @event.Password,
                Role = @event.Role,
                CreatedAt = @event.CreatedAt,
                IsActive = true
            };
            operations.Store(projection);
        }

        public void Project(UserUpdatedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                Name = @event.Name,
                Email = @event.Email,
                Username = @event.Username,
                UpdatedAt = @event.UpdatedAt,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public void Project(UserPasswordChangedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                PasswordHash = @event.NewPassword,
                UpdatedAt = @event.ChangedAt,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public void Project(UserRoleChangedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                Role = @event.NewRole,
                UpdatedAt = @event.ChangedAt,
                IsActive = true // Assume still active unless deactivated event is processed
            };
            operations.Store(projection);
        }

        public void Project(UserActivatedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                IsActive = true,
                UpdatedAt = @event.ActivatedAt
            };
            operations.Store(projection);
        }

        public void Project(UserDeactivatedEvent @event, IDocumentOperations operations)
        {
            var projection = new UserProjection
            {
                Id = @event.Id,
                IsActive = false,
                UpdatedAt = @event.DeactivatedAt
            };
            operations.Store(projection);
        }
    }
}
