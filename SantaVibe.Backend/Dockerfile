# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY SantaVibe.Api/SantaVibe.Api.csproj SantaVibe.Api/
COPY SantaVibe.Tests/SantaVibe.Tests.csproj SantaVibe.Tests/
RUN dotnet restore SantaVibe.Api/SantaVibe.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/SantaVibe.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published files
COPY --from=publish /app/publish .

# Set environment variable for ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SantaVibe.Api.dll"]
