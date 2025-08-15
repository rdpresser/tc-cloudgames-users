namespace TC.CloudGames.Users.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddScoped<IUserContext, UserContext>();

            return services;
        }
    }
}
