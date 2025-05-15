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
COPY ["SampleApp.Api/SampleApp.Api.csproj", "SampleApp.Api/"]
COPY ["Application.Contracts/Application.Contracts.csproj", "Application.Contracts/"]
COPY ["Domain.Model/Domain.Model.csproj", "Domain.Model/"]
COPY ["Domain.Shared/Domain.Shared.csproj", "Domain.Shared/"]
COPY ["Package/Package.Infrastructure.Data/Package.Infrastructure.Data.csproj", "Package/Package.Infrastructure.Data/"]
COPY ["Package/Package.Infrastructure.Common/Package.Infrastructure.Common.csproj", "Package/Package.Infrastructure.Common/"]
COPY ["Package/Package.Infrastructure.Common.Contracts/Package.Infrastructure.Common.Contracts.csproj", "Package/Package.Infrastructure.Common.Contracts/"]
COPY ["Package/Package.Infrastructure.Data.Contracts/Package.Infrastructure.Data.Contracts.csproj", "Package/Package.Infrastructure.Data.Contracts/"]
COPY ["Package/Package.Infrastructure.AspNetCore/Package.Infrastructure.AspNetCore.csproj", "Package/Package.Infrastructure.AspNetCore/"]
COPY ["Package/Package.Infrastructure.Grpc/Package.Infrastructure.Grpc.csproj", "Package/Package.Infrastructure.Grpc/"]
COPY ["Package/Package.Infrastructure.Host/Package.Infrastructure.Host.csproj", "Package/Package.Infrastructure.Host/"]
COPY ["SampleApp.Bootstrapper/SampleApp.Bootstrapper.csproj", "SampleApp.Bootstrapper/"]
COPY ["Application.MessageHandlers/Application.MessageHandlers.csproj", "Application.MessageHandlers/"]
COPY ["Package/Package.Infrastructure.BackgroundService/Package.Infrastructure.BackgroundServices.csproj", "Package/Package.Infrastructure.BackgroundService/"]
COPY ["Application.Services/Application.Services.csproj", "Application.Services/"]
COPY ["Domain.Rules/Domain.Rules.csproj", "Domain.Rules/"]
COPY ["Infrastructure.JobsApi/Infrastructure.JobsApi.csproj", "Infrastructure.JobsApi/"]
COPY ["Infrastructure.Repositories/Infrastructure.Repositories.csproj", "Infrastructure.Repositories/"]
COPY ["Infrastructure.Data/Infrastructure.Data.csproj", "Infrastructure.Data/"]
COPY ["Infrastructure.SampleApi/Infrastructure.SampleApi.csproj", "Infrastructure.SampleApi/"]
COPY ["Package/Package.Infrastructure.Auth/Package.Infrastructure.Auth.csproj", "Package/Package.Infrastructure.Auth/"]
COPY ["Package/Package.Infrastructure.AzureOpenAI/Package.Infrastructure.AzureOpenAI.csproj", "Package/Package.Infrastructure.AzureOpenAI/"]
COPY ["Package/Package.Infrastructure.Cache/Package.Infrastructure.Cache.csproj", "Package/Package.Infrastructure.Cache/"]
COPY ["Infrastructure.Configuration/Infrastructure.Configuration.csproj", "Infrastructure.Configuration/"]
COPY ["Infrastructure.RapidApi/Infrastructure.RapidApi.csproj", "Infrastructure.RapidApi/"]
COPY ["Package/Package.Infrastructure.BlandAI/Package.Infrastructure.BlandAI.csproj", "Package/Package.Infrastructure.BlandAI/"]
COPY ["Package/Package.Infrastructure.Messaging/Package.Infrastructure.Messaging.csproj", "Package/Package.Infrastructure.Messaging/"]
COPY ["Package/Package.Infrastructure.Storage/Package.Infrastructure.Storage.csproj", "Package/Package.Infrastructure.Storage/"]
COPY ["SampleApp.BackgroundServices/SampleApp.BackgroundServices.csproj", "SampleApp.BackgroundServices/"]
COPY ["SampleApp.Grpc/SampleApp.Grpc.csproj", "SampleApp.Grpc/"]
COPY ["SampleApp.Support/SampleApp.Support.csproj", "SampleApp.Support/"]
RUN dotnet restore "./SampleApp.Api/SampleApp.Api.csproj"
COPY . .
WORKDIR "/src/SampleApp.Api"
RUN dotnet build "./SampleApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SampleApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleApp.Api.dll"]