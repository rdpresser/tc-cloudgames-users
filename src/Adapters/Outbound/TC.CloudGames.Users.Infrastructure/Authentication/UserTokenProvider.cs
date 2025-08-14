namespace TC.CloudGames.Users.Infrastructure.Authentication
{
    public sealed record UserTokenProvider(Guid Id, string FirstName, string LastName, string Email, string Role);
}
