using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Wolverine.ErrorHandling;
using TC.CloudGames.SharedKernel.Infrastructure.Telemetry;

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
                .AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>()
                .AddCaching()
                .AddCustomAuthentication(builder.Configuration)
                .AddCustomFastEndpoints()
                .ConfigureAppSettings(builder.Configuration)
                .AddCustomHealthCheck()
                .AddCustomOpenTelemetry(builder.Configuration);

            return services;
        }

        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? TelemetryConstants.Version;
            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
            var instanceId = Environment.MachineName;
            var serviceName = TelemetryConstants.ServiceName;
            var serviceNamespace = TelemetryConstants.ServiceNamespace;

            // Carrega configuração do Grafana via Helper (para verificar se Agent está habilitado)
            var grafanaSettings = GrafanaHelper.Build(configuration);

            // ==============================================================
            // MÉTRICAS (Prometheus Exporter)
            // ==============================================================
            services.AddOpenTelemetry()
                // Configura o ResourceBuilder (metadados enviados com métricas e traces)
                .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.ToLowerInvariant(),
                    ["service.namespace"] = serviceNamespace.ToLowerInvariant(),
                    ["service.instance.id"] = instanceId,
                    ["container.name"] = Environment.GetEnvironmentVariable("HOSTNAME") ?? instanceId,
                    ["cloud.provider"] = "azure",
                    ["cloud.platform"] = "azure_kubernetes_service",
                    ["service.team"] = "engineering",
                    ["service.owner"] = "devops"
                }))
                .WithMetrics(metrics =>
                {
                    metrics
                        // Instrumentações padrão do .NET e ASP.NET Core
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()   // CPU, memória, GC
                        .AddFusionCacheInstrumentation()
                        .AddNpgsqlInstrumentation()
                        // Meters internos da plataforma
                        .AddMeter("Microsoft.AspNetCore.Hosting")
                        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                        .AddMeter("System.Net.Http")
                        .AddMeter("System.Runtime")
                        // Meters específicos da aplicação
                        .AddMeter("Wolverine")
                        .AddMeter("Marten")
                        .AddMeter(TelemetryConstants.UsersMeterName)
                        // Exporter Prometheus (para /metrics)
                        .AddPrometheusExporter();
                })

                // ==============================================================
                // TRACES (OTLP Exporter para Grafana Agent)
                // Apenas configurado se Grafana Agent estiver habilitado
                // ==============================================================
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = ctx =>
                            {
                                var path = ctx.Request.Path.Value ?? "";
                                return !path.Contains("/health") &&
                                       !path.Contains("/metrics") &&
                                       !path.Contains("/prometheus");
                            };

                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.method", request.Method);
                                activity.SetTag("http.scheme", request.Scheme);
                                activity.SetTag("http.host", request.Host.Value);
                                activity.SetTag("http.target", request.Path);
                                if (request.ContentLength.HasValue)
                                    activity.SetTag("http.request.size", request.ContentLength.Value);

                                activity.SetTag("user.id", request.HttpContext.User?.Identity?.Name);
                                activity.SetTag("user.authenticated", request.HttpContext.User?.Identity?.IsAuthenticated);
                                activity.SetTag("http.route", request.HttpContext.GetRouteValue("action")?.ToString());
                                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                            };

                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.status_code", response.StatusCode);
                                if (response.ContentLength.HasValue)
                                    activity.SetTag("http.response.size", response.ContentLength.Value);
                            };

                            options.EnrichWithException = (activity, ex) =>
                            {
                                activity.SetTag("exception.type", ex.GetType().Name);
                                activity.SetTag("exception.message", ex.Message);
                                activity.SetTag("exception.stacktrace", ex.StackTrace);
                            };
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.FilterHttpRequestMessage = request =>
                            {
                                var path = request.RequestUri?.AbsolutePath ?? "";
                                return !path.Contains("/health") &&
                                       !path.Contains("/metrics") &&
                                       !path.Contains("/prometheus");
                            };
                        })
                        .AddFusionCacheInstrumentation()
                        .AddNpgsql()
                        // Custom sources (Wolverine, Marten, Users)
                        .AddSource(TelemetryConstants.UserActivitySource)
                        .AddSource(TelemetryConstants.DatabaseActivitySource)
                        .AddSource(TelemetryConstants.CacheActivitySource);

                    // ========================================================
                    // OTLP Exporter (CONDICIONAL - apenas se Agent habilitado)
                    // ========================================================
                    if (grafanaSettings.Agent.Enabled)
                    {
                        tracing.AddOtlpExporter(otlp =>
                        {
                            otlp.Endpoint = new Uri(grafanaSettings.Otlp.Endpoint);
                            otlp.Protocol = grafanaSettings.Otlp.Protocol.ToLowerInvariant() == "grpc"
                                ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                                : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

                            // Headers (se houver - normalmente não precisa com Grafana Agent local)
                            if (!string.IsNullOrWhiteSpace(grafanaSettings.Otlp.Headers))
                            {
                                otlp.Headers = grafanaSettings.Otlp.Headers;
                            }

                            // Timeout
                            otlp.TimeoutMilliseconds = grafanaSettings.Otlp.TimeoutSeconds * 1000;
                        });

                        // Log via Console.WriteLine (evita BuildServiceProvider)
                        Console.WriteLine($"[INFO] OTLP Exporter configured - Endpoint: {grafanaSettings.Otlp.Endpoint}, Protocol: {grafanaSettings.Otlp.Protocol}");
                    }
                    else
                    {
                        // Log via Console.WriteLine (evita BuildServiceProvider)
                        Console.WriteLine("[WARN] Grafana Agent is DISABLED - Traces will be generated but NOT exported.");
                        Console.WriteLine("[WARN] To enable: Set Grafana:Agent:Enabled=true or GRAFANA_AGENT_ENABLED=true");
                    }
                });

            // ==============================================================
            // REGISTROS CUSTOM DE MÉTRICAS
            // ==============================================================
            services.AddSingleton<UserMetrics>();
            services.AddSingleton<SystemMetrics>();

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
                    tags: ["db", "sql", "postgres", "live", "ready"])
                .AddTypeActivatedCheck<RedisHealthCheck>("Redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["cache", "redis", "live", "ready"])
                .AddCheck("Memory", () =>
                {
                    var allocated = GC.GetTotalMemory(false);
                    var mb = allocated / 1024 / 1024;

                    return mb < 1024
                    ? HealthCheckResult.Healthy($"Memory usage: {mb} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {mb} MB");
                },
                    tags: ["memory", "system", "live"])
                .AddCheck("Custom-Metrics", () =>
                {
                    // Add any custom health logic for your metrics system
                    return HealthCheckResult.Healthy("Custom metrics are functioning");
                },
                    tags: ["metrics", "telemetry", "live"]);

            return services;
        }

        // Authentication and Authorization
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

            services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtSettings!.SecretKey)
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
                    s.Title = "TC.CloudGames.Users API";
                    s.Version = "v1";
                    s.Description = "User API for TC.CloudGames";
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
                // -------------------------------
                // Define schema for Wolverine durability and Postgres persistence
                // -------------------------------

                opts.UseSystemTextJsonForSerialization();
                opts.ApplicationAssembly = typeof(Program).Assembly;

                const string wolverineSchema = "wolverine";
                opts.Durability.MessageStorageSchemaName = wolverineSchema;
                opts.ServiceName = "tccloudgames";

                // -------------------------------
                // Persist Wolverine messages in Postgres using the same schema
                // -------------------------------
                opts.PersistMessagesWithPostgresql(
                    PostgresHelper.Build(builder.Configuration).ConnectionString,
                    wolverineSchema
                );

                ////opts.Policies.OnException<Exception>().RetryTimes(5);
                opts.Policies.OnAnyException()
                    .RetryWithCooldown(
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromMilliseconds(400),
                        TimeSpan.FromMilliseconds(600),
                        TimeSpan.FromMilliseconds(800),
                        TimeSpan.FromMilliseconds(1000)
                    );

                // -------------------------------
                // Enable durable local queues and auto transaction application
                // -------------------------------
                opts.Policies.UseDurableLocalQueues();
                opts.Policies.AutoApplyTransactions();

                // -------------------------------
                // Load and configure message broker
                // -------------------------------
                var broker = MessageBrokerHelper.Build(builder.Configuration);

                switch (broker.Type)
                {
                    case BrokerType.RabbitMQ when broker.RabbitMqSettings is { } mq:
                        var rabbitOpts = opts.UseRabbitMq(factory =>
                        {
                            factory.Uri = new Uri(mq.ConnectionString);
                            factory.VirtualHost = mq.VirtualHost;
                            factory.ClientProperties["application"] = "TC.CloudGames.Users.Api";
                            factory.ClientProperties["environment"] = builder.Environment.EnvironmentName;
                        });

                        if (mq.AutoProvision) rabbitOpts.AutoProvision();
                        if (mq.UseQuorumQueues) rabbitOpts.UseQuorumQueues();
                        if (mq.AutoPurgeOnStartup) rabbitOpts.AutoPurgeOnStartup();

                        // Durable outbox 
                        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

                        var exchangeName = $"{mq.Exchange}-exchange";
                        // Register messages
                        opts.PublishMessage<EventContext<UserCreatedIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        opts.PublishMessage<EventContext<UserUpdatedIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        opts.PublishMessage<EventContext<UserRoleChangedIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        opts.PublishMessage<EventContext<UserActivatedIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        opts.PublishMessage<EventContext<UserDeactivatedIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        break;

                    case BrokerType.AzureServiceBus when broker.ServiceBusSettings is { } sb:
                        var azureOpts = opts.ConfigureAzureServiceBus(sb, builder.Environment);

                        if (sb.AutoProvision) azureOpts.AutoProvision();
                        if (sb.AutoPurgeOnStartup) azureOpts.AutoPurgeOnStartup();
                        if (sb.UseControlQueues)
                        {
                            azureOpts.EnableWolverineControlQueues();
                            azureOpts.SystemQueuesAreEnabled(true);
                        }

                        azureOpts.UseTopicAndSubscriptionConventionalRouting(configure =>
                        {
                            configure.SubscriptionNameForListener(t => t.Name.ToLowerInvariant());
                            configure.TopicNameForListener(t => t.Name.ToLowerInvariant());
                            configure.TopicNameForSender(t => t.Name.ToLowerInvariant());
                        });

                        // Durable outbox for all sending endpoints
                        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
                        var topicName = $"{sb.TopicName}-topic";

                        opts.RegisterUserEvents();

                        opts.PublishMessage<EventContext<UserCreatedIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        opts.PublishMessage<EventContext<UserUpdatedIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        opts.PublishMessage<EventContext<UserRoleChangedIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        opts.PublishMessage<EventContext<UserActivatedIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        opts.PublishMessage<EventContext<UserDeactivatedIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        ////opts.PublishAllMessages()
                        ////    .ToAzureServiceBusTopic(topicName)
                        ////    .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "UserAggregate")
                        ////    .BufferedInMemory()
                        ////    .UseDurableOutbox();

                        break;
                }
            });

            // -------------------------------
            // Ensure all messaging resources and schema are created at startup
            // -------------------------------
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

                options.UseSystemTextJsonForSerialization(configure: cfg =>
                {
                    cfg.Converters.Add(new EmailJsonConverter());
                    cfg.Converters.Add(new PasswordJsonConverter());
                    cfg.Converters.Add(new RoleJsonConverter());

                    // Configurações extras, se necessário
                    ////cfg.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    ////cfg.WriteIndented = true;
                });

                // Event Store configuration (events schema)
                options.Events.DatabaseSchemaName = "events";

                // Document store configuration (documents schema)
                options.DatabaseSchemaName = "documents";

                // Register inline projections - for queries with index
                options.Projections.Add<UserProjectionHandler>(ProjectionLifecycle.Inline);

                // Snapshot automático do aggregate (para acelerar LoadAsync)
                options.Projections.Snapshot<UserAggregate>(SnapshotLifecycle.Inline);

                // Auto-create databases/schemas
                options.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(connProvider.MaintenanceConnectionString);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });

                // Duplicated fields for filtering and DateTime
                options.Schema.For<UserProjection>()
                    .DatabaseSchemaName("documents")
                    .Duplicate(x => x.Email, pgType: "varchar(255)")
                    .Duplicate(x => x.IsActive, pgType: "boolean")
                    .Duplicate(x => x.CreatedAt, pgType: "timestamptz")
                    .Duplicate(x => x.UpdatedAt, pgType: "timestamptz");

                // Computed indexes (case-insensitive) for text search
                options.Schema.For<UserProjection>()
                    .Index(x => x.Email, x => { x.Casing = ComputedIndex.Casings.Lower; x.IsUnique = true; x.Method = IndexMethod.btree; })
                    .Index(x => x.Username, x => { x.Casing = ComputedIndex.Casings.Lower; x.Method = IndexMethod.btree; })
                    .Index(x => x.Name, x => { x.Casing = ComputedIndex.Casings.Lower; x.Method = IndexMethod.btree; }); // full-text

                // GIN index on JSONB
                options.Schema.For<UserProjection>().GinIndexJsonData();

                return options;
            })
            .UseLightweightSessions() // optional, lightweight sessions for better performance
            .IntegrateWithWolverine(cfg => // enables transactional outbox + inbox with Wolverine
            {
                cfg.UseWolverineManagedEventSubscriptionDistribution = true;
            })
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
            services.Configure<AzureServiceBusOptions>(configuration.GetSection("AzureServiceBus"));
            services.Configure<PostgresOptions>(configuration.GetSection("Database"));
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<CacheProviderSettings>(configuration.GetSection("Cache"));
            services.Configure<GrafanaOptions>(configuration.GetSection("Grafana"));

            return services;
        }
    }
}