# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled-extra AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SampleApp.Gateway/SampleApp.Gateway.csproj", "SampleApp.Gateway/"]
COPY ["Package/Package.Infrastructure.AspNetCore/Package.Infrastructure.AspNetCore.csproj", "Package/Package.Infrastructure.AspNetCore/"]
COPY ["Package/Package.Infrastructure.Auth/Package.Infrastructure.Auth.csproj", "Package/Package.Infrastructure.Auth/"]
COPY ["Package/Package.Infrastructure.Common/Package.Infrastructure.Common.csproj", "Package/Package.Infrastructure.Common/"]
COPY ["Package/Package.Infrastructure.Common.Contracts/Package.Infrastructure.Common.Contracts.csproj", "Package/Package.Infrastructure.Common.Contracts/"]
COPY ["Package/Package.Infrastructure.Host/Package.Infrastructure.Host.csproj", "Package/Package.Infrastructure.Host/"]
RUN dotnet restore "./SampleApp.Gateway/SampleApp.Gateway.csproj"
COPY . .
WORKDIR "/src/SampleApp.Gateway"
RUN dotnet build "./SampleApp.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SampleApp.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleApp.Gateway.dll"]