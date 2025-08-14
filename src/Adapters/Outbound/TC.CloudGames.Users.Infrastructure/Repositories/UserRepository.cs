using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;
using TC.CloudGames.Users.Infrastructure.Projections;

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
                .ToListAsync(cancellationToken);

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
                .Where(u => u.IsActive)
                .Where(u => u.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefaultAsync(cancellationToken);

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

        public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        {
            if (user.UncommittedEvents.Any())
            {
                await base.SaveChangesAsync(user.Id, cancellationToken, [.. user.UncommittedEvents]);
                user.MarkEventsAsCommitted();
            }
        }

        // TODO: Descomentar e implementar o Outbox do Wolverine
        //public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        //{
        //    // 1. Salva o aggregate no event store do Marten
        //    _session.Events.StartStream<UserAggregate>(user.Id, user.UncommittedEvents.ToArray());

        //    // 2. Processa EventEnvelopes para o Outbox do Wolverine
        //    foreach (var eventEnvelope in user.UncommittedEvents.OfType<EventEnvelope<EventContext<UserAggregate>>>())
        //    {
        //        // Envia para o Outbox do Wolverine (mesma transação do Marten)
        //        await _session.SendAsync(eventEnvelope, cancellationToken);
        //    }

        //    // 3. Commit atômico: Event Store + Outbox
        //    await _session.SaveChangesAsync(cancellationToken);

        //    // 4. Marca eventos como commitados
        //    user.MarkEventsAsCommitted();
        //}
    }
}
