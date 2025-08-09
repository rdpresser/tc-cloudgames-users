global using Ardalis.Result;
global using FastEndpoints;
global using FluentValidation;
global using FluentValidation.Results;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Serilog.Context;
global using System.Linq.Expressions;
global using TC.CloudGames.SharedKernel.Extensions;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Service;
global using TC.CloudGames.Users.Application.Abstractions.Commands;
global using TC.CloudGames.Users.Application.Abstractions.Ports;
global using TC.CloudGames.Users.Domain.Aggregates;
global using TC.CloudGames.Users.Domain.ValueObjects;
/*
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Unit.Tests")]
[assembly: InternalsVisibleTo("TC.CloudGames.Integration.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("TC.CloudGames.BDD.Tests")]
*/