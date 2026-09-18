# Deployment Checklist

A runbook for taking the three services from local/InMemory to AWS production,
mirroring the acceptance criteria in
`docs/milestone-5-deployment-production-readiness.md`. Check items off top to
bottom — later steps assume earlier ones are done.

**Cost note:** the RDS instance size this project's docs specify
(`db.t3.medium`) is **not** Free Tier eligible. Nothing past step 2 should be
done without knowing what it costs and deliberately deciding to spend it.

---

## 0. Prerequisites (done, verified locally)

- [x] All three services build and run against InMemory (Phases 1–4)
- [x] `IDesignTimeDbContextFactory<T>` added per service (`*/Data/*ContextFactory.cs`)
- [x] `dotnet ef migrations add InitialCreate` generated real PostgreSQL migrations
      for all three services (`*/Migrations/`)
- [x] Each `Program.cs` calls `Database.MigrateAsync()` on startup when
      `ASPNETCORE_ENVIRONMENT=Production` and `DefaultConnection` isn't `"InMemory"`
- [x] `appsettings.Production.json` templates added per service (placeholders only,
      no real secrets — see each file's header comment)
- [x] `deploy/publish-all.ps1` produces a deployable zip per service (verified locally)

## 1. Generate the shared JWT secret

- [ ] Generate a new, real secret — **do not reuse the one committed in
      `appsettings.json`**, that value is dev-only and is in source control.
      At least 32 bytes/characters for HS256, e.g.:
      `openssl rand -base64 48` or `[Convert]::ToBase64String((1..48 | %{Get-Random -Max 256}))`
      in PowerShell.
- [ ] Store it somewhere you'll paste from twice (UserService and ReservationService
      need the identical value in their `Jwt__Secret` env var).

## 2. Provision the databases (AWS RDS)

- [ ] Create three PostgreSQL 15.x databases — either three separate RDS instances
      or three databases on one instance (`UserServiceDb`, `CatalogServiceDb`,
      `ReservationServiceDb`)
- [ ] Note each database's endpoint, port, username, password
- [ ] Configure the RDS security group to **not** allow public access — only
      inbound from the security group(s) the Elastic Beanstalk instances will use

## 3. Provision the application environments (AWS Elastic Beanstalk)

- [ ] Create three Elastic Beanstalk environments (.NET platform), one per service
- [ ] Note each environment's public URL — ReservationService needs UserService's
      and CatalogService's URLs; UserService needs ReservationService's URL
- [ ] Configure a security group allowing the three environments to reach each
      other over HTTP (inter-service calls)

## 4. Configure environment variables per service

Set these as Elastic Beanstalk environment properties (never in a committed
appsettings file). Reference: each service's `appsettings.Production.json`
documents the exact names.

**UserService:**
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings__DefaultConnection` → UserServiceDb's Npgsql connection string
- [ ] `Jwt__Secret` → the secret from step 1
- [ ] `Jwt__Issuer=LibraryManagementSystem`, `Jwt__Audience=LibraryUsers`
- [ ] `ServiceUrls__ReservationService` → ReservationService's EB URL

**CatalogService:**
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings__DefaultConnection` → CatalogServiceDb's Npgsql connection string

**ReservationService:**
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings__DefaultConnection` → ReservationServiceDb's Npgsql connection string
- [ ] `Jwt__Secret` → **the same value** as UserService's
- [ ] `Jwt__Issuer=LibraryManagementSystem`, `Jwt__Audience=LibraryUsers`
- [ ] `ServiceUrls__UserService` → UserService's EB URL
- [ ] `ServiceUrls__CatalogService` → CatalogService's EB URL

## 5. Build and deploy

- [ ] Run `./deploy/publish-all.ps1` to produce `publish/{Service}.zip` for all three
- [ ] Upload each zip as a new application version to its Elastic Beanstalk
      environment and deploy
- [ ] Watch each environment's health turn green; check the logs for the
      `Database.MigrateAsync()` call succeeding (schema created automatically —
      no manual migration step needed)

## 6. Verify — per service

**UserService**
- [ ] Swagger UI loads at `/swagger`
- [ ] `POST /api/auth/register` creates a user
- [ ] `POST /api/auth/login` returns a JWT
- [ ] `GET /api/users/profile` (with that JWT) returns real stats — this proves
      the ReservationService URL/connectivity is correct
- [ ] `GET /api/users/{userId}/validate` responds (called by ReservationService)

**CatalogService**
- [ ] Swagger UI loads
- [ ] `GET /api/catalog/books` works without a token
- [ ] Search/filter/sort combinations work
- [ ] `GET /api/catalog/books/{bookId}` works; 404 for a bad id
- [ ] `PUT /api/catalog/books/{bookId}/availability` works (called by ReservationService)

**ReservationService**
- [ ] Swagger UI loads
- [ ] `POST /api/reservations` succeeds — this proves connectivity to *both* other
      services (validates via UserService, checks/updates availability via CatalogService)
- [ ] Checkout/return work for a Librarian account and return 403 for a Patron
- [ ] `GET /api/reservations/history` and waitlist join/list/leave work
- [ ] Logs show the waitlist expiry background job ticking on its configured interval

## 7. Data persistence check

- [ ] Restart each Elastic Beanstalk environment (or redeploy) and confirm
      previously-created users/books/reservations are still there — this is the
      real proof InMemory isn't silently still in use somewhere

## 8. Teardown (when you're done testing)

RDS `db.t3.medium` costs money continuously while it exists. When you're done
verifying:
- [ ] Delete the RDS instance(s) (skip the final snapshot unless you want to keep one)
- [ ] Terminate the Elastic Beanstalk environments
- [ ] Double-check the AWS billing console shows no orphaned resources
