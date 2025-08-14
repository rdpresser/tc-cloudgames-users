using FluentValidation;
using FluentValidation.Resources;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Converters;
using TC.CloudGames.SharedKernel.Extensions;
using TC.CloudGames.SharedKernel.Infrastructure.Caching.HealthCheck;
using TC.CloudGames.SharedKernel.Infrastructure.Caching.Provider;
using TC.CloudGames.SharedKernel.Infrastructure.Database;
using TC.CloudGames.Users.Infrastructure.Authentication;
using TC.CloudGames.Users.Infrastructure.Projections;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace TC.CloudGames.Users.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure FluentValidation globally
            ConfigureFluentValidationGlobals();

            // Add Marten configuration
            services.AddMartenEventSourcing();

            services.AddHttpClient()
                .AddCorrelationIdGenerator();

            //services// Add custom telemetry services
            //    .AddSingleton<UserMetrics>()

            services.AddCaching();

            //services.AddCustomOpenTelemetry()

            services.AddCustomAuthentication(configuration)
                .AddCustomFastEndpoints()
                .ConfigureAppSettings(configuration)
                .AddCustomHealthCheck();

            return services;
        }

        // Health Checks with Enhanced Telemetry
        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                    .AddNpgSql(sp =>
                    {
                        var connectionProvider = sp.GetRequiredService<IConnectionStringProvider>();
                        return connectionProvider.ConnectionString;
                    },
                        name: "PostgreSQL",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: ["db", "sql", "postgres"])
                    .AddTypeActivatedCheck<RedisHealthCheck>("Redis",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: ["cache", "redis"])
                    .AddCheck("Memory", () =>
                    {
                        var allocated = GC.GetTotalMemory(false);
                        var mb = allocated / 1024 / 1024;

                        return mb < 1024
                        ? HealthCheckResult.Healthy($"Memory usage: {mb} MB")
                        : HealthCheckResult.Degraded($"High memory usage: {mb} MB");
                    },
                        tags: ["memory", "system"])
                    .AddCheck("Custom-Metrics", () =>
                    {
                        // Add any custom health logic for your metrics system
                        return HealthCheckResult.Healthy("Custom metrics are functioning");
                    },
                        tags: ["metrics", "telemetry"]);

            return services;
        }

        // Authentication and Authorization
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthenticationJwtBearer(s => s.SigningKey = configuration["Jwt:SecretKey"])
                    .AddAuthorization()
                    .AddHttpContextAccessor();

            return services;
        }

        // FastEndpoints Configuration
        public static IServiceCollection AddCustomFastEndpoints(this IServiceCollection services)
        {
            services.AddFastEndpoints(dicoveryOptions =>
            {
                dicoveryOptions.Assemblies = [typeof(Application.DependencyInjection).Assembly];
            })
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "TC.CloudGames API";
                    s.Version = "v1";
                    s.Description = "API for TC.CloudGames";
                    s.MarkNonNullablePropsAsRequired();
                };

                o.RemoveEmptyRequestSchema = true;
                o.NewtonsoftSettings = s => { s.Converters.Add(new StringEnumConverter()); };
            });

            return services;
        }

        private static IServiceCollection AddCaching(this IServiceCollection services)
        {
            // Add FusionCache for caching
            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromSeconds(20);
                    options.DistributedCacheDuration = TimeSpan.FromSeconds(30);
                })
                .WithDistributedCache(sp =>
                {
                    var cacheProvider = sp.GetRequiredService<ICacheProvider>();

                    var options = new RedisCacheOptions { Configuration = cacheProvider.ConnectionString, InstanceName = cacheProvider.InstanceName };

                    return new RedisCache(options);
                })
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .AsHybridCache();

            return services;
        }

        private static IServiceCollection AddMartenEventSourcing(this IServiceCollection services)
        {
            services.AddMarten(serviceProvider =>
            {
                var connProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();

                var options = new StoreOptions();
                options.Connection(connProvider.ConnectionString);

                // Event Store configuration
                options.Events.DatabaseSchemaName = "events";
                // Document database configuration  
                options.DatabaseSchemaName = "documents";
                // Register UserProjection as a document type so its table is created
                options.Schema.For<UserProjection>().DatabaseSchemaName("documents");
                // Register event projections
                options.Projections.Add<UserProjectionHandler>(ProjectionLifecycle.Inline);

                // Ensure database is created if missing
                options.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(connProvider.MaintenanceConnectionString);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });

                return options;
            })
            .UseLightweightSessions()
            .ApplyAllDatabaseChangesOnStartup();

            return services;
        }

        // FluentValidation Global Setup
        private static void ConfigureFluentValidationGlobals()
        {
            ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
            ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
            ValidatorOptions.Global.ErrorCodeResolver = validator => validator.Name;
            ValidatorOptions.Global.LanguageManager = new LanguageManager
            {
                Enabled = true,
                Culture = new System.Globalization.CultureInfo("en")
            };
        }

        public static IServiceCollection ConfigureAppSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<CacheProviderSettings>(configuration.GetSection("Cache"));

            return services;
        }
    }
}
