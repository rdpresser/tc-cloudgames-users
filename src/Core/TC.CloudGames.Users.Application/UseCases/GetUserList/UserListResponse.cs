namespace TC.CloudGames.Application.Users.GetUserList
{
    public sealed class UserListResponse
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Username { get; init; }
        public required string Email { get; init; }
        public required string Role { get; init; }
    }
}
