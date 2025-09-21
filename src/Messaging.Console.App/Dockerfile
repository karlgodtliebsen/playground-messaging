# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Messaging.Console.App/Messaging.Console.App.csproj", "Messaging.Console.App/"]
COPY ["Messaging.Domain.Library/Messaging.Domain.Library.csproj", "Messaging.Domain.Library/"]
COPY ["Messaging.EventHub.Library/Messaging.EventHub.Library.csproj", "Messaging.EventHub.Library/"]
COPY ["Messaging.Library/Messaging.Library.csproj", "Messaging.Library/"]
COPY ["Messaging.Kafka.Library/Messaging.Kafka.Library.csproj", "Messaging.Kafka.Library/"]
COPY ["Messaging.RabbitMq.Library/Messaging.RabbitMq.Library.csproj", "Messaging.RabbitMq.Library/"]
COPY ["Messaging.ServiceDefaults/Messaging.ServiceDefaults.csproj", "Messaging.ServiceDefaults/"]
RUN dotnet restore "./Messaging.Console.App/Messaging.Console.App.csproj"
COPY . .
WORKDIR "/src/Messaging.Console.App"
RUN dotnet build "./Messaging.Console.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Messaging.Console.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Messaging.Console.App.dll"]