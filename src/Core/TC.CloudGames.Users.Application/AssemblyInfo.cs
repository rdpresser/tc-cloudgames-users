global using Ardalis.Result;
global using FastEndpoints;
global using FluentValidation;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.Contracts.Events;
global using TC.CloudGames.Contracts.Events.Users;
global using TC.CloudGames.SharedKernel.Application.Commands;
global using TC.CloudGames.SharedKernel.Application.Ports;
global using TC.CloudGames.SharedKernel.Domain.Aggregate;
global using TC.CloudGames.SharedKernel.Domain.Events;
global using TC.CloudGames.SharedKernel.Extensions;
global using TC.CloudGames.SharedKernel.Infrastructure.Messaging;
global using TC.CloudGames.SharedKernel.Infrastructure.UserClaims;
global using TC.CloudGames.Users.Application.Abstractions;
global using TC.CloudGames.Users.Application.Abstractions.Mappers;
global using TC.CloudGames.Users.Application.Abstractions.Ports;
global using TC.CloudGames.Users.Domain.Aggregates;
global using TC.CloudGames.Users.Domain.ValueObjects;
global using static TC.CloudGames.Users.Domain.Aggregates.UserAggregate;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("TC.CloudGames.Users.Unit.Tests")]