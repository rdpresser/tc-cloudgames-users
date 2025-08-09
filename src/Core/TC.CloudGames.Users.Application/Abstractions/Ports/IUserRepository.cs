using TC.CloudGames.Users.Domain.Aggregates;

namespace TC.CloudGames.Users.Application.Abstractions.Ports
{
    public interface IUserRepository
    {
        Task<UserAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
