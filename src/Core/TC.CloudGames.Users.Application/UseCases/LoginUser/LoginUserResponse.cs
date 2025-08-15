namespace TC.CloudGames.Users.Application.UseCases.LoginUser
{
    public sealed record LoginUserResponse(
        string JwtToken,
        string Email);
}
