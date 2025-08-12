using Microsoft.Extensions.DependencyInjection;
using TC.CloudGames.Users.Infrastructure.Repositories;

namespace TC.CloudGames.Users.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
