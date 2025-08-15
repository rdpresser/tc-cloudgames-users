global using Marten;
global using Microsoft.Extensions.DependencyInjection;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.SharedKernel.Infrastructure.Authentication;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Provider;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Service;
global using TC.CloudGames.SharedKernel.Infrastructure.Clock;
global using TC.CloudGames.SharedKernel.Infrastructure.Database;
global using TC.CloudGames.SharedKernel.Infrastructure.Repositories;
global using TC.CloudGames.Users.Application.Abstractions.Ports;
global using TC.CloudGames.Users.Domain.Aggregates;
global using TC.CloudGames.Users.Infrastructure.Authentication;
global using TC.CloudGames.Users.Infrastructure.Repositories;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Users.Unit.Tests")]