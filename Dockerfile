FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/API/Homeless.Domain/Homeless.Domain.csproj API/Homeless.Domain/
COPY src/API/Homeless.Application/Homeless.Application.csproj API/Homeless.Application/
COPY src/API/Homeless.Infrastructure/Homeless.Infrastructure.csproj API/Homeless.Infrastructure/
COPY src/API/Homeless.API/Homeless.API.csproj API/Homeless.API/

RUN dotnet restore API/Homeless.API/Homeless.API.csproj

COPY src/API/ API/

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish API/Homeless.API/Homeless.API.csproj -c ${BUILD_CONFIGURATION} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Homeless.API.dll"]
