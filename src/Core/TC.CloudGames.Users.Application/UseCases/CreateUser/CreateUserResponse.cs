namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public sealed record CreateUserResponse(
        Guid Id,
        string Name,
        string Email,
        string Username,
        string Role);
}
