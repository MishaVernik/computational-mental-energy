FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy Cme.Core project first (required dependency)
COPY Cme.Core/Cme.Core.csproj Cme.Core/
COPY Cme.Core/*.cs Cme.Core/

# Copy API csproj and restore dependencies (from root context)
COPY CmeSim.Api/CmeSim.Api.csproj CmeSim.Api/
WORKDIR /src/CmeSim.Api
RUN dotnet restore

# Copy remaining API files and build
COPY CmeSim.Api/ .
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 5000

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=30s \
    CMD curl -f http://localhost:5000/api/dashboard/summary || exit 1

ENTRYPOINT ["dotnet", "CmeSim.Api.dll"]


