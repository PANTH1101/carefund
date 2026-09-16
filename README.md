# NGO Donation Management System

A comprehensive donation management platform built with ASP.NET Core MVC that connects NGOs with donors.

## Technology Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core MVC** - Web application framework
- **Entity Framework Core** - ORM for database operations
- **SQL Server** - Database (running in Docker)
- **ASP.NET Core Identity** - Authentication and authorization
- **Bootstrap** - Frontend styling
- **C#** - Programming language

## Features

### Phase 1 - Foundation (Completed ✅)

- User authentication and authorization system
- Role-based access control (Admin, NGO, Donor)
- Database schema with proper relationships
- One user = one role enforcement (unique constraint)

### Database Models

1. **ApplicationUser** - Extends IdentityUser with FullName
2. **NGO** - NGO profile with verification status
3. **NGODocument** - Document storage for NGO verification
4. **Campaign** - Fundraising campaigns created by NGOs
5. **Donation** - Donation records linking donors, NGOs, and campaigns
6. **Payment** - Payment transaction details

### Key Relationships

- One ApplicationUser → One Role (enforced by unique index)
- One ApplicationUser (NGO) → One NGO entity
- One NGO → Many Campaigns
- One NGO → Many Documents
- One Campaign → Many Donations
- One ApplicationUser (Donor) → Many Donations
- One Donation → One Payment

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for SQL Server)
- macOS, Windows, or Linux

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/PANTH1101/ngo-donation-management-system.git
cd ngo-donation-management-system
```

### 2. Start SQL Server in Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sql_server_container \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 3. Configure Connection String

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NGODonationDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  }
}
```

### 4. Apply Database Migrations

```bash
dotnet ef database update
```

This will:
- Create the `NGODonationDB` database
- Create all required tables
- Seed the roles (Admin, NGO, Donor)

### 5. Run the Application

```bash
dotnet run
```

The application will be available at `http://localhost:5212`

## Database Structure

### Identity Tables
- AspNetUsers
- AspNetRoles
- AspNetUserRoles (with unique constraint on UserId)
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims

### Application Tables
- NGOs
- NGODocuments
- Campaigns
- Donations
- Payments

## Project Structure

```
NGODonationSystem/
├── Controllers/          # MVC Controllers
├── Data/                # Database context
├── Models/              # Entity models
├── Views/               # Razor views
├── wwwroot/            # Static files
├── Migrations/         # EF Core migrations
├── Program.cs          # Application entry point
└── appsettings.json    # Configuration
```

## Development Notes

### Architecture

This is a **simple MVC application** designed for educational purposes. It follows a straightforward architecture:

- Browser → Controller → EF Core/DbContext → SQL Server
- No service layer, repository pattern, or complex abstractions
- Business logic can be in controllers for simplicity
- Easy to understand and explain in academic settings

### Role Enforcement

The system enforces **one user = one role** at the database level through a unique index on `AspNetUserRoles.UserId`. This prevents users from having multiple roles simultaneously.

### Password Policy

The default password policy requires:
- At least 6 characters
- At least one digit
- At least one uppercase letter
- At least one lowercase letter
- Special characters are optional

## Migrations

The project includes the following migrations:

1. **InitialCreate** - Creates all tables and relationships
2. **EnforceOneUserOneRole** - Adds unique constraint on AspNetUserRoles.UserId

To add a new migration:
```bash
dotnet ef migrations add MigrationName
```

To apply migrations:
```bash
dotnet ef database update
```

## Building the Project

```bash
dotnet build
```

## Security Considerations

⚠️ **Important Security Notes:**

1. Never commit real passwords to source control
2. Use User Secrets for development: `dotnet user-secrets init`
3. Store production connection strings in secure configuration
4. The password in appsettings.json is a placeholder

## Future Phases

- Phase 2: Registration, Login, and NGO Verification
- Phase 3: Campaign Management
- Phase 4: Donation Processing with Razorpay
- Phase 5: Receipt Generation with QuestPDF
- Phase 6: Email Notifications

## License

This project is for educational purposes.

## Author

Panth Chauhan

## Support

For issues or questions, please open an issue on GitHub.
