# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FapBackend.sln ./
COPY Directory.Build.props ./
COPY Fap.Api/Fap.Api.csproj Fap.Api/
COPY Fap.Domain/Fap.Domain.csproj Fap.Domain/
COPY Fap.Infrastructure/Fap.Infrastructure.csproj Fap.Infrastructure/

RUN dotnet restore ./Fap.Api/Fap.Api.csproj

COPY . .
RUN dotnet publish ./Fap.Api/Fap.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Cloud Run / container platforms expect the app to listen on $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENV DOTNET_ReadyToRun=0

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fap.Api.dll"]
