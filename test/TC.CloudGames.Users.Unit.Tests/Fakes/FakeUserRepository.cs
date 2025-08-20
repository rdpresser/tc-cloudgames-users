using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public Task<UserAggregate?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.FromResult<UserAggregate?>(null);
    public Task SaveAsync(UserAggregate aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    ////public Task SaveAsync<TEvent>(UserAggregate aggregate, IEnumerable<EventContext<TEvent, UserAggregate>> contexts, CancellationToken cancellationToken = default) where TEvent : BaseDomainEvent => Task.CompletedTask;
    public Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<UserAggregate>>(new List<UserAggregate>());
    public Task<UserByEmailResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<UserByEmailResponse?>(null);
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default) => Task.FromResult<UserTokenProvider?>(null);
    public Task<IReadOnlyList<UserListResponse>> GetUserListAsync(GetUserListQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UserListResponse>>(new List<UserListResponse>());
    public Task InsertOrUpdateAsync(Guid aggregateId, CancellationToken cancellationToken = default, params object[] events) => Task.CompletedTask;
    public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<UserAggregate> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.FromResult<UserAggregate>(null!);
}
