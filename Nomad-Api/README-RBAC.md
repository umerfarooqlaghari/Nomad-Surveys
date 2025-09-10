# Multi-Tenant RBAC System for Nomad Surveys

## Overview

This is a production-ready, scalable multi-tenant Role-Based Access Control (RBAC) system built with ASP.NET Core 8, Entity Framework Core, and PostgreSQL. The system implements shared schema multi-tenancy with comprehensive security features.

## Architecture

### Domain Repository Pattern
- **Controllers**: API routing and HTTP request handling
- **Services**: Business logic and orchestration
- **Repositories**: Data access abstraction (via EF Core)
- **Entities**: Database models
- **Domain Models**: Business logic models
- **DTOs**: Request/Response data transfer objects

### Multi-Tenancy
- **Shared Schema**: Single database with `TenantId` column on tenant-bound entities
- **Tenant Resolution**: URL path prefix (`/{tenantSlug}/api/...`)
- **Global Query Filters**: Automatic tenant isolation at EF Core level
- **Middleware**: Tenant resolution and authorization enforcement

## Key Features

### 🔐 Authentication & Authorization
- **JWT Bearer Authentication** with tenant-aware claims
- **Policy-based Authorization** with custom attributes
- **Role-based Access Control** with fine-grained permissions
- **Tenant Isolation** enforced at middleware and database levels

### 👥 User Roles
- **SuperAdmin**: Global administrator with access to all tenants
- **TenantAdmin**: HR Admin who manages users, assigns surveys, views reports
- **Participant**: Users who fill surveys assigned to them

### 🏢 Company Onboarding
Complete company registration with:
- Company details (name, employees, location, industry)
- Contact person information
- Automatic tenant and admin user creation

### 🛡️ Security Features
- **Tenant Authorization Middleware**: Validates JWT tenant claims against URL
- **Global Query Filters**: Automatic tenant data isolation
- **Custom Authorization Attributes**: `[AuthorizeTenant]`, `[AuthorizeSuperAdmin]`, etc.
- **Automatic TenantId Assignment**: On entity creation

## Database Schema

### Core Entities
- **Tenants**: Company/organization containers
- **Companies**: Company information and contact details
- **Users**: Extended ASP.NET Identity users with tenant association
- **Roles**: Tenant-scoped roles with permissions
- **Permissions**: Fine-grained access control
- **UserTenantRoles**: Many-to-many user-role assignments with tenant scope

### Relationships
```
Tenant 1:1 Company
Tenant 1:* Users
Tenant 1:* TenantRoles
User *:* TenantRole (via UserTenantRole)
TenantRole *:* Permission (via RolePermission)
```

## API Endpoints

### Authentication
```
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/change-password
POST /api/auth/validate-token
```

### Tenant Management (SuperAdmin only)
```
POST /api/tenant
GET  /api/tenant
GET  /api/tenant/{id}
GET  /api/tenant/by-slug/{slug}
PUT  /api/tenant/{id}
POST /api/tenant/{id}/activate
POST /api/tenant/{id}/deactivate
```

### Tenant-Scoped User Management
```
GET  /{tenantSlug}/api/users
GET  /{tenantSlug}/api/users/{id}
POST /{tenantSlug}/api/users
POST /{tenantSlug}/api/users/{id}/roles
POST /{tenantSlug}/api/users/{id}/activate
POST /{tenantSlug}/api/users/{id}/deactivate
GET  /{tenantSlug}/api/users/roles
```

### Company Management
```
GET /{tenantSlug}/api/company
PUT /{tenantSlug}/api/company
```

## Configuration

### JWT Settings (appsettings.json)
```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere",
    "Issuer": "NomadSurveys",
    "Audience": "NomadSurveysUsers",
    "ExpiryInHours": 24
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=nomad_surveys;Username=user;Password=pass"
  }
}
```

## Usage Examples

### 1. Create a New Tenant
```bash
curl -X POST "http://localhost:5231/api/tenant" \
  -H "Authorization: Bearer {superadmin-jwt}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corporation",
    "slug": "acme-corp",
    "description": "Technology company",
    "company": {
      "name": "Acme Corporation",
      "numberOfEmployees": 500,
      "location": "New York, USA",
      "industry": "Technology",
      "contactPersonName": "John Doe",
      "contactPersonEmail": "john.doe@acme.com",
      "contactPersonRole": "HR Manager"
    },
    "tenantAdmin": {
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@acme.com",
      "password": "SecurePassword123!"
    }
  }'
```

### 2. Login to Tenant
```bash
curl -X POST "http://localhost:5231/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@acme.com",
    "password": "SecurePassword123!",
    "tenantSlug": "acme-corp"
  }'
```

### 3. Access Tenant-Scoped Resources
```bash
curl -X GET "http://localhost:5231/acme-corp/api/users" \
  -H "Authorization: Bearer {tenant-admin-jwt}"
```

## Security Considerations

### Tenant Isolation
- All tenant-bound entities automatically filtered by `TenantId`
- Middleware validates JWT tenant claims against URL path
- SuperAdmin can bypass tenant restrictions

### JWT Claims Structure
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "TenantId": "tenant-guid",
  "role": ["TenantAdmin"],
  "Permission": ["manage_users", "view_reports"]
}
```

### Authorization Policies
- **SuperAdmin**: Full system access
- **TenantAdmin**: Full access within tenant
- **Participant**: Limited access to assigned surveys
- **TenantUser**: Basic tenant user access

## Development Setup

### Prerequisites
- .NET 8 SDK
- PostgreSQL database
- Entity Framework Core tools

### Running the Application
```bash
cd Nomad-Surveys/Nomad-Api
dotnet restore
dotnet ef database update
dotnet run --project Nomad.Api
```

### Testing Endpoints
- Health Check: `GET /api/test/health`
- Swagger UI: `http://localhost:5231/swagger`
- Tenant Info: `GET /{tenantSlug}/api/test/tenant-info`

## Seed Data

The system includes a comprehensive seed service that creates:
- Default permissions and roles
- SuperAdmin user
- Sample tenant with company and users
- Role-permission assignments

**Default Credentials:**
- SuperAdmin: `superadmin@nomadsurveys.com` / `SuperAdmin123!`
- Sample Tenant Admin: `admin@acmecorp.com` / `TenantAdmin123!`
- Sample Participant: `participant@acmecorp.com` / `Participant123!`

## Production Deployment

### Security Checklist
- [ ] Change default JWT secret key
- [ ] Use strong database passwords
- [ ] Enable HTTPS in production
- [ ] Configure proper CORS policies
- [ ] Set up proper logging and monitoring
- [ ] Implement rate limiting
- [ ] Configure proper error handling

### Performance Considerations
- Database indexing on `TenantId` columns
- Connection pooling configuration
- Caching for frequently accessed data
- Proper pagination for large datasets

## Extensibility

The system is designed for easy extension:
- Add new permissions via database seeding
- Create custom authorization policies
- Extend user properties and roles
- Add new tenant-scoped entities
- Implement custom middleware

## Support

For questions or issues, please refer to the API documentation available at `/swagger` when running the application.
