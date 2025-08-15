using TC.CloudGames.SharedKernel.Application.Commands;

namespace TC.CloudGames.Users.Application.UseCases.LoginUser
{
    public sealed record LoginUserCommand(
        string Email,
        string Password) : IBaseCommand<LoginUserResponse>;
}
