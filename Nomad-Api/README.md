# Nomad Surveys API

ASP.NET 8 Web API for the Nomad Survey Builder application with Entity Framework Core and PostgreSQL.

## Features

- **ASP.NET 8 Web API** with controllers
- **Entity Framework Core 9.0** with PostgreSQL provider
- **Database Migrations** support
- **Health Check** endpoints
- **CRUD Operations** for surveys
- **Domain Repository Pattern** ready structure

## Database Configuration

The application is configured to connect to a PostgreSQL database hosted on Render.com:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=dpg-d307kf56ubrc73ek00s0-a.singapore-postgres.render.com;Database=nomad_surveys;Username=nomad_surveys_user;Password=1C0OhZHt7d0z3xhWxIOWOSOBc3fXdMVn;Port=5432;SSL Mode=Require;Trust Server Certificate=true;"
  }
}
```

## Project Structure

```
Nomad.Api/
├── Controllers/
│   ├── SurveysController.cs    # CRUD operations for surveys
│   ├── HealthController.cs     # Health check endpoints
│   └── WeatherForecastController.cs
├── Data/
│   └── NomadSurveysDbContext.cs # Entity Framework DbContext
├── Models/
│   └── Survey.cs               # Survey entity model
├── Migrations/                 # EF Core migrations
├── Program.cs                  # Application startup
└── appsettings.json           # Configuration

Nomad.Api.Tests/
├── UnitTest1.cs               # Sample unit test
└── Nomad.Api.Tests.csproj     # Test project file
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL database (already configured)

### Running the Application

1. **Build the solution:**
   ```bash
   dotnet build
   ```

2. **Run database migrations:**
   ```bash
   dotnet ef database update --project Nomad.Api
   ```

3. **Start the API:**
   ```bash
   dotnet run --project Nomad.Api
   ```

4. **Access the API:**
   - API Base URL: `http://localhost:5231`
   - Health Check: `GET /api/health`
   - Database Health: `GET /api/health/database`
   - Surveys: `GET /api/surveys`

### Running Tests

```bash
dotnet test
```

## API Endpoints

### Health Checks
- `GET /api/health` - General health check
- `GET /api/health/database` - Database connectivity check

### Surveys
- `GET /api/surveys` - Get all surveys
- `GET /api/surveys/{id}` - Get survey by ID
- `POST /api/surveys` - Create new survey
- `PUT /api/surveys/{id}` - Update survey
- `DELETE /api/surveys/{id}` - Delete survey

## Database Schema

### Surveys Table
- `Id` (int, primary key, auto-increment)
- `Title` (varchar(200), required)
- `Description` (varchar(1000), optional)
- `CreatedAt` (timestamp, default: current timestamp)
- `UpdatedAt` (timestamp, optional)
- `IsActive` (boolean, default: true)
- `CreatedBy` (varchar(100), required)

## Development

### Adding New Migrations

```bash
dotnet ef migrations add MigrationName --project Nomad.Api
dotnet ef database update --project Nomad.Api
```

### Package Dependencies

- Microsoft.EntityFrameworkCore (9.0.9)
- Microsoft.EntityFrameworkCore.Design (9.0.9)
- Npgsql.EntityFrameworkCore.PostgreSQL (9.0.4)
- Microsoft.AspNetCore.OpenApi (9.0.6)

## Next Steps

1. Implement additional entities (Questions, Responses, etc.)
2. Add authentication and authorization
3. Implement repository pattern
4. Add comprehensive unit tests
5. Set up API documentation with Swagger
6. Add logging and monitoring
