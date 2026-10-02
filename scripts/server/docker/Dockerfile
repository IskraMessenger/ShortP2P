# ShortP2P Messenger Server — framework-dependent image (ASP.NET Core 10).
# Build context must be the repository root.
#
#   docker build -f scripts/server/docker/Dockerfile -t shortp2p-messengerserver .
#
# Or via compose / up scripts under scripts/server/docker.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json nuget.config Directory.Build.props Directory.Build.targets ./
COPY ShortP2P.Crypto ./ShortP2P.Crypto
COPY src/Shared ./src/Shared
COPY src/Server/Directory.Build.props ./src/Server/
COPY src/Server ./src/Server
COPY src/ShortP2P.TrustSystem ./src/ShortP2P.TrustSystem

RUN dotnet restore src/Server/ShortP2P.MessengerServer.Api/ShortP2P.MessengerServer.Api.csproj

RUN dotnet publish src/Server/ShortP2P.MessengerServer.Api/ShortP2P.MessengerServer.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /var/lib/shortp2p/data /etc/shortp2p/certs \
    && chmod 755 /var/lib/shortp2p /var/lib/shortp2p/data /etc/shortp2p /etc/shortp2p/certs

COPY --from=build /app/publish .
COPY scripts/server/docker/appsettings.Production.json ./appsettings.Production.json
COPY scripts/server/docker/entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production \
    INTERNAL_PORT=51111 \
    HOST_PORT=51111 \
    SHORTP2P_DATA_DIR=/var/lib/shortp2p/data \
    SHORTP2P_CERT_DIR=/etc/shortp2p/certs

EXPOSE 51111

VOLUME ["/var/lib/shortp2p/data", "/etc/shortp2p/certs"]

ENTRYPOINT ["/entrypoint.sh"]
