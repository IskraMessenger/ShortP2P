# Сборка пакетов ShortP2P Messenger Server

Только **отдельная нода** `ShortP2P.MessengerServer.Api`. Клиент не входит.

Версия берётся из `<Version>` / `<InformationalVersion>` в
`src/Server/ShortP2P.MessengerServer.Api/ShortP2P.MessengerServer.Api.csproj`,
иначе из `scripts/server/VERSION` (сейчас `0.1.0`).

Скрипты можно запускать из корня репозитория или из `scripts/server`.

## Windows (Inno Setup)

Нужны .NET SDK и [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```powershell
powershell -File scripts\server\windows\build-installer.ps1
```

Или по шагам:

```powershell
powershell -File scripts\server\publish.ps1 -Rid win-x64
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=0.1.0 scripts\server\windows\shortp2p-messengerserver.iss
```

Результат: `scripts/server/out/installer/shortp2p-messengerserver-<версия>-win-x64.exe`.

Установка (админ):

| Что | Путь |
|-----|------|
| Бинарники | `C:\Program Files\ShortP2P\MessengerServer\` |
| Конфиг и данные | `C:\ProgramData\ShortP2P\MessengerServer\` |
| LiteDB | `...\data\messenger-auth.litedb`, `messenger-trust.litedb`, `messenger-host-powers.litedb` |
| Сертификат | `...\certs\` (пусто, свой PFX) |
| Служба | `ShortP2PMessengerServer`, `ASPNETCORE_ENVIRONMENT=Production` |

Апгрейд не затирает `appsettings.Production.json`, `certs\` и `*.litedb`.
Деинсталлятор не удаляет данные.

## Linux (.deb)

Нужны .NET SDK и (для финального файла) `dpkg-deb`.

```bash
./scripts/server/debian/build-deb.sh
```

Или по шагам:

```bash
./scripts/server/publish.sh linux-x64
./scripts/server/debian/build-deb.sh --skip-publish
```

Результат: `scripts/server/out/deb/shortp2p-messengerserver_<версия>-1_amd64.deb`
(если `dpkg-deb` нет — только дерево `scripts/server/out/deb-staging`).

```bash
sudo dpkg -i scripts/server/out/deb/shortp2p-messengerserver_0.1.0-1_amd64.deb
```

| Что | Путь |
|-----|------|
| Пакет | `shortp2p-messengerserver` |
| Бинарники | `/opt/shortp2p-messengerserver/` |
| Конфиг (conffile) | `/etc/shortp2p/appsettings.Production.json` |
| Данные / LiteDB | `/var/lib/shortp2p/data/` |
| Сертификат | `/etc/shortp2p/certs/` |
| Unit | `shortp2p-messengerserver.service`, `User=shortp2p` |

`postgresql` только в Recommends. `apt remove` данные не трогает, `apt purge` — удаляет.

## Docker Compose

Нужен Docker Engine + Compose v2. Сборка из корня репозитория (образ `shortp2p-messengerserver`).
Маппинг портов `HOST:INTERNAL`, внутренний по умолчанию **51111**.
Лимиты контейнера: память **≥ 512 МБ** (`MEMORY_MB`), CPU **≥ 1** (`CPUS`).

### Release (Production)

```bash
./scripts/server/docker/run.Release.sh
./scripts/server/docker/run.Release.sh 8080 --memory 1024 --cpus 2
./scripts/server/docker/run.Release.sh 8080:51111 -m 512 -c 1
```

```powershell
.\scripts\server\docker\run.Release.ps1
.\scripts\server\docker\run.Release.ps1 8080 -MemoryMb 1024 -Cpus 2
```

| Что | Значение |
|-----|----------|
| Compose | `docker-compose.yml` |
| Env | `.env.example` → `.env` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| Volumes | `shortp2p-data`; certs bind `CERTS_DIR` → `/etc/shortp2p/certs` |

### Development

```bash
./scripts/server/docker/run.Development.sh
./scripts/server/docker/run.Development.sh 8080 --memory 1024 --cpus 2
./scripts/server/docker/run.Development.sh 8080:51111 -m 512 -c 1
```

```powershell
.\scripts\server\docker\run.Development.ps1
.\scripts\server\docker\run.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
```

Swagger: `https://localhost:<HOST_PORT>/swagger`

| Что | Значение |
|-----|----------|
| Compose | `docker-compose.Development.yml` |
| Env | `.env.Development.example` → `.env.Development` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| Volumes | `shortp2p-data-dev`; certs bind `CERTS_DIR` → `/etc/shortp2p/certs` (отдельно от Production data) |

Общее: `Trust:SelfPort` = `HOST_PORT`; listen в контейнере — `INTERNAL_PORT` (default `51111`);
`mem_limit` = `MEMORY_MB` (default `512`), `cpus` = `CPUS` (default `1`).
TLS: `CERTS_DIR` → `/etc/shortp2p/certs` (default `%APPUSER%/ShortP2P/MessengerServer/certs`;
`--certs-dir` / `-CertsDir` / env `CERTS_DIR`).
При первом запуске entrypoint создаёт self-signed PFX и при необходимости `Auth:SigningKey` в data volume.

### Persistent (PostgreSQL 11 companion)

Включает `Persistence:Enabled=true` и контейнер `postgres:11` как **companion** `messengerserver`
на общей Compose-сети (DNS `postgres`). Данные БД — bind-mount на хост; TLS-сертификаты —
отдельный bind-mount (не внутри persistence).

Каталог Postgres по умолчанию: `%APPUSER%/ShortP2P/MessengerServer/persistence` (Release) или `.../persistence-development` (Development).
Каталог TLS по умолчанию: `%APPUSER%/ShortP2P/MessengerServer/certs` (общий для всех Docker run-скриптов).
`APPUSER` → если не задан: `%LOCALAPPDATA%` (Windows) / `XDG_DATA_HOME` или `~/.local/share` (Unix).
Можно указать явно: `--persistence-dir` / `-PersistenceDir`, `--certs-dir` / `-CertsDir`.
TLS: `CERTS_DIR` → `/etc/shortp2p/certs` (entrypoint создаёт self-signed PFX при отсутствии).

При первом запуске `run-persistent` скрипт **спрашивает** логин и пароль Postgres-админа:
- логин: Enter → **`shortp2p`**;
- пароль: Enter → автогенерация в контейнере (8–64 символа, латиница и цифры, есть верхний/нижний регистр и цифра);
- заданный вручную пароль тоже должен удовлетворять этим правилам.

Учётные данные сохраняются **только в docker-volume** `shortp2p-pg-secrets` (не на хосте).
Порядок старта: `postgres` до `healthy`, затем `messengerserver` (`depends_on`);
`entrypoint.sh` ждёт `credentials.env` и TCP `postgres:<POSTGRES_PORT>`, затем выставляет env:
`Persistence__Enabled=true` и `Persistence__ConnectionString`
(`Host=postgres;Port=<POSTGRES_PORT>;Username/Password` из secrets).
Это перекрывает `appsettings.json` (`Enabled: false`, demo-пароль) — пароли в git не кладём.
Повторный запуск (когда уже есть `pgdata`) промпт пропускает.

Postgres **не публикуется на хост** — слушает `POSTGRES_PORT` (по умолчанию **5432**) только в Compose-сети.
Порт меняется через `POSTGRES_PORT` в `.env.persistent.*`, env, или `--postgres-port` / `-PostgresPort`.
`HOST_PORT`/`INTERNAL_PORT` относятся только к Messenger Server и не связаны с портом Postgres.
Наружу открыт лишь порт Messenger Server.

```bash
./scripts/server/docker/run-persistent.Release.sh
./scripts/server/docker/run-persistent.Release.sh 8080 --persistence-dir /data/shortp2p/pg
./scripts/server/docker/run-persistent.Release.sh --certs-dir /etc/shortp2p-host/certs
./scripts/server/docker/run-persistent.Release.sh --postgres-port 5433
./scripts/server/docker/run-persistent.Development.sh --memory 1024 --cpus 2
```

```powershell
.\scripts\server\docker\run-persistent.Release.ps1
.\scripts\server\docker\run-persistent.Release.ps1 -PersistenceDir 'D:\data\shortp2p\pg'
.\scripts\server\docker\run-persistent.Release.ps1 -CertsDir 'D:\data\shortp2p\certs'
.\scripts\server\docker\run-persistent.Release.ps1 -PostgresPort 5433
.\scripts\server\docker\run-persistent.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
```

| Что | Значение |
|-----|----------|
| Overlay | `docker-compose.persistent.yml` (+ `docker-compose.yml` / `docker-compose.Development.yml`) |
| Env | `.env.persistent.Release.example` / `.env.persistent.Development.example` |
| Postgres | `postgres:11` companion на Compose-сети; без host port; admin через промпт / auto; secrets volume |
| Connection | `Host=postgres;Port=<POSTGRES_PORT>` (default `5432`) |
| Host data | `PERSISTENCE_DIR` → `/var/lib/postgresql/data` |
| Host certs | `CERTS_DIR` → `/etc/shortp2p/certs` (default `%APPUSER%/ShortP2P/MessengerServer/certs`) |

### Raspberry Pi / ARM64

Thin wrappers force `DOCKER_PLATFORM=linux/arm64` and `DOCKER_DEFAULT_PLATFORM=linux/arm64`,
then call the same run scripts / compose files above (plus overlay `docker-compose.arm64.yml`
with `platform: linux/arm64` on `messengerserver`). Same CLI as the non-arm64 scripts
(ports, memory, cpus, certs, persistence, postgres credentials).

Non-arm64 `run*.ps1` / `run*.sh` **clear** sticky `DOCKER_PLATFORM` /
`DOCKER_DEFAULT_PLATFORM` left over from a prior `run-arm64-*` in the same shell, and on
amd64 hosts force `linux/amd64` so BuildKit does not fall into qemu-user arm64 (segfault
during `dotnet restore`). Only the `run-arm64-*` wrappers select `linux/arm64`.

**On the Pi** (64-bit OS, `uname -m` → `aarch64` / `arm64`) use the `.sh` scripts:

```bash
./scripts/server/docker/run-arm64.Release.sh
./scripts/server/docker/run-arm64.Release.sh 8080 --memory 1024 --cpus 2
./scripts/server/docker/run-arm64.Development.sh
./scripts/server/docker/run-arm64-persistent.Release.sh --persistence-dir /data/shortp2p/pg
./scripts/server/docker/run-arm64-persistent.Development.sh --memory 1024 --cpus 2
```

**From Windows** (buildx / QEMU cross-build, then run or push):

```powershell
.\scripts\server\docker\run-arm64.Release.ps1
.\scripts\server\docker\run-arm64.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
.\scripts\server\docker\run-arm64-persistent.Release.ps1 -PersistenceDir 'D:\data\shortp2p\pg'
.\scripts\server\docker\run-arm64-persistent.Development.ps1
```

Requirements / notes:

- Prefer a **64-bit** host (`aarch64`). Scripts fail with a clear message on 32-bit ARM
  (`armv7l` / `armhf`). Use [Raspberry Pi OS 64-bit](https://www.raspberrypi.com/software/),
  or build/push from an aarch64 machine / Docker buildx with QEMU.
- Docker Engine + Compose v2 on the Pi; first build can take a while on low-RAM boards
  (raise `--memory` if needed, default still 512 MB).
- Persistent mode also pulls `postgres:11` for `linux/arm64` via `DOCKER_DEFAULT_PLATFORM`.

## После установки

1. Задайте `Auth:SigningKey` (≥ 32 символов) в Production json.
2. Укажите `Trust:SelfHost` (адрес ноды для клиентов, не оставляйте `127.0.0.1` в LAN/WAN).
3. Положите свой TLS-сертификат в каталог `certs` (инсталлятор PFX не кладёт).
4. Postgres по желанию: поставьте СУБД сами, создайте БД, выставьте `Persistence:Enabled=true` и строку подключения. По умолчанию `false`.

Что переживает рестарт: LiteDB (аккаунты, trust, host-powers) по абсолютным путям.
RAM-кеш — нет. Inbox и blobs — только при Persistence + Postgres.

## Ограничения хоста (нужны правки C#)

Пакеты ставят службу **сейчас**, но процесс ещё не оформлен как Windows Service / systemd notify:

- В `Program.cs` нет `UseWindowsService()`. Служба Windows, скорее всего, упадёт с ошибкой 1053, пока хост не начнёт отвечать SCM.
- Нет `UseSystemd()`. Unit специально `Type=simple` (не `notify`).
- `KestrelServerCertificateReader` сначала ищет cert в `CurrentUser\My` (ASP.NET HTTPS dev cert на Windows), иначе берёт PFX из `Kestrel:Endpoints:*:Certificate:Path` / `Password` (как Docker entrypoint).
