# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Messaging.Kafka.WebApi/Messaging.Kafka.WebApi.csproj", "Messaging.Kafka.WebApi/"]
COPY ["Libraries/Messaging.Domain.Library/Messaging.Domain.Library.csproj", "Libraries/Messaging.Domain.Library/"]
COPY ["MemoryMapped/MemoryMapped.Queue/MemoryMapped.Queue.csproj", "MemoryMapped/MemoryMapped.Queue/"]
COPY ["Libraries/Messaging.EventHub.Library/Messaging.EventHub.Library.csproj", "Libraries/Messaging.EventHub.Library/"]
COPY ["Libraries/Messaging.Library/Messaging.Library.csproj", "Libraries/Messaging.Library/"]
COPY ["Libraries/Messaging.Kafka.Library/Messaging.Kafka.Library.csproj", "Libraries/Messaging.Kafka.Library/"]
COPY ["Aspire/Messaging.ServiceDefaults/Messaging.ServiceDefaults.csproj", "Aspire/Messaging.ServiceDefaults/"]
RUN dotnet restore "./Messaging.Kafka.WebApi/Messaging.Kafka.WebApi.csproj"
COPY . .
WORKDIR "/src/Messaging.Kafka.WebApi"
RUN dotnet build "./Messaging.Kafka.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Messaging.Kafka.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Messaging.Kafka.WebApi.dll"]