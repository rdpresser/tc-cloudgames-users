using TC.CloudGames.Users.Application.UseCases.GetUserList;

namespace TC.CloudGames.Users.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<UserAggregate>, IUserRepository
    {
        public UserRepository(IDocumentSession session)
            : base(session)
        {
        }

        public override async Task<IEnumerable<UserAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Use projections directly; IsActive uses duplicated column index
            var userProjections = await Session.Query<UserProjection>()
                .Where(u => u.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

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
            if (string.IsNullOrWhiteSpace(email)) return null;

            var lowerEmail = email.ToLower();

            var projection = await Session.Query<UserProjection>()
                .Where(u => u.IsActive && u.Email.ToLower() == lowerEmail)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Username,
                    x.Email,
                    x.Role,
                    x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

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
            if (string.IsNullOrWhiteSpace(email)) return false;

            var lowerEmail = email.ToLower();

            return await Session.Query<UserProjection>()
                .AnyAsync(u => u.IsActive && u.Email.ToLower() == lowerEmail, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var lowerEmail = email.ToLower();

            var projection = await Session.Query<UserProjection>()
                .Where(u => u.IsActive && u.Email.ToLower() == lowerEmail)
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
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

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
            var usersQuery = Session.Query<UserProjection>()
                .Where(u => u.IsActive);

            // Dynamic filtering (case-insensitive)
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filter = query.Filter.ToLower();
                bool isGuid = Guid.TryParse(filter, out var guid);

                usersQuery = usersQuery.Where(u =>
                    (isGuid && u.Id == guid) ||
                    u.Name.ToLower().Contains(filter) ||
                    u.Username.ToLower().Contains(filter) ||
                    u.Email.ToLower().Contains(filter) ||
                    u.Role.ToLower().Contains(filter));
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

        ////public async Task<UserAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        ////{
        ////    var projection = await Session.Query<UserProjection>()
        ////        .Where(u => u.IsActive && u.Id == id)
        ////        .Select(x => new
        ////        {
        ////            x.Id,
        ////            x.Name,
        ////            x.Email,
        ////            x.Username,
        ////            x.PasswordHash,
        ////            x.Role,
        ////            x.CreatedAt,
        ////            x.UpdatedAt,
        ////            x.IsActive
        ////        })
        ////        .FirstOrDefaultAsync(cancellationToken)
        ////        .ConfigureAwait(false);

        ////    if (projection == null)
        ////        return null;

        ////    return UserAggregate.FromProjection(
        ////        projection.Id,
        ////        projection.Name,
        ////        projection.Email,
        ////        projection.Username,
        ////        projection.PasswordHash,
        ////        projection.Role,
        ////        projection.CreatedAt,
        ////        projection.UpdatedAt,
        ////        projection.IsActive);
        ////}
    }
}
