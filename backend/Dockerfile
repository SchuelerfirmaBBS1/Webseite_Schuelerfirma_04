# syntax=docker/dockerfile:1
#
# Images for the backend. Build context: backend/
#   target "api"     (default) – the Web API, listens on port 8080 as a non-root user
#   target "migrate"           – runs the EF Core migrations once and exits (migration bundle)
# deploy/compose.yaml builds both; see README "Deployment (Docker)".

ARG DOTNET_VERSION=10.0

# ---- build: restore and publish the API ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Restore first so the package layer stays cached as long as the project file does not change.
COPY src/Schuelerfirma.Api/Schuelerfirma.Api.csproj src/Schuelerfirma.Api/
RUN dotnet restore src/Schuelerfirma.Api/Schuelerfirma.Api.csproj

COPY src/ src/
RUN dotnet publish src/Schuelerfirma.Api/Schuelerfirma.Api.csproj \
        --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- bundle: EF Core migration bundle --------------------------------------------------------------
FROM build AS bundle
# Keep in line with the Microsoft.EntityFrameworkCore version in Schuelerfirma.Api.csproj.
ARG DOTNET_EF_VERSION=9.0.14
RUN dotnet tool install dotnet-ef --version ${DOTNET_EF_VERSION} --tool-path /tools
# The tool targets an older runtime than the SDK image ships, hence the roll-forward. The bundle reads
# the connection string from ConnectionStrings__Default (DesignTimeDbContextFactory), so it never
# appears on a command line.
RUN DOTNET_ROLL_FORWARD=Major /tools/dotnet-ef migrations bundle \
        --project src/Schuelerfirma.Api --configuration Release --output /app/efbundle

# ---- runtime base ----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# ---- migrate ---------------------------------------------------------------------------------------
FROM runtime AS migrate
COPY --from=bundle /app/efbundle ./efbundle
# The bundle is a single-file app that extracts itself on start; the root file system is read-only
# (compose), so it extracts into the /tmp tmpfs.
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---- api (default target, must stay the last stage) ------------------------------------------------
FROM runtime AS api

# Writable data directories. They belong to the non-root user, so named volumes mounted here
# inherit that owner on first use. Everything else in the container stays read-only (compose).
RUN mkdir -p /var/lib/schuelerfirma/keys \
    && chown -R "$APP_UID:$APP_UID" /var/lib/schuelerfirma

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/var/lib/schuelerfirma/keys

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Schuelerfirma.Api.dll"]
