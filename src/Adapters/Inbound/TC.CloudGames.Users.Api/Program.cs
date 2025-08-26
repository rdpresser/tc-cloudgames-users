using TC.CloudGames.Users.Api.Extensions;
using TC.CloudGames.Users.Application;
using TC.CloudGames.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
    ////builder.Services.AddScoped<IMessageDatabaseInitializer, MessageDatabaseInitializer>();
}

var app = builder.Build();

////app.MapDefaultEndpoints();

if (!builder.Environment.IsEnvironment("Testing"))
{
    await app.CreateMessageDatabase().ConfigureAwait(false);
}

// Executa inicialização do banco, se não estiver em ambiente de teste
if (!builder.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        ////string? usersDbConnStr = $"{builder.Configuration.GetConnectionString("UsersDbConnection")};Timeout=30;CommandTimeout=30";
        ////string? maintenanceDbConnStr = $"{builder.Configuration.GetConnectionString("MaintenanceDbConnection")};Timeout=30;CommandTimeout=30";
        ////string? mqConnStr = builder.Configuration.GetConnectionString("TC-CloudGames-RabbitMq-Host");
        ////string? cacheConnStr = builder.Configuration.GetConnectionString("TC-CloudGames-Redis-Host");

        ////Console.WriteLine(usersDbConnStr);
        ////Console.WriteLine(maintenanceDbConnStr);
        ////Console.WriteLine(mqConnStr);
        ////Console.WriteLine(cacheConnStr);

        ////var initializer = scope.ServiceProvider.GetRequiredService<IMessageDatabaseInitializer>();
        ////await initializer.CreateAsync(default).ConfigureAwait(false);
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