using TC.CloudGames.SharedKernel.Infrastructure.Database.Initializer;
using TC.CloudGames.Users.Api.Extensions;
using TC.CloudGames.Users.Application;
using TC.CloudGames.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotNetEnv.Env.Load(Path.Combine("./", ".env"));

// Configure Serilog as logging provider
builder.Host.UseCustomSerilog(builder.Configuration);

//***************** ADICIONAR **************************************************/
//builder.AddCustomLoggingTelemetry()
//********************************************************************************/

// Register application, infrastructure and API services
builder.Services.AddUserServices(builder);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<IMessageDatabaseInitializer, MessageDatabaseInitializer>();
}

var app = builder.Build();

// Executa inicialização do banco, se não estiver em ambiente de teste
if (!builder.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<IMessageDatabaseInitializer>();
        await initializer.CreateAsync(default).ConfigureAwait(false);
    }
}

// Get logger instance for Program and log telemetry configuration
//***************** ADICIONAR **************************************************/
//var logger = app.Services.GetRequiredService<ILogger<TC.CloudGames.Users.Api.Program>>()
//TelemetryConstants.LogTelemetryConfiguration(logger)
//********************************************************************************/

// Use metrics authentication middleware extension
app.UseMetricsAuthentication();

app.UseAuthentication()
  .UseAuthorization()
  .UseCustomFastEndpoints()
  .UseCustomMiddlewares();

// Run the application
await app.RunAsync().ConfigureAwait(false);