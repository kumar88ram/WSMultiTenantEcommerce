# Multi-Tenant Ecommerce Bootstrap

This repository contains a full-stack bootstrap for a multi-tenant ecommerce solution composed of a .NET 8 Web API, Angular front-end workspace, and a .NET MAUI mobile shell.

## Solution Layout

```
backend/
  src/
    MultiTenantEcommerce.Domain/
    MultiTenantEcommerce.Application/
    MultiTenantEcommerce.Infrastructure/
    MultiTenantEcommerce.Auth/
    MultiTenantEcommerce.Presentation/
  database/
    initial_schema.sql
frontend/
  angular/
mobile/
  MultiTenantEcommerce.Maui/
```

### Backend (.NET 8 Web API)

* **Domain**: Entities for tenants, users, roles, and refresh tokens.
* **Application**: Authentication service contracts and implementations.
* **Infrastructure**: EF Core persistence layer and JWT helpers.
* **Presentation**: ASP.NET Core Web API with JWT authentication and multi-tenant middleware.
* **Auth**: Dedicated project for JWT token infrastructure (referenced by the API).

Key entry points:

* `Program.cs` configures dependency injection, JWT authentication, Swagger, EF Core and multi-tenancy.
* `ApplicationDbContext` contains EF Core DbSets and model configuration.
* `MultiTenantMiddleware` resolves tenants from the `X-Tenant` header or subdomain and enforces scoping.
* Sample migration `20240608000100_InitialCreate.cs` sets up Users, Roles, Tenants, UserRoles, and RefreshTokens.

> **Connection Strings**: Update `backend/src/MultiTenantEcommerce.Presentation/appsettings.json` with your SQL Server credentials before running migrations.

#### Running the API

1. Restore dependencies: `dotnet restore` (requires .NET 8 SDK).
2. Apply migrations: `dotnet ef database update` from the Presentation project directory.
3. Run the API: `dotnet run --project backend/src/MultiTenantEcommerce.Presentation`.

During development, the API expects requests to include an `X-Tenant` header or subdomain representing the tenant identifier.

### Front-end (Angular)

The Angular workspace is generated with Angular CLI conventions and organised into feature modules:

* `auth` – login component and JWT handling service.
* `core` – guards, interceptors, and token storage service.
* `shared` – shared components/services placeholder.
* `admin` – secured admin dashboard route.
* `tenant-store` – placeholder for tenant-scoped state management.

The `AuthService` handles login, token refresh, and logout while persisting tokens via `TokenStorageService`. The HTTP interceptor injects bearer tokens and tenant headers.

#### Running the Front-end

1. Install dependencies: `npm install` from `frontend/angular`.
2. Start the dev server: `npm start`.
3. Configure `environment.ts` with the API base URL if different from the default `https://localhost:5001`.

### Mobile (.NET MAUI)

The MAUI project provides a login screen and secure token storage using `SecureStorage`. `ApiService` centralizes HTTP calls to the API and adds tenant headers when available.

#### Running the Mobile App

1. Restore dependencies: `dotnet restore mobile/MultiTenantEcommerce.Maui/MultiTenantEcommerce.Maui.csproj`.
2. Run on the desired target: `dotnet build` then `dotnet maui run -t android` (or another platform).

Update the `ApiService` base address to match your API endpoint.

### Database Scripts

* `backend/database/initial_schema.sql` provides SQL Server DDL to set up Tenants, Users, Roles, UserRoles, and RefreshTokens.

### Multi-tenant Strategy

Tenant resolution defaults to the `X-Tenant` header but can be swapped by adjusting `MultiTenantMiddleware.ResolveTenantIdentifier`. The resolver is registered as a scoped service, enabling alternative strategies (e.g., JWT claims, query string, or per-request context) without modifying the application layer.

### Next Steps

* Implement additional domain logic, tenant provisioning workflows, and tenant-specific seeding.
* Replace placeholder UI and navigation stubs with production-ready screens.
* Harden refresh token lifecycle (e.g., sliding expiration, rotation policies).
* Add automated tests for services and middleware.

## Prerequisites

* .NET 8 SDK for backend and MAUI projects.
* Node.js 18+ and Angular CLI for the front-end.
* SQL Server instance accessible to the API.

Ensure all connection strings and JWT secrets are updated before running in production environments.
