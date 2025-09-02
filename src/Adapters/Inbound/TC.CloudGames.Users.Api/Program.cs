using TC.CloudGames.Users.Api.Extensions;
using TC.CloudGames.Users.Application;
using TC.CloudGames.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

//**************************************************************
// Add services default using sharedKernel project
////builder.AddServiceDefaults();

// Load environment variables from .env files
var environment = builder.Environment.EnvironmentName.ToLowerInvariant();

// Find project root by looking for solution file or git directory
var projectRoot = FindProjectRoot() ?? Directory.GetCurrentDirectory();

// Load base .env file first (if exists)
var baseEnvFile = Path.Combine(projectRoot, ".env");
if (File.Exists(baseEnvFile))
{
    DotNetEnv.Env.Load(baseEnvFile);
    Console.WriteLine($"Loaded base .env from: {baseEnvFile}");
}

// Load environment-specific .env file (overrides base values)
var envFile = Path.Combine(projectRoot, $".env.{environment}");
if (File.Exists(envFile))
{
    DotNetEnv.Env.Load(envFile);
    Console.WriteLine($"Loaded {environment} .env from: {envFile}");
}
else
{
    Console.WriteLine($"Environment file not found: {envFile}");
}

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
////if (!builder.Environment.IsEnvironment("Testing"))
////{
////    using (var scope = app.Services.CreateScope())
////    {
////        ////string? usersDbConnStr = $"{builder.Configuration.GetConnectionString("UsersDbConnection")};Timeout=30;CommandTimeout=30";
////        ////string? maintenanceDbConnStr = $"{builder.Configuration.GetConnectionString("MaintenanceDbConnection")};Timeout=30;CommandTimeout=30";
////        ////string? mqConnStr = builder.Configuration.GetConnectionString("TC-CloudGames-RabbitMq-Host");
////        ////string? cacheConnStr = builder.Configuration.GetConnectionString("TC-CloudGames-Redis-Host");

////        ////Console.WriteLine(usersDbConnStr);
////        ////Console.WriteLine(maintenanceDbConnStr);
////        ////Console.WriteLine(mqConnStr);
////        ////Console.WriteLine(cacheConnStr);

////        ////var initializer = scope.ServiceProvider.GetRequiredService<IMessageDatabaseInitializer>();
////        ////await initializer.CreateAsync(default).ConfigureAwait(false);
////    }
////}

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

static string? FindProjectRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory != null)
    {
        // Look for common project root indicators
        if (directory.GetFiles("*.sln").Length > 0 ||
            directory.GetDirectories(".git").Length > 0 ||
            HasEnvFiles(directory))
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    return null;
}

static bool HasEnvFiles(DirectoryInfo directory)
{
    return directory.GetFiles(".env").Length > 0 ||
           directory.GetFiles(".env.*").Length > 0;
}