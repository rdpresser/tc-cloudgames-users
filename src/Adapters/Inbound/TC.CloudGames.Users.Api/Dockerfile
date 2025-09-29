# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Directory.Build.props first (build optimization)
COPY services/users/Directory.Build.props ./

# Copy shared common projects (required for ProjectReferences)
COPY ["shared/common/src/TC.CloudGames.Messaging/TC.CloudGames.Messaging.csproj", "shared/common/src/TC.CloudGames.Messaging/"]
COPY ["shared/common/src/TC.CloudGames.SharedKernel/TC.CloudGames.SharedKernel.csproj", "shared/common/src/TC.CloudGames.SharedKernel/"]
COPY ["shared/common/src/TC.CloudGames.Contracts/TC.CloudGames.Contracts.csproj", "shared/common/src/TC.CloudGames.Contracts/"]

# Copy project files maintaining directory structure (required for ProjectReferences)
COPY ["services/users/src/Adapters/Inbound/TC.CloudGames.Users.Api/TC.CloudGames.Users.Api.csproj", "services/users/src/Adapters/Inbound/TC.CloudGames.Users.Api/"]
COPY ["services/users/src/Core/TC.CloudGames.Users.Application/TC.CloudGames.Users.Application.csproj", "services/users/src/Core/TC.CloudGames.Users.Application/"]
COPY ["services/users/src/Core/TC.CloudGames.Users.Domain/TC.CloudGames.Users.Domain.csproj", "services/users/src/Core/TC.CloudGames.Users.Domain/"]
COPY ["services/users/src/Adapters/Outbound/TC.CloudGames.Users.Infrastructure/TC.CloudGames.Users.Infrastructure.csproj", "services/users/src/Adapters/Outbound/TC.CloudGames.Users.Infrastructure/"]

# Restore packages
RUN dotnet restore "services/users/src/Adapters/Inbound/TC.CloudGames.Users.Api/TC.CloudGames.Users.Api.csproj"

# Copy source code maintaining structure
COPY . .

# Build the application
WORKDIR "/src/services/users/src/Adapters/Inbound/TC.CloudGames.Users.Api"
RUN dotnet build "TC.CloudGames.Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TC.CloudGames.Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TC.CloudGames.Users.Api.dll"]