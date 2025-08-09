namespace TC.CloudGames.Users.Application.Abstractions.Commands
{
    public interface IBaseCommand : ICommand<Result>
    {
    }

    public interface IBaseCommand<TResponse> : ICommand<Result<TResponse>>
    {
    }
}
