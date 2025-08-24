using Polly;
using TC.CloudGames.SharedKernel.Infrastructure.Database.Initializer;

namespace TC.CloudGames.Users.Api.Extensions
{
    public class SchemaInitializationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SchemaInitializationService> _logger;

        public SchemaInitializationService(IServiceProvider serviceProvider, ILogger<SchemaInitializationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔧 Iniciando criação de schema Wolverine...");

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(2 * attempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Tentativa {RetryCount} falhou. Retentando em {Delay}s...", retryCount, timeSpan.TotalSeconds);
                    });

            await policy.ExecuteAsync(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var schemaCreator = scope.ServiceProvider.GetRequiredService<IMessageDatabaseInitializer>();

                await schemaCreator.CreateAsync(cancellationToken);

                _logger.LogInformation("✅ Schema Wolverine criado com sucesso.");
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
