# =============================================================================
# Stage 1: Base Runtime Image
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# =============================================================================
# Stage 2: Build Stage
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first (for better caching)
COPY ["Fap.Api/Fap.Api.csproj", "Fap.Api/"]
COPY ["Fap.Domain/Fap.Domain.csproj", "Fap.Domain/"]
COPY ["Fap.Infrastructure/Fap.Infrastructure.csproj", "Fap.Infrastructure/"]

# Restore NuGet packages (cached layer nếu csproj không đổi)
RUN dotnet restore "Fap.Api/Fap.Api.csproj"

# Copy toàn bộ source code
COPY . .

# Build project
WORKDIR "/src/Fap.Api"
RUN dotnet build "Fap.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =============================================================================
# Stage 3: Publish Stage
# =============================================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fap.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# =============================================================================
# Stage 4: Final Runtime Image
# =============================================================================
FROM base AS final
WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Metadata
LABEL maintainer="FAP Team"
LABEL description="FAP Backend API - University Academic & Student Management on Blockchain"
LABEL version="1.0"

# Health check (optional but recommended)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:80/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Fap.Api.dll"]
