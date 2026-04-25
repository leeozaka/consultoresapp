FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/API/Homeless.Domain/Homeless.Domain.csproj API/Homeless.Domain/
COPY src/API/Homeless.Application/Homeless.Application.csproj API/Homeless.Application/
COPY src/API/Homeless.Infrastructure/Homeless.Infrastructure.csproj API/Homeless.Infrastructure/
COPY src/WORKERS/Homeless.Worker/Homeless.Worker.csproj WORKERS/Homeless.Worker/

RUN dotnet restore WORKERS/Homeless.Worker/Homeless.Worker.csproj

COPY src/API/ API/
COPY src/WORKERS/Homeless.Worker/ WORKERS/Homeless.Worker/

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish WORKERS/Homeless.Worker/Homeless.Worker.csproj -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Homeless.Worker.dll"]
