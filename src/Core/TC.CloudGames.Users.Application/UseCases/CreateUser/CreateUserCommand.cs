namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public sealed record CreateUserCommand(
        string Name,
        string Email,
        string Username,
        string Password,
        string Role) : IBaseCommand<CreateUserResponse>;
}