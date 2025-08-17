global using Ardalis.Result;
global using FastEndpoints;
global using FastEndpoints.Security;
global using FastEndpoints.Swagger;
global using FluentValidation;
global using FluentValidation.Resources;
global using HealthChecks.UI.Client;
global using JasperFx.Events.Projections;
global using Marten;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Caching.StackExchangeRedis;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Newtonsoft.Json.Converters;
global using Serilog;
global using Serilog.Core;
global using Serilog.Enrichers.Sensitive;
global using Serilog.Enrichers.Span;
global using Serilog.Events;
global using Serilog.Sinks.Grafana.Loki;
global using System.Diagnostics.CodeAnalysis;
global using System.Net;
global using TC.CloudGames.SharedKernel.Api.EndPoints;
global using TC.CloudGames.SharedKernel.Application.Behaviors;
global using TC.CloudGames.SharedKernel.Extensions;
global using TC.CloudGames.SharedKernel.Infrastructure.Authentication;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.HealthCheck;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Provider;
global using TC.CloudGames.SharedKernel.Infrastructure.Database;
global using TC.CloudGames.SharedKernel.Infrastructure.Middleware;
global using TC.CloudGames.Users.Api.Middleware;
global using TC.CloudGames.Users.Api.Telemetry;
global using TC.CloudGames.Users.Application.UseCases.CreateUser;
global using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;
global using TC.CloudGames.Users.Infrastructure.Projections;
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
global using ValidationException = TC.CloudGames.Users.Api.Exceptions.ValidationException;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Users.Unit.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// REMARK: Required for functional and integration tests to work.
namespace TC.CloudGames.Users.Api
{
    public partial class Program;
}