namespace TC.CloudGames.Users.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(IDocumentSession session)
            : base(session)
        {

        }

        public Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<UserAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await GetByIdAsync<UserAggregate>(id, cancellationToken);
        }

        public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        {
            if (user.UncommittedEvents.Any())
            {
                await base.SaveChangesAsync<UserAggregate>(user.Id, cancellationToken, [.. user.UncommittedEvents]);
                user.MarkEventsAsCommitted();
            }
        }
    }
}
