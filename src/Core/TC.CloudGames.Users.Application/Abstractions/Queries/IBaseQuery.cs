using TC.CloudGames.Users.Application.Abstractions.Commands;

namespace TC.CloudGames.Users.Application.Abstractions.Queries
{
    public interface IBaseQuery<TResponse> : IBaseCommand<TResponse>
    {
    }
}
