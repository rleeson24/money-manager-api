# Money Manager API

ASP.NET Core Web API for the Money Manager application.

## Solution Structure

The solution follows a clean architecture pattern with the following projects:

- **MoneyManager.API** - Web API project containing controllers and configuration
- **MoneyManager.Core** - Business logic layer containing use cases, models, and mappers
- **MoneyManager.Data** - Data access layer containing repositories, database models, and utilities

## Prerequisites

- .NET 10.0 SDK
- SQL Server (cloud or local)
- Azure Key Vault (optional, for production secrets)

## Configuration

### appsettings.json

Configure the following settings:

- **ConnectionStrings:DefaultConnection** - SQL Server connection string
- **JwtToken:SecretKey** - Secret key for JWT token generation
- **JwtToken:Issuer** - JWT token issuer
- **JwtToken:Audience** - JWT token audience
- **AllowedOrigins** - CORS allowed origins (semicolon-separated)
- **AzureAd** - Azure AD configuration for authentication
- **ApplicationInsights:ConnectionString** - Application Insights connection string (optional)
- **KeyVaultName** - Azure Key Vault name (optional)

### Database Setup

Run the SQL script in `Database/CreateTables.sql` to create the required database tables.

## Endpoints

### Expenses

- `GET /api/expenses` - Get all expenses (optional `month` query parameter: YYYY-MM format)
- `GET /api/expenses/{id}` - Get a specific expense
- `POST /api/expenses` - Create a new expense
- `PUT /api/expenses/{id}` - Update an expense
- `PATCH /api/expenses/{id}` - Partially update an expense
- `DELETE /api/expenses/{id}` - Delete an expense
- `PATCH /api/expenses/bulk` - Bulk update expenses
- `DELETE /api/expenses/bulk` - Bulk delete expenses

### Categories

- `GET /api/categories` - Get all categories

### Payment Methods

- `GET /api/payment-methods` - Get all payment methods

## Security

The API uses:
- JWT Bearer authentication
- Azure AD authentication (Microsoft Identity)
- CORS configuration
- Authorization policies

All endpoints require authentication except for health checks.

## Running the Application

```bash
dotnet run --project src/main/MoneyManager.API/MoneyManager.API.csproj
```

The API will be available at `https://localhost:5001` (or the configured port).

## Development

### Building

```bash
dotnet build
```

### Running Tests

Tests can be added to a separate test project following the same structure.

## Architecture

The application follows a clean architecture pattern:

1. **API Layer** - Controllers handle HTTP requests/responses
2. **Core Layer** - Use cases contain business logic
3. **Data Layer** - Repositories handle database operations

This separation ensures:
- Testability
- Maintainability
- Scalability
