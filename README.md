# RentalManager

Backend for an equipment rental business: the catalogue, customers, reservations,
availability checks, pricing and monthly reporting.

Written as a portfolio project to work with ASP.NET Core the way I would on a
normal business application — layered, tested, containerised, no framework tricks.

**Status:** work in progress. The domain model, database layer, authentication,
catalogue/customer endpoints and reservations with availability are in place.
Reporting and deployment land in the next commits.

## Stack

- .NET 8, ASP.NET Core, controllers
- EF Core 8 with MySQL (Pomelo provider)
- JWT bearer auth, role-based authorisation
- FluentValidation, xUnit
- Swagger / OpenAPI

## Running

Needs the .NET 8 SDK and a MySQL 8 instance.

```
dotnet run --project src/RentalManager.Api
```

Swagger: http://localhost:5080/swagger
Health: http://localhost:5080/health

The development connection string in `appsettings.Development.json` points at a
local MySQL instance. Change it there or override `ConnectionStrings__MySql`.

On startup the application applies any pending migrations and inserts the
reference data if the tables are empty, so a fresh database is enough to run it.

Swagger is switched on by the `Swagger:Enabled` setting — true in Development,
false by default. Override it with `Swagger__Enabled=true` where the API should
expose the docs.

## Authentication

`POST /api/auth/login` takes an email and password and returns a signed JWT.
Passwords are hashed with PBKDF2 (SHA-256, 100 000 iterations, per-user salt);
the stored value carries the iteration count so it can be upgraded later.

Signing is configured under the `Jwt` section. In Development a local key and a
seeded admin account are supplied in `appsettings.Development.json`. For any
other environment, set `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` and optionally
`Seed__AdminEmail` / `Seed__AdminPassword`. The app refuses to start if the
signing key is missing or shorter than 32 characters.

Roles are `Staff`, `Manager` and `Admin`. `/api/auth/me` needs a valid token;
`/api/system/diagnostics` is restricted to Manager and Admin by the
`ManagerOrAdmin` policy. `/health` and `/api/system/info` stay public.

## Endpoints

All routes below require a token. Catalogue and customer reads are available to
every authenticated role; catalogue/customer writes need Manager or Admin. Any
authenticated role can manage reservations.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/categories?page&pageSize` | paged category list |
| GET | `/api/categories/{id}` | single category |
| POST | `/api/categories` | create category |
| PUT | `/api/categories/{id}` | rename category |
| GET | `/api/equipment?page&pageSize&categoryId&status&search` | paged equipment list |
| GET | `/api/equipment/{id}` | single item |
| POST | `/api/equipment` | add item |
| PUT | `/api/equipment/{id}` | rename item / change daily rate |
| PATCH | `/api/equipment/{id}/status` | change availability status |
| GET | `/api/customers?page&pageSize&activeOnly&search` | paged customer list |
| GET | `/api/customers/{id}` | single customer |
| POST | `/api/customers` | create customer |
| PUT | `/api/customers/{id}` | update customer details |
| POST | `/api/customers/{id}/deactivate` | deactivate customer |
| GET | `/api/reservations?page&pageSize&status&customerId&from&to` | paged reservation list |
| GET | `/api/reservations/availability?from&to` | stock and booked units per item |
| GET | `/api/reservations/{id}` | single reservation |
| POST | `/api/reservations` | create draft with lines |
| POST | `/api/reservations/{id}/lines` | add a line to a draft |
| POST | `/api/reservations/{id}/confirm` | draft → confirmed |
| POST | `/api/reservations/{id}/start` | confirmed → active |
| POST | `/api/reservations/{id}/complete` | active → completed |
| POST | `/api/reservations/{id}/cancel` | cancel a non-finished reservation |

Validation failures return a 400 Validation Problem Details body; duplicate
category names, customer emails and unavailable equipment return 409.

## Reservations

A reservation starts as a draft, holds one or more equipment lines and computes
its total from `unit price × days × quantity` at the time the line is added.
Equipment has a `Stock` count; only confirmed and active lines count as booked,
and periods are half-open, so a booking ending on the 4th does not collide with
one starting on the 4th.

Availability is checked when a line is written. It is a service-level check,
not a database exclusion constraint, so it does not replace locking under heavy
concurrent writes.

## Database

Migrations live in `src/RentalManager.Infrastructure/Persistence/Migrations` and
are managed with the EF Core tools, pinned in `.config/dotnet-tools.json`:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/RentalManager.Infrastructure
dotnet ef database update --project src/RentalManager.Infrastructure
```

## Tests

```
dotnet test
```

The suite is self-contained. API integration tests run through
`WebApplicationFactory` with an EF Core in-memory provider, so no MySQL server
or test schema is needed. Domain rules, password hashing and JWT generation are
covered by unit tests.

## Layout

```
src/
  RentalManager.Domain          entities and rules, no dependencies
  RentalManager.Application     use cases, DTOs, abstractions
  RentalManager.Infrastructure  EF Core, migrations, external services
  RentalManager.Api             controllers, validation, auth, host
tests/
  RentalManager.Tests           unit and integration tests
```

## Next

- [x] Domain model and EF Core mappings
- [x] Authentication and roles
- [x] Catalogue and customer endpoints
- [x] Reservations, availability and pricing
- [ ] Overdue handling and reporting
- [ ] Docker, CI
