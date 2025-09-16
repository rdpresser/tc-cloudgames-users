namespace TC.CloudGames.Users.Application.UseCases.UpdateUser
{
    public sealed record UpdateUserResponse(
        Guid Id,
        string Name,
        string Email,
        string Username,
        string Role);
}
