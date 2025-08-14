global using Ardalis.Result;
global using FastEndpoints;
global using FastEndpoints.Security;
global using FastEndpoints.Swagger;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Serilog;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Users.Unit.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// REMARK: Required for functional and integration tests to work.
namespace TC.CloudGames.Users.Api
{
    public partial class Program;
}