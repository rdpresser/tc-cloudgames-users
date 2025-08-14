using Microsoft.Extensions.DependencyInjection;
using TC.CloudGames.SharedKernel.Infrastructure.Authentication;
using TC.CloudGames.SharedKernel.Infrastructure.Caching.Provider;
using TC.CloudGames.SharedKernel.Infrastructure.Clock;
using TC.CloudGames.SharedKernel.Infrastructure.Database;
using TC.CloudGames.Users.Infrastructure.Authentication;
using TC.CloudGames.Users.Infrastructure.Repositories;

namespace TC.CloudGames.Users.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddScoped<IUserContext, UserContext>();

            return services;
        }
    }
}
