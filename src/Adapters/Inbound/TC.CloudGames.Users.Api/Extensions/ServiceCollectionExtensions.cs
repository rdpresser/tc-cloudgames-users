using JasperFx.Resources;
using TC.CloudGames.Contracts.Events.Users;
using TC.CloudGames.SharedKernel.Infrastructure.MessageBroker;
using TC.CloudGames.SharedKernel.Infrastructure.Messaging;
using TC.CloudGames.Users.Domain.Aggregates;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.Marten;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Wolverine.Runtime.Routing;

namespace TC.CloudGames.Users.Api.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            // Configure FluentValidation globally
            ConfigureFluentValidationGlobals();

            // Add Marten configuration only if not testing
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                services.AddMartenEventSourcing();
                builder.AddWolverineMessaging();
            }

            services.AddHttpClient()
                .AddCorrelationIdGenerator()
                .AddCaching()
                .AddCustomAuthentication(builder.Configuration)
                .AddCustomFastEndpoints()
                .ConfigureAppSettings(builder.Configuration)
                .AddCustomHealthCheck();

            //services// Add custom telemetry services
            //    .AddSingleton<UserMetrics>()
            //services.AddCustomOpenTelemetry()

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

        // 2) Configure Wolverine messaging with RabbitMQ transport and durable outbox
        private static WebApplicationBuilder AddWolverineMessaging(this WebApplicationBuilder builder)
        {
            builder.Host.UseWolverine(opts =>
            {
                // --------------------------------------------------
                // Envelope customizer + routing convention
                // --------------------------------------------------
                opts.Services.AddSingleton<IEnvelopeCustomizer, GenericEventContextEnvelopeCustomizer>();
                opts.Services.AddSingleton<IMessageRoutingConvention, EventContextRoutingConvention>();

                // --------------------------------------------------
                // Durable local queues e outbox
                // --------------------------------------------------
                opts.Policies.UseDurableLocalQueues();
                opts.Policies.AutoApplyTransactions();

                // --------------------------------------------------
                // Load broker configuration
                // --------------------------------------------------
                var broker = MessageBrokerHelper.Build(builder.Configuration);

                if (broker.Type == BrokerType.RabbitMQ && broker.RabbitMqSettings != null)
                {
                    var mq = broker.RabbitMqSettings;

                    // Configure RabbitMQ connection
                    var rabbitOpts = opts.UseRabbitMq(factory =>
                    {
                        factory.Uri = new Uri(mq.ConnectionString);
                        factory.VirtualHost = mq.VirtualHost;

                        // Add client metadata
                        factory.ClientProperties["application"] = "TC.CloudGames.Users.Api";
                        factory.ClientProperties["environment"] = builder.Environment.EnvironmentName;
                    });

                    if (mq.AutoProvision)
                        rabbitOpts.AutoProvision(); // call separately

                    // Apply feature toggles
                    if (mq.UseQuorumQueues)
                        rabbitOpts.UseQuorumQueues(); // call separately

                    if (mq.AutoPurgeOnStartup)
                        rabbitOpts.AutoPurgeOnStartup(); // call separately

                    // Register messages
                    opts.PublishMessage<EventContext<UserCreatedIntegrationEvent, UserAggregate>>()
                        .ToRabbitExchange(mq.Exchange);
                    opts.PublishMessage<EventContext<UserUpdatedIntegrationEvent, UserAggregate>>()
                        .ToRabbitExchange(mq.Exchange);
                    opts.PublishMessage<EventContext<UserRoleChangedIntegrationEvent, UserAggregate>>()
                        .ToRabbitExchange(mq.Exchange);
                    opts.PublishMessage<EventContext<UserActivatedIntegrationEvent, UserAggregate>>()
                        .ToRabbitExchange(mq.Exchange);
                    opts.PublishMessage<EventContext<UserDeactivatedIntegrationEvent, UserAggregate>>()
                        .ToRabbitExchange(mq.Exchange);

                    // Publish all messages to the configured exchange
                    opts.PublishAllMessages()
                        .ToRabbitExchange(mq.Exchange);

                    // Durable outbox for all sending endpoints
                    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
                }
                else if (broker.Type == BrokerType.AzureServiceBus && broker.ServiceBusSettings != null)
                {
                    var sb = broker.ServiceBusSettings;

                    // Configure Azure Service Bus connection
                    var azureOpts = opts.UseAzureServiceBus(sb.ConnectionString);

                    if (sb.AutoProvision)
                        azureOpts.AutoProvision();

                    // Apply feature toggles
                    if (sb.AutoPurgeOnStartup)
                        azureOpts.AutoPurgeOnStartup();

                    if (sb.UseControlQueues)
                        azureOpts.EnableWolverineControlQueues();

                    // Publish all messages to a Topic with durable outbox
                    opts.PublishAllMessages()
                        .ToAzureServiceBusTopic(sb.TopicName)
                        .BufferedInMemory();

                    // Durable outbox for all sending endpoints
                    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
                }

                // --------------------------------------------------
                // Persist Wolverine messages in Postgres
                // --------------------------------------------------
                opts.PersistMessagesWithPostgresql(
                    PostgresHelper.Build(builder.Configuration).ConnectionString,
                    "wolverine");
            });

            // Create all messaging resources + Postgres schema at startup
            builder.Services.AddResourceSetupOnStartup();

            return builder;
        }

        // 1) Configure Marten with event sourcing, projections, and Wolverine integration
        private static IServiceCollection AddMartenEventSourcing(this IServiceCollection services)
        {
            services.AddMarten(serviceProvider =>
            {
                var connProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();

                var options = new StoreOptions();
                options.Connection(connProvider.ConnectionString);
                options.Logger(new ConsoleMartenLogger()); // optional: log SQL for debugging

                // Event Store configuration (events schema)
                options.Events.DatabaseSchemaName = "events";

                // Document store configuration (documents schema)
                options.DatabaseSchemaName = "documents";

                // Register projection documents
                options.Schema.For<UserProjection>().DatabaseSchemaName("documents");

                // Register inline projections
                options.Projections.Add<UserProjectionHandler>(ProjectionLifecycle.Inline);

                // Auto-create databases/schemas if missing
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
            .UseLightweightSessions() // optional, lightweight sessions for better performance
            .IntegrateWithWolverine() // enables transactional outbox + inbox with Wolverine
            .ApplyAllDatabaseChangesOnStartup(); // optional, automatically applies schema changes at startup

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
            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
            services.Configure<PostgresOptions>(configuration.GetSection("Database"));
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<CacheProviderSettings>(configuration.GetSection("Cache"));

            return services;
        }
    }
}
