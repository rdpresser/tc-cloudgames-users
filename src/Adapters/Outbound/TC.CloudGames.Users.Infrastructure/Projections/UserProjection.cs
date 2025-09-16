namespace TC.CloudGames.Users.Infrastructure.Projections
{
    public class UserProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = Domain.ValueObjects.Role.User.Value;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
    }
}
