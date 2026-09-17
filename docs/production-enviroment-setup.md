# AWS Deployment Guide: RDS & Elastic Beanstalk for Microservices

## Overview

This guide provides instructions for deploying your three ASP.NET Core microservices to AWS Elastic Beanstalk with RDS
PostgreSQL databases.

---

## Part 1: RDS PostgreSQL Database Setup

### Create Database Instances

You have two options:

1. **Three separate database instances** (more isolated, higher cost)
2. **One database instance with three databases** (recommended, cost-effective)

We'll use **Option 2**: One RDS instance with three databases. (db.t3.micro)

**AWS Console → RDS → Create database**

**Required Configuration:**

| Setting                | Value                      | Notes                               |
|------------------------|----------------------------|-------------------------------------|
| Deployment             | Single-AZ DB instance      | Cost-effective option               |
| DB instance identifier | `library-microservices-db` | Unique name for your instance       |
| Master username        | `postgres`                 | Database admin user                 |
| Master password        | Create secure password     | Save this - required for connection |
| Credentials management | Self managed               | Manual password control             |
| Instance class         | `db.t3.micro`             | Burstable classes section           |
| Storage type           | General Purpose SSD (gp2)  | Default option                      |
| Allocated storage      | 20 GiB                     | As specified                        |
| Compute resource       | Don't connect to EC2       | Manual configuration                |
| Network type           | IPv4                       | Standard                            |
| VPC                    | Default VPC                | Must match Elastic Beanstalk        |
| DB subnet group        | default                    | Use existing                        |
| Public access          | yes                         | Security best practice              |
| VPC security group     | default                    | Will configure later                |
| Initial database name  | `userservicedb`            | First database (User Service)       |

**After Creation:**

- Wait for status to show "Available" (5-10 minutes)
- Navigate to database in RDS console
- Copy the **Endpoint** from Connectivity & security tab
- Format: `library-microservices-db.xxxxx.us-east-1.rds.amazonaws.com`
- Save this endpoint for application configuration

### Create Additional Databases

Connect to your RDS instance and create the other two databases:

```bash
# Install PostgreSQL client (if not already installed)
# macOS: brew install postgresql
# Ubuntu: sudo apt-get install postgresql-client

# Connect to RDS instance
psql -h library-microservices-db.xxxxx.us-east-1.rds.amazonaws.com -U postgres -d postgres

# Create additional databases
CREATE DATABASE catalogservicedb;
CREATE DATABASE reservationservicedb;

# Verify databases
\l

# Exit
\q
```


- Note after this you can turn public access off again, you just need this to create the extra databases
---

## Part 2: Application Preparation

### Install Required NuGet Packages

For each microservice, add PostgreSQL support:

```bash
# Navigate to each service directory and run:
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.10
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.0.10
```

### Update Program.cs for Production

For each service, modify database configuration:

```csharp
// Configure database based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<YourDbContext>(options =>
        options.UseInMemoryDatabase("YourServiceDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<YourDbContext>(options =>
        options.UseNpgsql(connectionString));
}
```

### Create Entity Framework Migrations

For each service:

```bash
# Install EF Core Tools globally (once)
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"

# User Service
cd UserService
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef migrations list
cd ..

# Catalog Service
cd CatalogService
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef migrations list
cd ..

# Reservation Service
cd ReservationService
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef migrations list
cd ..
```

### Build Production Packages

Build each microservice:

```bash
# User Service
cd UserService
dotnet clean
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../UserService.zip .
cd ../..

# Catalog Service
cd CatalogService
dotnet clean
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../CatalogService.zip .
cd ../..

# Reservation Service
cd ReservationService
dotnet clean
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../ReservationService.zip .
cd ../..

# Verify migrations are included
dotnet ef migrations list
```

---

## Part 3: Elastic Beanstalk Deployment

### Deploy User Service (Port 5001)

**AWS Console → Elastic Beanstalk → Create application**

**Configuration:**

| Setting          | Value                                               |
|------------------|-----------------------------------------------------|
| Environment tier | Web server environment                              |
| Application name | `user-service`                                      |
| Environment name | `user-service-env`                                  |
| Domain           | Leave blank (auto-generated)                        |
| Platform         | .NET Core on Linux                                  |
| Platform branch  | .NET 10 running on 64bit Amazon Linux |
| Platform version | Latest recommended version                          |
| Application code | Upload your code                                    |
| Version label    | `v1.0.0`                                            |
| Source           | Local file → Select `UserService.zip`               |
| Presets          | Single instance (free tier)                         |

