FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src
COPY FinOpsDemoApp/FinOpsDemoApp.csproj FinOpsDemoApp/
RUN dotnet restore FinOpsDemoApp/FinOpsDemoApp.csproj
COPY . .
RUN dotnet publish FinOpsDemoApp/FinOpsDemoApp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=builder /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV APP_MODE=exporter
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "FinOpsDemoApp.dll"]
