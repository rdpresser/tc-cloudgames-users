using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public Task<UserAggregate?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.FromResult<UserAggregate?>(null);
    public Task SaveAsync(UserAggregate aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PersistAsync(UserAggregate aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<UserAggregate>>(new List<UserAggregate>());
    public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<UserAggregate> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default) => Task.FromResult<UserAggregate>(null!);
    public Task<UserByEmailResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<UserByEmailResponse?>(null);
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default) => Task.FromResult<UserTokenProvider?>(null);
    public Task<IReadOnlyList<UserListResponse>> GetUserListAsync(GetUserListQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UserListResponse>>(new List<UserListResponse>());
}
