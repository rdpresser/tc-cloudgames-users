using TC.CloudGames.Application.Users.GetUserList;

namespace TC.CloudGames.Users.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<UserAggregate>, IUserRepository
    {
        public UserRepository(IDocumentSession session)
            : base(session)
        {

        }

        public async Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Efficient approach: Use projections directly instead of replaying events
            // Since projections are always up-to-date, this avoids N+1 queries and unnecessary event replay
            var userProjections = await Session.Query<UserProjection>()
                .Where(u => u.IsActive)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return userProjections.Select(projection =>
                UserAggregate.FromProjection(
                    projection.Id,
                    projection.Name,
                    projection.Email,
                    projection.Username,
                    projection.PasswordHash,
                    projection.Role,
                    projection.CreatedAt,
                    projection.UpdatedAt,
                    projection.IsActive));
        }

        public async Task<UserByEmailResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var projection = await Session.Query<UserProjection>()
                .Where(u => u.IsActive && u.Email.ToLower() == email.ToLower())
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Username,
                    x.Email,
                    x.Role,
                    x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (projection == null)
                return null;

            return new UserByEmailResponse
            {
                Id = projection.Id,
                Name = projection.Name,
                Username = projection.Username,
                Email = projection.Email,
                Role = projection.Role
            };
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await Session.Query<UserProjection>()
                .AnyAsync(u => u.IsActive && u.Email.ToLower() == email.ToLower(), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        {
            if (user.UncommittedEvents.Any())
            {
                await base.SaveChangesAsync(user.Id, cancellationToken, [.. user.UncommittedEvents]).ConfigureAwait(false);
                user.MarkEventsAsCommitted();
            }
        }

        public async Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var projection = await Session.Query<UserProjection>()
                .Where(u => u.IsActive && u.Email.ToLower() == email.ToLower())
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Email,
                    x.Username,
                    x.PasswordHash,
                    x.Role,
                    x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (projection == null)
                return null;

            if (!Password.FromHash(projection.PasswordHash).Value.Verify(password))
                return null;

            return new UserTokenProvider(
                projection.Id,
                projection.Name,
                projection.Email,
                projection.Username,
                projection.Role);
        }

        public async Task<IReadOnlyList<UserListResponse>> GetUserListAsync(GetUserListQuery query, CancellationToken cancellationToken = default)
        {
            // Start with active users
            var usersQuery = Session.Query<UserProjection>()
                .Where(u => u.IsActive);

            // Dynamic filtering (search across multiple fields)
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filter = query.Filter.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Name.ToLower().Contains(filter) ||
                    u.Username.ToLower().Contains(filter) ||
                    u.Email.ToLower().Contains(filter) ||
                    u.Role.ToLower().Contains(filter)
                );
            }

            // Dynamic sorting
            usersQuery = query.SortBy.ToLower() switch
            {
                "name" => query.SortDirection.ToLower() == "desc"
                    ? usersQuery.OrderByDescending(u => u.Name)
                    : usersQuery.OrderBy(u => u.Name),
                "username" => query.SortDirection.ToLower() == "desc"
                    ? usersQuery.OrderByDescending(u => u.Username)
                    : usersQuery.OrderBy(u => u.Username),
                "email" => query.SortDirection.ToLower() == "desc"
                    ? usersQuery.OrderByDescending(u => u.Email)
                    : usersQuery.OrderBy(u => u.Email),
                "role" => query.SortDirection.ToLower() == "desc"
                    ? usersQuery.OrderByDescending(u => u.Role)
                    : usersQuery.OrderBy(u => u.Role),
                _ => query.SortDirection.ToLower() == "desc"
                    ? usersQuery.OrderByDescending(u => u.Id)
                    : usersQuery.OrderBy(u => u.Id)
            };

            // Pagination
            usersQuery = usersQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

            // Project to UserListResponse
            var userList = await usersQuery
                .Select(u => new UserListResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return userList;
        }

        // Descomentar e implementar o Outbox do Wolverine
        //public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        // 
        //    // 1. Salva o aggregate no event store do Marten
        //    _session.Events.StartStream<UserAggregate>(user.Id, user.UncommittedEvents.ToArray())

        //    // 2. Processa EventEnvelopes para o Outbox do Wolverine
        //    foreach (var eventEnvelope in user.UncommittedEvents.OfType<EventEnvelope<EventContext<UserAggregate>>>())
        //    
        //        // Envia para o Outbox do Wolverine (mesma transação do Marten)
        //        await _session.SendAsync(eventEnvelope, cancellationToken)
        //    

        //    // 3. Commit atômico: Event Store + Outbox
        //    await _session.SaveChangesAsync(cancellationToken)

        //    // 4. Marca eventos como commitados
        //    user.MarkEventsAsCommitted()
        //
    }
}
