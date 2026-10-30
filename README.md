# RentalManager

Backend for an equipment rental business: the catalogue, customers, reservations,
availability checks, pricing and monthly reporting.

Written as a portfolio project to work with ASP.NET Core the way I would on a
normal business application — layered, tested, containerised, no framework tricks.

**Status:** work in progress. This commit is the skeleton only — the host, the
project layout and the configuration. Domain and endpoints land in the next
commits.

## Stack

- .NET 8, ASP.NET Core, controllers
- EF Core 8 with MySQL (Pomelo provider)
- JWT bearer auth, role-based authorisation
- FluentValidation, xUnit
- Swagger / OpenAPI

## Running

Needs the .NET 8 SDK. MySQL is only required once the data layer exists.

```
dotnet run --project src/RentalManager.Api
```

Swagger: http://localhost:5080/swagger
Health: http://localhost:5080/health

The development connection string in `appsettings.Development.json` points at a
local MySQL instance. Change it there or override `ConnectionStrings__MySql`.

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

- [ ] Domain model and EF Core mappings
- [ ] Authentication and roles
- [ ] Catalogue and customer endpoints
- [ ] Reservations, availability and pricing
- [ ] Overdue handling and reporting
- [ ] Docker, CI, seed data
