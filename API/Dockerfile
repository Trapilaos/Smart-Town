# syntax=docker/dockerfile:1
# Build context is the repository root (global.json and the solution live there):
#   docker build -f API/Dockerfile -t smart-town-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file changes.
COPY global.json ./
COPY API/API.csproj API/
RUN dotnet restore API/API.csproj

COPY API/ API/
RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# SQLite lives on a volume so data survives container replacement.
# The aspnet image ships a non-root "app" user; $APP_UID is its numeric id.
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
VOLUME /data

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/smarttown.db"
# TokenKey (>= 64 chars) must be supplied at run time, e.g. -e TokenKey=...

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
