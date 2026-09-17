FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/EventHub.Domain/EventHub.Domain.csproj          src/EventHub.Domain/
COPY src/EventHub.Infrastructure/EventHub.Infrastructure.csproj src/EventHub.Infrastructure/
COPY src/EventHub.Api/EventHub.Api.csproj                src/EventHub.Api/
RUN dotnet restore src/EventHub.Api/EventHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/EventHub.Api/EventHub.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN adduser --disabled-password --no-create-home --uid 10001 appuser
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
ENTRYPOINT ["dotnet", "EventHub.Api.dll"]
