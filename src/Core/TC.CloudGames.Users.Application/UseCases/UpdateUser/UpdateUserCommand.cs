namespace TC.CloudGames.Users.Application.UseCases.UpdateUser
{
    public sealed record UpdateUserCommand(
        Guid Id,
        string Name,
        string Email,
        string Username) : IBaseCommand<UpdateUserResponse>;
}
