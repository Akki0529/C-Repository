# Development Environment Setup

## Overview

This guide walks you through setting up your local development environment for the Library Management System API. You'll learn how to configure your ASP.NET Core application and run it locally with an in-memory database.

The provided starter project includes pre-configured packages, so you can focus on building the core functionality of your library system.

---

## Project Structure

```
Capstone/
├── Controllers/                    # API controllers (you will create)
├── Models/                         # Entity models and DTOs (you will create)
├── Services/                       # Business logic services (you will create)
├── Data/                           # DbContext (you will create)
├── docs/                           # Project documentation
├── appsettings.json               # Base application configuration (PROVIDED)
├── appsettings.Development.json   # Development configuration (PROVIDED)
├── Program.cs                     # Application entry point
└── Capstone.csproj                # Project file (PROVIDED)
```

---

## Provided Configuration

### Capstone.csproj (Project File)

This file has been pre-configured with all necessary NuGet packages:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.9" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.4" />
<PackageReference Include="FluentValidation" Version="11.8.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

### appsettings.Development.json

Contains configuration for local development with in-memory database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "InMemory"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Getting Started

### Prerequisites

- **.NET 9.0 SDK** - Check version with `dotnet --version`
- **IDE** - Visual Studio 2022, VS Code, or JetBrains Rider
- **Git** - Version control

**No database installation required!** You'll use an in-memory database for development.

### Step 1: Restore Dependencies

```bash
# Navigate to your project directory
cd Capstone

# Restore NuGet packages
dotnet restore
```

### Step 2: Build the Project

```bash
# Build the project
dotnet build
```

### Step 3: Run the Application

```bash
# Run the application
dotnet run
```

The application will be accessible at:
- **HTTP**: `http://localhost:{port}`
- **Swagger UI**: `https://localhost:{port}/swagger`

### Step 4: Verify Everything Works

Open your browser and navigate to:
```
https://localhost:{port}/swagger
```

You should see the Swagger UI interface displaying your API documentation.

---

## In-Memory Database

The in-memory database is perfect for local development:

- **No installation needed** - Works immediately
- **Fast** - Everything runs in memory
- **Fresh start** - Data resets each time you restart the application
- **Easy testing** - Clean slate for every development session

### Configuring DbContext

In your `Program.cs`, configure the in-memory database:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("LibraryDb"));
```

### Creating Seed Data

After you create your entity classes in Milestone 1: Data Modeling, you'll need sample data for testing. You can add a data seeder that runs when your application starts.

Note: The code below is just an example structure. You'll create the actual `Book`, `User`, and `Reservation` entities with their proper properties and relationships in Milestone 1.

```csharp
public class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.Books.AnyAsync())
        {
            var books = new List<Book>
            {
                new Book
                {
                    BookId = Guid.NewGuid(),
                    Isbn = "978-0-13-468599-1",
                    Title = "Clean Code",
                    Author = "Robert C. Martin",
                    Genre = "Technology",
                    TotalCopies = 5,
                    AvailableCopies = 5
                }
            };
            
            await context.Books.AddRangeAsync(books);
            await context.SaveChangesAsync();
        }
    }
}
```

Call the seeder in `Program.cs`:

```csharp
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await DataSeeder.SeedAsync(context);
```

---

## Daily Development Workflow

**Start your day:**
```bash
dotnet run
```

**Make changes:**
- Edit your code in your IDE
- Save files
- Stop the application (`Ctrl+C`)
- Run `dotnet run` again

**Optional - Hot Reload:**
```bash
dotnet watch run
# Application automatically restarts when you save changes
```

---

## Common Issues and Solutions

### Port Already in Use

**Solution:**
```bash
# Windows PowerShell
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess

# Kill the process or change ports in appsettings.json
```

### Cannot Access Swagger

**Solution:**
- Use HTTP: `http://localhost:{port}/swagger` (not HTTPS)

### Data Disappears After Restart

**This is expected!** The in-memory database doesn't persist data.
- Use the data seeder to repopulate data on each startup

---

## Next Steps

You're ready to start building! Proceed to:
- **Milestone 1: Data Modeling** - Create your entity classes and DbContext
- **Milestone 2: Authentication** - Implement user registration and JWT authentication
- **Milestone 3: Catalog Service** - Build book browsing functionality
- **Milestone 4: Reservation Service** - Implement reservation management
- **Milestone 5: Testing** - Write comprehensive tests
- **Milestone 6: Deployment** - Deploy to AWS Elastic Beanstalk with RDS PostgreSQL

---

## Important Reminders

- Data resets on every application restart (in-memory database behavior)
- Always test using HTTP (`http://localhost:{port}`)
- Swagger UI is your friend for testing endpoints
- Check console logs for errors and information

Good luck building your Library Management System!