using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Unit.Tests.Fakes
{
    /// <summary>
    /// Fake implementation of IUserRepository for testing purposes.
    /// Maintains an in-memory list of users to simulate repository behavior.
    /// </summary>
    public class FakeUserRepository : IUserRepository
    {
        private readonly List<UserAggregate> _users = new();

        #region IBaseRepository<UserAggregate> Implementations

        public Task<UserAggregate?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == aggregateId));

        public Task<UserAggregate> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.First(u => u.Id == aggregateId));

        public Task SaveAsync(UserAggregate aggregate, CancellationToken cancellationToken = default)
        {
            var existing = _users.FirstOrDefault(u => u.Id == aggregate.Id);
            if (existing != null)
            {
                _users.Remove(existing);
            }
            _users.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task PersistAsync(UserAggregate aggregate, CancellationToken cancellationToken = default)
            => SaveAsync(aggregate, cancellationToken);

        public Task CommitAsync(UserAggregate aggregate, CancellationToken cancellationToken = default)
            => Task.CompletedTask; // In-memory, nothing extra to commit

        public Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<UserAggregate>>(_users);

        public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default)
        {
            _users.RemoveAll(u => u.Id == aggregateId);
            return Task.CompletedTask;
        }

        public Task<UserAggregate?> LoadSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default)
            => GetByIdAsync(aggregateId, cancellationToken);

        public Task<UserByEmailResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return Task.FromResult<UserByEmailResponse?>(null);

            return Task.FromResult<UserByEmailResponse?>(
                new UserByEmailResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Username = user.Username,
                    Email = user.Email.Value,
                    Role = user.Role.Value
                }
            );
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Any(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            // Simulate password check
            var user = _users.FirstOrDefault(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase)
                                                  && u.PasswordHash == password);
            if (user == null) return Task.FromResult<UserTokenProvider?>(null);

            return Task.FromResult<UserTokenProvider?>(
                new UserTokenProvider
                (
                    Id: user.Id,
                    Name: user.Name,
                    Email: user.Email,
                    Username: user.Username,
                    Role: user.Role
                ));
        }

        public Task<IReadOnlyList<UserListResponse>> GetUserListAsync(GetUserListQuery query, CancellationToken cancellationToken = default)
        {
            var list = _users
                .Select(u => new UserListResponse { Id = u.Id, Email = u.Email.Value, Username = u.Username, Name = u.Name, Role = u.Role.Value })
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<UserListResponse>>(list);
        }

        #endregion

        #region Helpers for Tests

        /// <summary>
        /// Adds a user directly to the fake repository (for testing purposes).
        /// </summary>
        public void AddFakeUser(UserAggregate user)
        {
            _users.Add(user);
        }

        /// <summary>
        /// Clears all users from the repository.
        /// </summary>
        public void Clear()
        {
            _users.Clear();
        }

        #endregion
    }
}
