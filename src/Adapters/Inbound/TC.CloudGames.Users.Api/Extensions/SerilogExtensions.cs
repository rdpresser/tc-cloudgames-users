using Serilog.Formatting.Json;

namespace TC.CloudGames.Users.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class SerilogExtensions
    {
        public static IHostBuilder UseCustomSerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            return hostBuilder.UseSerilog((hostContext, services, loggerConfiguration) =>
            {
                // Lê configuração padrão (sinks, níveis, overrides)
                loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);

                // Valores úteis
                var environment = configuration["ASPNETCORE_ENVIRONMENT"]?.ToLower() ?? "development";
                var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? TelemetryConstants.Version;
                var instanceId = Environment.MachineName;

                // Timezone (se configurado)
                var timeZoneId = configuration["TimeZone"] ?? "UTC";
                TimeZoneInfo? timeZone = null;
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch
                {
                    // fallback para UTC se TimeZone inválido
                    timeZone = TimeZoneInfo.Utc;
                }

                // --- Enrichers essenciais ---
                // Mantém enriquecimento com span/trace, contexto e propriedades estáticas
                loggerConfiguration.Enrich.With(new UtcToLocalTimeEnricher(timeZone));
                loggerConfiguration.Enrich.WithSpan();           // garante trace_id/span_id quando disponível
                loggerConfiguration.Enrich.FromLogContext();

                // OpenTelemetry semantic conventions / resource consistency
                loggerConfiguration.Enrich.WithProperty("service.name", TelemetryConstants.ServiceName);
                loggerConfiguration.Enrich.WithProperty("service.namespace", TelemetryConstants.ServiceNamespace);
                loggerConfiguration.Enrich.WithProperty("service.version", serviceVersion);
                loggerConfiguration.Enrich.WithProperty("deployment.environment", environment);
                loggerConfiguration.Enrich.WithProperty("cloud.provider", "azure");
                loggerConfiguration.Enrich.WithProperty("cloud.platform", "azure_container_apps");
                loggerConfiguration.Enrich.WithProperty("service.instance.id", instanceId);

                // --- Sinks ---
                // Console em JSON (stdout) — ideal para que o Grafana Agent / Loki colete dos containers
                // Mantemos o JsonFormatter para estrutura consistente e parsing fácil pelo Loki/agent.
                loggerConfiguration.WriteTo.Console(new JsonFormatter(renderMessage: true));

                // OBS: quaisquer outros sinks configurados via appsettings.json permanecem, porque ReadFrom.Configuration()
                // já carregou tudo. Se quiser garantir que NÃO haja sinks extras (ex: file, loki) remova essas seções do config.
            });
        }
    }
}
