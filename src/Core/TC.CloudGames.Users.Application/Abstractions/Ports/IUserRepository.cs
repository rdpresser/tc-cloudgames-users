using TC.CloudGames.SharedKernel.Infrastructure.Authentication;
using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;

namespace TC.CloudGames.Users.Application.Abstractions.Ports
{
    public interface IUserRepository : IBaseRepository<UserAggregate>
    {
        Task<UserByEmailResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<UserTokenProvider?> GetUserTokenInfoAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