**Environment Variables for User Service:**

* Don't include the `[]` in your actual values, replace them

| Name                                   | Value                                                                                             | Description                                          |
|----------------------------------------|---------------------------------------------------------------------------------------------------|------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                                                      | Activates production configuration                   |
| `ConnectionStrings__DefaultConnection` | `Host=[RDS-ENDPOINT];Port=5432;Database=userservicedb;Username=postgres;Password=[YOUR-PASSWORD]` | PostgreSQL connection string                         |
| `Jwt__Secret`                          | `[generate-secure-secret]`                                                                        | Generate with: `openssl rand -base64 32`             |
| `Jwt__Issuer`                          | `LibraryManagementApi`                                                                            | JWT token issuer                                     |
| `Jwt__Audience`                        | `LibraryManagementApiUsers`                                                                       | JWT token audience                                   |
| `ServiceUrls__ReservationService`      | `http://reservation-service-env.us-east-1.elasticbeanstalk.com`                                   | URL of Reservation Service (update after deployment) |

### Deploy Catalog Service (Port 5002)

Repeat the Elastic Beanstalk creation process with these values:

**Configuration:**

| Setting          | Value                                    |
|------------------|------------------------------------------|
| Application name | `catalog-service`                        |
| Environment name | `catalog-service-env`                    |
| Source           | Local file → Select `CatalogService.zip` |

**Environment Variables for Catalog Service:**

| Name                                   | Value                                                                                                | Description                        |
|----------------------------------------|------------------------------------------------------------------------------------------------------|------------------------------------|
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                                                         | Activates production configuration |
| `ConnectionStrings__DefaultConnection` | `Host=[RDS-ENDPOINT];Port=5432;Database=catalogservicedb;Username=postgres;Password=[YOUR-PASSWORD]` | PostgreSQL connection string       |

### Deploy Reservation Service (Port 5003)

Repeat the Elastic Beanstalk creation process with these values:

**Configuration:**

| Setting          | Value                                        |
|------------------|----------------------------------------------|
| Application name | `reservation-service`                        |
| Environment name | `reservation-service-env`                    |
| Source           | Local file → Select `ReservationService.zip` |

**Environment Variables for Reservation Service:**

| Name                                   | Value                                                                                                    | Description                        |
|----------------------------------------|----------------------------------------------------------------------------------------------------------|------------------------------------|
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                                                             | Activates production configuration |
| `ConnectionStrings__DefaultConnection` | `Host=[RDS-ENDPOINT];Port=5432;Database=reservationservicedb;Username=postgres;Password=[YOUR-PASSWORD]` | PostgreSQL connection string       |
| `Jwt__Secret`                          | `[same-as-user-service]`                                                                                 | Must match User Service JWT secret |
| `Jwt__Issuer`                          | `LibraryManagementApi`                                                                                   | Must match User Service            |
| `Jwt__Audience`                        | `LibraryManagementApiUsers`                                                                              | Must match User Service            |
| `ServiceUrls__UserService`             | `http://user-service-env.us-east-1.elasticbeanstalk.com`                                                 | URL of User Service                |
| `ServiceUrls__CatalogService`          | `http://catalog-service-env.us-east-1.elasticbeanstalk.com`                                              | URL of Catalog Service             |

**Note:** After deploying each service, copy its URL and update the other services' environment variables.

---

## Part 4: Security Configuration

### Update RDS Security Group

After all services and RDS are running:

1. **AWS Console → EC2 → Security Groups**
2. Find the RDS security group (check RDS instance details for security group ID)
3. Click **Edit inbound rules**
4. Add three inbound rules:
   - **Type:** PostgreSQL, **Port:** 5432, **Source:** User Service security group
   - **Type:** PostgreSQL, **Port:** 5432, **Source:** Catalog Service security group
   - **Type:** PostgreSQL, **Port:** 5432, **Source:** Reservation Service security group
5. Click **Save rules**

### Allow Inter-Service Communication

1. **AWS Console → EC2 → Security Groups**
2. For each Elastic Beanstalk security group:
   - Click **Edit inbound rules**
   - Add HTTP rule allowing traffic from other service security groups
   - **Type:** HTTP, **Port:** 80, **Source:** Other services' security groups

