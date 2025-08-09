using TC.CloudGames.Users.Application.Abstractions.Queries;

namespace TC.CloudGames.Users.Application.Abstractions.Ports
{
    public interface ICachedQuery<TResponse> : IBaseQuery<TResponse>, ICachedQuery;

    public interface ICachedQuery
    {
        string CacheKey { get; set; }

        TimeSpan? Duration { get; }
        TimeSpan? DistributedCacheDuration { get; }
    }
}
