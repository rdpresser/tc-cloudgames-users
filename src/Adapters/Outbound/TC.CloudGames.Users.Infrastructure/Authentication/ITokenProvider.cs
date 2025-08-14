namespace TC.CloudGames.Users.Infrastructure.Authentication
{
    public interface ITokenProvider
    {
        string Create(UserTokenProvider user);
    }
}
