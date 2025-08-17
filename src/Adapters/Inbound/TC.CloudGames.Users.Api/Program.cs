using TC.CloudGames.Users.Api.Extensions;
using TC.CloudGames.Users.Application;
using TC.CloudGames.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load(Path.Combine("./", ".env"));

builder.Host.UseCustomSerilog(builder.Configuration);


//***************** ADICIONAR **************************************************/
//builder.AddCustomLoggingTelemetry()
//********************************************************************************/

builder.Services.AddUserServices(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

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

await app.RunAsync();