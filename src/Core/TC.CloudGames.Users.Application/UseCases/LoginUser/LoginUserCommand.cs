namespace TC.CloudGames.Users.Application.UseCases.LoginUser
{
    public sealed record LoginUserCommand(
        string Email,
        string Password) : IBaseCommand<LoginUserResponse>;
}
