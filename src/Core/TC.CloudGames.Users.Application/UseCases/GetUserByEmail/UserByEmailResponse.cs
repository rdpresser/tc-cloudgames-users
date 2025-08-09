namespace TC.CloudGames.Users.Application.UseCases.GetUserByEmail
{
    public class UserByEmailResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Username { get; init; }
        public string Email { get; init; }
        public string Role { get; init; }
    }
}
