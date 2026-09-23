# EventHub

[![CI](https://github.com/meissnerM224/EventHub/actions/workflows/ci.yaml/badge.svg)](https://github.com/meissnerM224/EventHub/actions/workflows/ci.yaml)
[![Release](https://github.com/meissnerM224/EventHub/actions/workflows/release.yaml/badge.svg)](https://github.com/meissnerM224/EventHub/actions/workflows/release.yaml)

Backend for an event/meetup platform. Users create events, sign up for them with a
limited number of seats, and search and filter events.

A learning project for backend architecture: layering, dependency injection, the
repository pattern, auth, concurrency, caching.

![scalar-overview.png](scalar-overview.png)

## Tech stack

| Area               | Choice                                  |
|--------------------|-----------------------------------------|
| Language/framework | C#, ASP.NET Core                        |
| Database           | PostgreSQL 17                           |
| ORM                | Entity Framework Core                   |
| Cache              | Redis 7                                 |
| Object storage     | MinIO (S3-compatible), for event images |
| Auth               | JWT + ASP.NET Identity                  |
| Tests              | xUnit + Testcontainers                  |
| CI/CD              | GitHub Actions, image published to GHCR |
| Deployment         | Docker Compose behind Caddy             |

```mermaid
flowchart LR
    Client["Client"] -->|" HTTPS "| Proxy["Caddy<br/>reverse proxy"]
    Proxy --> Api["ASP.NET Core API<br/>controllers"]
    Api --> Svc["Domain services<br/>business rules"]
    Svc -->|" 1. cache hit? "| Redis[("Redis 7")]
    Svc -->|" 2. on miss "| Repo["Repositories<br/>EF Core"]
    Repo --> Db[("PostgreSQL 17")]
    Repo -.->|" cache result "| Redis
    Proxy -->|" /media/* "| Minio[("MinIO<br/>object storage")]
    Api -->|" uploads "| Minio
```

## Quickstart

Runs the published image from GHCR. No .NET SDK required, only Docker.

```bash
cp .env.example .env    # fill in the values
docker compose up -d
```

Compose starts PostgreSQL, Redis and MinIO, applies the migrations in a one-shot
`migrate` container, creates the `media` bucket and only then starts the API behind
Caddy.

|                   | URL                       |
|-------------------|---------------------------|
| API               | `http://localhost/api`    |
| API documentation | `http://localhost/scalar` |

See [`.env.example`](.env.example) for the variables that have to be set. Compose
refuses to start if one of them is missing.

```bash
docker compose down      # stop
docker compose down -v   # stop and drop database and uploaded images
```

### Compose files

`compose.yaml` is the deployment setup: every service pulls the published image from
GHCR, and only Caddy exposes ports. `compose.override.yaml` is picked up automatically
and adapts that for development — it builds the image from source as
`eventhub-api:local` and publishes Postgres, Redis and MinIO on their default ports so
you can reach them from the host.

To run exactly what CI produced, skip the override:

```bash
docker compose -f compose.yaml up -d
```

### Routing

Caddy terminates every request and splits them in two:

| Path            | Goes to                                                                         |
|-----------------|---------------------------------------------------------------------------------|
| `/media/*`      | MinIO directly — event images are served as static files, never through the API |
| everything else | the API on port 8080, including `/scalar`                                       |

The `media/events` prefix is public read, so image URLs work in a browser without a
token. Uploading still requires an organizer token via `POST /api/uploads/images`.

Uploaded images pass through ImageSharp before they reach storage: the decoder itself rejects anything that is not a
real image, the picture is scaled down to at most 1600 px and re-encoded as WebP. The format therefore never depends on
what the client sent. The API stores the relative path (/media/events/<guid>.webp), never an absolute URL - the same
record works behind any host name.

Talking plain HTTP behind the proxy is deliberate: that traffic never leaves the Compose network.

## Develop locally

Run the dependencies in containers and the API from the SDK.

### Prerequisites

- .NET SDK (see `global.json` or `TargetFramework` in the `.csproj` files)
- Docker – required for the tests too, not just for local development
- `dotnet-ef` as a global tool:

```bash
dotnet tool install --global dotnet-ef
```

On Linux the tool lands in `~/.dotnet/tools`, which is not on the PATH by default:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### 1. Start the dependencies

```bash
docker compose up -d db cache storage storage-init
```

### 2. Set the user secrets

Credentials and the JWT key do not belong in the repository. They live in user secrets,
which are only read in `Development`.

```bash
cd src/EventHub.Api

dotnet user-secrets set "ConnectionStrings:Default" \
    "Host=localhost;Port=5432;Database=eventhub;Username=postgres;Password=dev"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Storage:Endpoint" "http://localhost:9000"
dotnet user-secrets set "Storage:AccessKey" "<MINIO_ROOT_USER from .env>"
dotnet user-secrets set "Storage:SecretKey" "<MINIO_ROOT_PASSWORD from .env>"

dotnet user-secrets list
```

The database and JWT keys are required; the app throws on startup if either is missing. The storage keys are only needed
for image uploads. Storage:Endpoint points at localhost because the override publishes MinIO's ports — inside Compose
the same value is http://storage:9000.

Everything else lives in appsettings.json, because none of it is secret: ConnectionStrings:Redis plus Jwt:Issuer, Jwt:
Audience and Jwt:ExpiryMinutes.

ConnectionStrings:Default stays in appsettings.json as an empty entry so the repository still documents which keys the
app expects.

### 3. Set up the database

```bash
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
```

### 4. Run

```bash
cd src/EventHub.Api && dotnet run
```

The API listens on `http://localhost:5000`, the API documentation is at
`http://localhost:5000/scalar`.

Port and environment come from `Properties/launchSettings.json`. Without that file
`dotnet run` starts in `Production`, user secrets are never read, and every request fails
on a missing `Jwt:Key`.

## Migrations

Run all commands from the repository root. The migrations live in
`EventHub.Infrastructure`, the configuration in the startup project `EventHub.Api` –
hence both arguments every time.

```bash
# Add a migration
dotnet ef migrations add <Name> --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Apply
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Remove the last migration, as long as it has not been applied
dotnet ef migrations remove --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Inspect the generated SQL without running it
dotnet ef migrations script --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
```

In the Compose setup the same migrations run automatically in the `migrate` container
before the API starts.

**Rule:** applied migrations are never edited, only superseded by new ones. Before
applying, read the generated file – indexes, nullability and delete behaviour reveal how
EF understood the fluent API chain.

## CI

Every pull request against `main` and `develop` has to pass four gates: restore,
`dotnet format --verify-no-changes`, a Release build with `-warnaserror`, and the full
test suite. Formatting slips and compiler warnings fail the build, so neither reaches the
main branch.

The release workflow builds the container image and pushes it to GHCR. That is the same
image Compose pulls, so what runs locally is what CI produced.

## Tests

```bash
dotnet test
```

Docker has to be running. The tests spin up their own Postgres and Redis containers via
Testcontainers on random ports, apply the real migrations and seed users and categories.
No setup required, and the local development database is left untouched.

The images deliberately match the ones used for local development. Testing against a
different Postgres major version than you run is an avoidable risk.

The tests' JWT key sits in the test code in plain text. That is correct here: it signs
nothing that ever leaves a test process.

## Endpoints

![scalar-event-booking.png](scalar-event-booking.png)

| Method   | Path                           | Access                |
|----------|--------------------------------|-----------------------|
| `POST`   | `/api/auth/register`           | anonymous             |
| `POST`   | `/api/auth/login`              | anonymous             |
| `GET`    | `/api/events`                  | anonymous             |
| `GET`    | `/api/events/{id}`             | anonymous             |
| `POST`   | `/api/events`                  | Organizer             |
| `PUT`    | `/api/events/{id}`             | the event's organizer |
| `DELETE` | `/api/events/{id}`             | the event's organizer |
| `POST`   | `/api/uploads/images`          | the event's organizer |
| `POST`   | `/api/events/{id}/bookings`    | authenticated         |
| `DELETE` | `/api/events/{id}/bookings/me` | authenticated         |
| `GET`    | `/api/events/{id}/bookings`    | the event's organizer |

`GET /api/events` takes optional query parameters:
`?categoryId=1&location=berlin&from=2026-10-01T00:00:00Z&to=2026-10-31T00:00:00Z`

### Example

Against the SDK run on port 5000. In the Compose setup, replace the base URL with
`http://localhost`.

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
    -H 'Content-Type: application/json' \
    -d '{"email":"organizer@test.com","password":"Test1234!"}' | jq -r .accessToken)

curl -s http://localhost:5000/api/events | jq
curl -s -X POST http://localhost:5000/api/events/<eventId>/bookings \
    -H "Authorization: Bearer $TOKEN"
```

## Benchmark

`scripts/bench.sh` measures `GET /api/events` over N calls.

```bash
./scripts/bench.sh http://localhost:5000/api/events 100
```

Needs `bc` and `jq`. The first call after startup measures the startup, not the query –
JIT, connection setup and building the EF model cost seconds there. Median and p95 after
a warm-up run are the numbers that mean something.

Measured with roughly 500 events, locally, requests one after another:

|        | without cache | with Redis |
|--------|---------------|------------|
| Median | 10.1 ms       | 3.8 ms     |
| p95    | 20.2 ms       | 5.2 ms     |
| Max    | 25.6 ms       | 6.9 ms     |

The median drops by a factor of three, the p95 by a factor of four. So response times do
not just get faster, they get steadier – Redis hands back a ready-made string from
memory, while Postgres plans and reads afresh for every request.

## Project structure

```
EventHub/
├── EventHub.sln
├── scripts/
│   └── bench.sh
├── src/
│   ├── EventHub.Domain/          → references nothing
│   ├── EventHub.Infrastructure/  → Domain
│   └── EventHub.Api/             → Domain + Infrastructure
└── tests/
    └── EventHub.Api.Tests/       → Api (Domain/Infrastructure transitively)
```

```mermaid
flowchart RL
    Api["EventHub.Api<br/>controllers, composition root"] --> Domain["EventHub.Domain<br/>entities, interfaces, rules"]
    Infra["EventHub.Infrastructure<br/>EF Core, Redis, Identity"] --> Domain
    Api --> Infra
    Tests["EventHub.Api.Tests<br/>xUnit, Testcontainers"] --> Api
```

**Dependencies point inwards, never outwards.** Domain holds the entities, interfaces,
read models and the services carrying the business rules. Infrastructure supplies the
implementations that need third-party technology (EF Core, Redis, Identity). Api consists
of controllers, input models and the composition root in `Program.cs`.

Why separate projects rather than plain folders: the compiler enforces the split. A
`using Microsoft.EntityFrameworkCore;` inside Domain simply fails to build.

## Configuration

The key stays the same, the source changes. Later sources override earlier ones:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (Development only)
4. Environment variables (`ConnectionStrings__Default`, double underscore)
5. Command-line arguments

Driven by `ASPNETCORE_ENVIRONMENT`. No `if` in the code, no separate keys for dev and
prod.

## Next up

A React frontend on top of the API, and deployment to a Raspberry Pi in my home network.