---

## Verification

### Test Each Service

**User Service:**

```bash
# Health check
curl http://user-service-env.us-east-1.elasticbeanstalk.com/health

# Swagger UI
http://user-service-env.us-east-1.elasticbeanstalk.com/swagger

# Register a user
curl -X POST http://user-service-env.us-east-1.elasticbeanstalk.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "firstName": "Test",
    "lastName": "User",
    "phoneNumber": "+1-555-0123"
  }'

# Login
curl -X POST http://user-service-env.us-east-1.elasticbeanstalk.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

**Catalog Service:**

```bash
# Health check
curl http://catalog-service-env.us-east-1.elasticbeanstalk.com/health

# Browse catalog
curl http://catalog-service-env.us-east-1.elasticbeanstalk.com/api/catalog/books
```

**Reservation Service:**

```bash
# Health check
curl http://reservation-service-env.us-east-1.elasticbeanstalk.com/health

# Create reservation (requires token from User Service login)
curl -X POST http://reservation-service-env.us-east-1.elasticbeanstalk.com/api/reservations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer [YOUR-JWT-TOKEN]" \
  -d '{
    "bookId": "[BOOK-ID-FROM-CATALOG]"
  }'
```

### Check Application Logs

For each service:

- **Elastic Beanstalk Console → [Service] → Logs → Request Logs → Last 100 Lines**

Look for:

- Successful database connection
- Applied migrations
- Application startup confirmation

---

## Common Issues & Solutions

### Issue: Service Can't Connect to Database

**Check:**

- Connection string format is correct with double underscores (`__`)
- RDS endpoint matches exactly
- Database name is correct for each service
- RDS security group allows inbound from all service security groups
- All services and RDS are in the same VPC
- Database credentials are correct
- RDS instance status is "Available"

### Issue: Services Can't Communicate

**Check:**

- Service URLs are correct in environment variables
- Security groups allow HTTP traffic between services
- All services are running (green health status)
- Network is configured correctly (same VPC)

### Issue: JWT Token Validation Fails

**Check:**

- JWT secret is identical across User Service and Reservation Service
- Jwt__Issuer and Jwt__Audience match across services
- Token is being sent with "Bearer " prefix

### Issue: Migrations Not Applied

**Check:**

- Migrations folder is included in ZIP files
- `MigrateAsync()` is called in Program.cs
- Application has permission to create tables
- Check logs for migration errors

---

## Deployment Checklist

### RDS Setup

- [ ] RDS PostgreSQL instance created (`library-microservices-db`)
- [ ] Database status is "Available"
- [ ] RDS endpoint documented and saved
- [ ] Three databases created (userservicedb, catalogservicedb, reservationservicedb)

### User Service

- [ ] User Service ZIP built with migrations
- [ ] Elastic Beanstalk environment created
- [ ] All environment variables configured
- [ ] Environment health shows "Ok" (green)
- [ ] Swagger UI accessible
- [ ] User registration works
- [ ] User login returns JWT token

### Catalog Service

- [ ] Catalog Service ZIP built with migrations
- [ ] Elastic Beanstalk environment created
- [ ] All environment variables configured
- [ ] Environment health shows "Ok" (green)
- [ ] Swagger UI accessible
- [ ] Catalog browsing works

### Reservation Service

- [ ] Reservation Service ZIP built with migrations
- [ ] Elastic Beanstalk environment created
- [ ] All environment variables configured
- [ ] Environment health shows "Ok" (green)
- [ ] Swagger UI accessible
- [ ] Can create reservations with authentication
- [ ] Waitlist expiry background job is running (check application logs for its periodic output)

### Security & Communication

- [ ] RDS security group allows all services to connect
- [ ] Service security groups allow inter-service communication
- [ ] User Service can call Reservation Service
- [ ] Reservation Service can call User Service
- [ ] Reservation Service can call Catalog Service

### End-to-End Testing

- [ ] Complete reservation workflow works (reserve → checkout → return)
- [ ] Profile endpoint shows statistics from Reservation Service
- [ ] Book availability updates via Catalog Service
- [ ] Role-based authorization enforced

---

## Additional Resources

- ASP.NET Core Configuration: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
- AWS Elastic Beanstalk .NET: https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-linux-platform.html
- AWS RDS PostgreSQL: https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html
- Entity Framework Core Migrations: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/
