# EventHub

[![CI](https://github.com/meissnerM224/EventHub/actions/workflows/ci.yaml/badge.svg?branch=develop)](https://github.com/meissnerM224/EventHub/actions/workflows/ci.yaml)
[![Web](https://github.com/meissnerM224/EventHub/actions/workflows/web.yaml/badge.svg?branch=develop)](https://github.com/meissnerM224/EventHub/actions/workflows/web.yaml)
[![Release](https://github.com/meissnerM224/EventHub/actions/workflows/release.yaml/badge.svg?branch=main)](https://github.com/meissnerM224/EventHub/actions/workflows/release.yaml)

EventHub is an event and meetup platform: users can discover events, organizers manage them, and authenticated users can book places. The repository contains an ASP.NET Core API and a React web client. It is also a learning project for layered architecture, dependency injection, authentication, concurrency, caching, and object storage.

## Tech stack

| Area | Technology |
|---|---|
| API | C#, ASP.NET Core 10 |
| Web | React 19, TypeScript, Vite |
| Database | PostgreSQL 17, Entity Framework Core |
| Cache | Redis 7 |
| Object storage | Garage (S3-compatible) |
| Authentication | ASP.NET Identity and JWT in an HttpOnly cookie |
| Tests | xUnit, Testcontainers; Vitest and Testing Library for the web client |
| CI/CD | GitHub Actions; API container published to GHCR |
| Deployment | Docker Compose and Caddy |

## Run with Docker Compose

The Compose setup runs PostgreSQL, Redis, Garage, a migration job, the API, and Caddy, which also serves the web client. Copy `.env.example` to `.env`, replace every example value with unique credentials, then start the services:

```bash
cp .env.example .env
# Edit .env and set strong, unique values for every variable.
docker compose up -d
```

Compose requires `POSTGRES_PASSWORD`, `JWT_KEY`, `POSTGRES_DB`, `POSTGRES_USER`, `GARAGE_RPC_SECRET`, `GARAGE_ADMIN_TOKEN`, `STORAGE_ACCESS_KEY`, and `STORAGE_SECRET_KEY`. Use a long random value for `JWT_KEY` (at least 32 characters) and appropriate secrets for Garage; do not commit `.env`.

The `migrate` service applies database migrations before the API starts. `storage-init` configures Garage and initializes the media bucket. The API is available through Caddy at `https://localhost`; accept or trust the local development certificate as appropriate. The Scalar API reference is at `https://localhost/scalar`.

```bash
docker compose down      # stop the services
docker compose down -v   # also remove database, Garage data, and Caddy state
```

### Compose files and routing

`docker-compose.yaml` is the deployment configuration and pulls two images from GHCR: the API (`eventhub`) and the proxy (`eventhub-web`, Caddy with the built web client and the `Caddyfile`, see `web/Dockerfile`). `docker-compose.override.yaml` is applied automatically for local development: it builds both images from source and publishes PostgreSQL, Redis, and Garage ports to the host. To use only the published images and deployment configuration, run:

```bash
docker compose -f docker-compose.yaml up -d
```

Caddy terminates HTTPS on `localhost`, forwards `/api/*` to the API service, routes `/media/*` to Garage's public media endpoint, and serves the React app for every other path. Unknown paths such as `/events/<id>` get `index.html`, so client-side routes work on reload. Files under `/assets/` carry a content hash and are cached for a year; `index.html` is always revalidated. Every response, including errors, gets the security headers (CSP, `X-Frame-Options`, `nosniff`, `Referrer-Policy`, `Permissions-Policy`).

Because the `Caddyfile` and the web client are built into the proxy image, changes to either need a rebuild of that image:

```bash
docker compose up -d --build proxy
```

Uploaded image files are decoded and re-encoded as WebP, with the longest edge limited to 1600 pixels. Image uploads are limited to 5 MB and require the Organizer role.

The image is uploaded before the event is created. Images that no event references are deleted by a background job once they are older than 24 hours, so abandoned or failed create-event forms do not leave files behind. Images of cancelled events are kept. The job runs hourly; both values can be changed with `ImageCleanup:IntervalMinutes` and `ImageCleanup:GracePeriodHours`.

## Develop locally

### Prerequisites

- .NET 10 SDK
- Docker, for PostgreSQL, Redis, Garage, and the integration tests
- Node.js 26 and pnpm 11 for the web client
- `dotnet-ef` as a global tool:

```bash
dotnet tool install --global dotnet-ef
```

On Linux, add `~/.dotnet/tools` to `PATH` if needed:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### 1. Start the dependencies

Start the services needed by the API (the default Compose override publishes their ports):

```bash
docker compose up -d db cache storage storage-init
```

### 2. Configure API user secrets

Connection strings, signing keys, and storage credentials should not be committed. Configure them as .NET user secrets from the API project directory:

```bash
cd src/EventHub.Api

dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>"
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
dotnet user-secrets set "Jwt:Key" "<a unique random signing key of at least 32 characters>"
dotnet user-secrets set "Storage:Endpoint" "http://localhost:3900"
dotnet user-secrets set "Storage:AccessKey" "<STORAGE_ACCESS_KEY from .env>"
dotnet user-secrets set "Storage:SecretKey" "<STORAGE_SECRET_KEY from .env>"

dotnet user-secrets list
```

The API requires database, Redis, and JWT configuration at startup. The JWT issuer, audience, and expiry are configured in `appsettings.json`; the storage bucket defaults to `media`. The Compose network uses the service name `storage` for Garage's S3 endpoint.

### 3. Apply migrations and run the API

Run migrations from the repository root:

```bash
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
```

Then start the API:

```bash
cd src/EventHub.Api && dotnet run
```

The API uses the local launch settings (typically `https://localhost:5001` and `http://localhost:5000`). Scalar is available at `/scalar`. User secrets are read in the `Development` environment.

### 4. Run the web client

In another terminal, install dependencies and start Vite:

```bash
cd web
pnpm install --frozen-lockfile
pnpm dev
```

Vite serves the client on its development port and proxies `/api` to `https://localhost` and `/media` to the local Garage media endpoint, as configured in `web/vite.config.ts`. Start the Compose proxy as well if you want to use those proxy targets.

The client includes:

- event discovery and detail pages, with booking and cancelling on the detail page until the event starts
- registration and login
- an account area with settings and bookings, split into upcoming and past events; upcoming bookings can be cancelled
- an organizer-only create-event form with image upload
- an administrator-only admin page (`/admin`) to grant or revoke the Organizer role and to create categories

Password reset is not available yet: the API has no reset endpoint, so the forgot-password page is a placeholder.

## Authentication and authorization

Registration and login set a signed JWT in the `eventhub_auth` HttpOnly, Secure, SameSite=Strict cookie. The API also supports bearer tokens. The web client uses the cookie-based session and checks the current user through `/api/auth/me`. There are three roles: every new account is a `Participant`, `Organizer` is required for event management and image upload, and `Administrator` is required for the user list, Organizer grants and revocations, and creating categories.

### Set up the first administrator

New accounts get the `Participant` role. An administrator can grant and revoke `Organizer` on the admin page (`/admin`), but no endpoint grants `Administrator` itself, so the first one is assigned directly in the database.

1. Register the account through the web client or `POST /api/auth/register`.
2. Assign the role with SQL. With Docker Compose:

   ```bash
   docker compose exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' <<'SQL'
   INSERT INTO "UserRoles" ("UserId", "RoleId")
   SELECT u."Id", r."Id"
   FROM "Users" u
   JOIN "Roles" r ON r."NormalizedName" = 'ADMINISTRATOR'
   WHERE u."NormalizedEmail" = UPPER('admin@example.com')
   ON CONFLICT DO NOTHING;
   SQL
   ```

   Replace `admin@example.com` with the registered email. `INSERT 0 1` means the role was assigned; `INSERT 0 0` means the email was not found or the user already is an administrator.
3. Sign out and sign in again. Roles are part of the JWT, so the existing session does not see the new role.

To check who is an administrator:

```sql
SELECT u."Email"
FROM "UserRoles" ur
JOIN "Users" u ON u."Id" = ur."UserId"
JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE r."NormalizedName" = 'ADMINISTRATOR';
```

## Security

`tests/EventHub.Api.Tests/SecurityTests.cs` replays each of the attacks below and expects it to be stopped.

| Threat | Protection |
|---|---|
| Password guessing on one account | Identity lockout: 5 wrong passwords lock the account for 15 minutes (`auth.locked_out`). A successful login resets the counter. |
| Trying many accounts, bulk registration | Rate limit on `POST /api/auth/login` and `/register`: 10 requests per minute per client IP (`auth.too_many_requests`, HTTP 429 with `Retry-After`). Configurable with `RateLimiting:AuthPermitLimit`. The client IP is taken from `X-Forwarded-For` only when it comes from the Docker network. |
| Stolen token after logout, stale roles after a role change | The JWT carries the user's security stamp, and every request compares it with the database. Logout and granting or revoking a role change the stamp, so older tokens are rejected immediately instead of at expiry. Logout therefore ends the user's sessions on all devices. |
| Decompression bombs | Uploads are limited to 5 MB *and* 40 megapixels. The pixel count is read from the image header before any pixel memory is allocated (`file.too_many_pixels`). |
| Response time revealing registered emails at login | An unknown email is checked against a dummy password hash, so both cases cost the same. |
| Internals in error responses | Outside `Development`, a 500 contains only a generic message and a `traceId`. The API reference (`/scalar`) is only served in `Development`. |
| SQL injection, XSS, CSRF | Queries go through EF Core and are parameterized; React escapes output and uploads are re-encoded as WebP; the auth cookie is `HttpOnly`, `Secure`, `SameSite=Strict` and limited to `/api`. |

Known limitations, accepted for now:

- **Registration reveals whether an email is registered** (`user.email_taken`). Hiding it needs email confirmation: always answer "check your inbox" and send either a confirmation or an "already registered" mail. That requires an email provider, which the project does not have yet.
- **`GET /api/events` has no paging.** The list returns all upcoming events. With the current data size this is fine; once it grows, add `page`/`pageSize` together with paging in the web client.
- **Every authenticated request reads the security stamp from the database.** This is a single indexed lookup by primary key; cache it in Redis if it ever shows up in profiling (with a short TTL, since the TTL is the revocation delay).

### Set up the first administrator

New accounts get the `Participant` role. An administrator can grant and revoke `Organizer` on the admin page (`/admin`), but no endpoint grants `Administrator` itself, so the first one is assigned directly in the database.

1. Register the account through the web client or `POST /api/auth/register`.
2. Assign the role with SQL. With Docker Compose:

   ```bash
   docker compose exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' <<'SQL'
   INSERT INTO "UserRoles" ("UserId", "RoleId")
   SELECT u."Id", r."Id"
   FROM "Users" u
   JOIN "Roles" r ON r."NormalizedName" = 'ADMINISTRATOR'
   WHERE u."NormalizedEmail" = UPPER('admin@example.com')
   ON CONFLICT DO NOTHING;
   SQL
   ```

   Replace `admin@example.com` with the registered email. `INSERT 0 1` means the role was assigned; `INSERT 0 0` means the email was not found or the user already is an administrator.
3. Sign out and sign in again. Roles are part of the JWT, so the existing session does not see the new role.

To check who is an administrator:

```sql
SELECT u."Email"
FROM "UserRoles" ur
JOIN "Users" u ON u."Id" = ur."UserId"
JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE r."NormalizedName" = 'ADMINISTRATOR';
```

## API endpoints

| Method | Path | Access |
|---|---|---|
| `POST` | `/api/auth/register` | Anonymous |
| `POST` | `/api/auth/login` | Anonymous |
| `POST` | `/api/auth/logout` | Clears the auth cookie |
| `GET` | `/api/auth/me` | Authenticated |
| `GET` | `/api/events` | Anonymous; supports event filters |
| `GET` | `/api/events/{id}` | Anonymous |
| `GET` | `/api/categories` | Anonymous |
| `POST` | `/api/categories` | Administrator; names are unique, ignoring case |
| `POST` | `/api/events` | Organizer |
| `PUT` | `/api/events/{id}` | Organizer; event owner |
| `DELETE` | `/api/events/{id}` | Organizer; event owner |
| `POST` | `/api/uploads/images` | Organizer; multipart image upload, max 5 MB |
| `GET` | `/api/events/{eventId}/bookings` | Authenticated; event organizer |
| `POST` | `/api/events/{eventId}/bookings` | Authenticated |
| `DELETE` | `/api/events/{eventId}/bookings/me` | Authenticated |
| `GET` | `/api/me/bookings` | Authenticated; own confirmed bookings |
| `GET` | `/api/admin/users` | Administrator; all users with their roles |
| `PUT` | `/api/admin/users/{userId}/roles/organizer` | Administrator |
| `DELETE` | `/api/admin/users/{userId}/roles/organizer` | Administrator |

`GET /api/events` accepts optional filters such as category, location, and date range. See the Scalar API reference for request and response schemas.

## Migrations

Run Entity Framework commands from the repository root. Migrations live in `EventHub.Infrastructure`; the startup configuration is in `EventHub.Api`, so both project arguments are needed:

```bash
# Add a migration
dotnet ef migrations add <Name> --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Apply migrations
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Remove the last migration if it has not been applied
dotnet ef migrations remove --project src/EventHub.Infrastructure --startup-project src/EventHub.Api

# Inspect generated SQL
dotnet ef migrations script --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
```

Compose runs the same migrations in its one-shot `migrate` service before starting the API. Applied migrations should not be edited; create a new migration to make subsequent schema changes.

## Tests and checks

The API tests use Testcontainers and require Docker:

```bash
dotnet test
```

The web-client checks are defined in `web/package.json`:

```bash
cd web
pnpm format:check
pnpm lint
pnpm exec tsc -b
pnpm test
pnpm build
```

GitHub Actions runs the .NET restore, formatting, Release build, and test suite, plus the web formatting, lint, type-check, tests, and production build on pushes and pull requests to `main` and `develop`. The release workflow builds and publishes the API image and the proxy image (with the web client) to GHCR from `main`.

## Project structure

```text
EventHub/
├── garage/                  # Garage image and initialization
├── scripts/                 # helper scripts
├── src/
│   ├── EventHub.Domain/     # domain entities, interfaces, and rules
│   ├── EventHub.Infrastructure/ # EF Core, Redis, Identity, storage
│   └── EventHub.Api/        # controllers and composition root
├── tests/
│   └── EventHub.Api.Tests/  # xUnit and Testcontainers
└── web/                     # React, TypeScript, and Vite client
```

Dependencies point inward: Domain has no dependency on Infrastructure or the API; Infrastructure implements domain interfaces, and the API composes the services and exposes HTTP endpoints.
