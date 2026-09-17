# Milestone 2: User Service & Authentication

**Goal:** Build User Service for registration, authentication, and user profile management

**Related User Stories:** US-001 (User Registration), US-002 (User Authentication), US-003 (View Profile)

---

## Business Requirements

### User Registration (US-001)
- Library patrons must be able to create accounts with email and personal information
- Email addresses must be unique across the system
- Passwords must meet security requirements:
  - Minimum 8 characters
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one number
  - At least one special character
- Phone numbers must be in valid format
- New accounts default to:
  - Role: Patron
  - MembershipStatus: Active
  - MemberSince: Current date
- Returns user profile with success message upon registration
- Returns validation error if email already exists

### User Authentication (US-002)
- Registered users must be able to log in with email and password
- System issues JWT tokens for authenticated sessions
- Tokens include user claims (userId, email, role)
- Token validity period: 24 hours (86400 seconds)
- Returns access token with user profile information
- Returns authentication error for invalid credentials
- Password comparison uses secure hashing (BCrypt)

### Profile Viewing (US-003)
- Authenticated users can view their complete profile information
- Profile includes: userId, email, firstName, lastName, phoneNumber, role, membershipStatus, memberSince
- Profile displays statistics:
  - activeReservations: Count of reservations with Reserved or CheckedOut status
  - borrowingHistory: Total count of completed reservations
- Statistics are retrieved from Reservation Service via inter-service communication
- Requires valid JWT token for access

### User Validation (Internal)
- Provides internal endpoint for other services to validate users
- Used by Reservation Service to:
  - Validate user exists and is active
  - Check user's active reservation count
  - Verify user hasn't exceeded reservation limit
- Returns user information and reservation statistics

---

## General Technical Requirements

**Technology Stack:**
- ASP.NET Core 8.0 or 9.0
- Entity Framework Core 8.0+
- PostgreSQL 15+ (or in-memory for development)
- BCrypt for password hashing
- JWT Bearer authentication

**Security Requirements:**
- Passwords must be hashed using BCrypt before storage
- JWT tokens signed with secure secret key
- Tokens include expiration claims
- Sensitive endpoints require authentication
- Role-based authorization for administrative functions

**Service Communication:**
- User Service exposes HTTP endpoints for other services
- Reservation Service calls User Service for token validation
- User Service calls Reservation Service for statistics (profile endpoint)
- All inter-service calls use HTTP/REST

**Data Integrity:**
- Email uniqueness enforced at database level
- Audit fields (CreatedAt, UpdatedAt) automatically populated
- Membership date set on account creation

---

## Deliverables

### 1. User Entity and Database Context
Implement User entity with all required fields:
- UserId (Guid, Primary Key)
- Email (string, unique, required, max 255)
- PasswordHash (string, required)
- FirstName (string, required, max 100)
- LastName (string, required, max 100)
- PhoneNumber (string, required, max 20)
- Role (enum: Patron, Librarian)
- MembershipStatus (enum: Active, Suspended)
- MemberSince (DateTime?, nullable)
- CreatedAt, UpdatedAt (audit fields)

Create UserServiceContext with proper configuration:
- Configure unique index on Email
- Configure enum conversions
- Configure audit field auto-population
- Set up appropriate database constraints

### 2. Registration Endpoint
Implement endpoint that:
- Validates all input fields
- Checks email uniqueness
- Validates password strength requirements
- Validates phone number format
- Hashes password using BCrypt
- Creates user with default values (Patron role, Active status)
- Sets MemberSince to current date
- Returns user profile with success message
- Handles validation errors appropriately

### 3. Authentication Endpoint
Implement endpoint that:
- Validates email exists in database
- Compares password using BCrypt verification
- Generates JWT token with appropriate claims
- Sets token expiration to 24 hours
- Returns token with user profile information
- Returns authentication error for invalid credentials
- Logs failed authentication attempts

### 4. JWT Token Generation
Implement token service that:
- Creates JWT tokens with claims (userId, email, role)
- Signs tokens with secure secret key
- Sets appropriate expiration time
- Includes standard claims (iss, aud, exp, iat)
- Returns token in Bearer format

### 5. Profile Endpoint
Implement endpoint that:
- Extracts userId from JWT token claims
- Retrieves user record from database
- Calls Reservation Service to get statistics:
  - activeReservations count (Reserved or CheckedOut status)
  - borrowingHistory count (total reservations)
- Combines user data with statistics
- Returns complete profile information
- Handles cases where Reservation Service is unavailable

### 6. User Validation Endpoint (Internal)
Implement internal endpoint that:
- Validates user exists by userId
- Checks user's membership status is Active
- Returns user information
- Used by Reservation Service before creating reservations
- Returns appropriate error if user not found or suspended

### 7. Authentication Middleware
Configure JWT authentication middleware that:
- Validates token signature
- Checks token expiration
- Extracts claims for authorization
- Handles authentication failures
- Protects secured endpoints

---

## API Endpoints to Implement

Based on `api-contracts.md` and microservices architecture, implement these endpoints:

### POST /api/auth/register
- **Access:** Public (no authentication)
- **Request:** email, password, firstName, lastName, phoneNumber
- **Success (201):** userId, email, firstName, lastName, role, membershipStatus, createdAt, message
- **Error (400):** VALIDATION_ERROR if email exists or validation fails
- **Business Logic:**
  - Validate email format and uniqueness
  - Validate password meets security requirements
  - Validate phone number format
  - Hash password with BCrypt
  - Create user with Patron role, Active status
  - Set MemberSince to current date

### POST /api/auth/login
- **Access:** Public (no authentication)
- **Request:** email, password
- **Success (200):** accessToken, tokenType (Bearer), expiresIn (86400), user object
- **Error (401):** AUTHENTICATION_FAILED for invalid credentials
- **Business Logic:**
  - Look up user by email
  - Verify password hash using BCrypt
  - Generate JWT token with userId, email, role claims
  - Set token expiration to 24 hours
  - Return token and user profile

### GET /api/users/profile
- **Access:** Requires authentication (Bearer token)
- **Success (200):** Complete user profile with statistics
- **Error (401):** UNAUTHORIZED if token missing/invalid
- **Business Logic:**
  - Extract userId from JWT claims
  - Retrieve user from database
  - Call Reservation Service GET /api/reservations/statistics/{userId}
  - Combine data and return complete profile
  - Handle Reservation Service unavailability gracefully

### GET /api/users/{userId}/validate (Internal)
- **Access:** Internal use by Reservation Service
- **Path Parameter:** userId (Guid)
- **Success (200):** userId, email, firstName, lastName, role, membershipStatus, activeReservationsCount
- **Error (404):** User not found
- **Error (400):** User suspended
- **Business Logic:**
  - Validate user exists
  - Check membership status is Active
  - Call Reservation Service to get active reservations count
  - Return user information with reservation count
- **Note:** This endpoint is used by Reservation Service to validate users before creating reservations

---

## Acceptance Criteria

- [ ] User Service runs independently on port 5001
- [ ] UserServiceContext configured with User entity
- [ ] Email uniqueness enforced at database level
- [ ] Passwords hashed with BCrypt (never stored as plain text)
- [ ] Registration endpoint validates all input fields
- [ ] Registration creates user with Patron role and Active status by default
- [ ] Registration sets MemberSince to current date
- [ ] Registration returns 400 if email already exists
- [ ] Login endpoint validates credentials correctly
- [ ] Login returns JWT token with 24-hour expiration
- [ ] JWT token includes userId, email, and role in claims
- [ ] Profile endpoint requires valid JWT token
- [ ] Profile endpoint extracts userId from token claims
- [ ] Profile endpoint calls Reservation Service for statistics
- [ ] Profile endpoint returns activeReservations and borrowingHistory counts
- [ ] Profile endpoint handles Reservation Service unavailability
- [ ] User validation endpoint returns user information
- [ ] User validation endpoint checks membership status
- [ ] Authentication middleware validates JWT tokens
- [ ] Protected endpoints return 401 without valid token
- [ ] Swagger documentation accessible at /swagger
- [ ] All endpoints follow API contract specifications

---

## Inter-Service Communication Examples

### Profile Statistics Flow:
1. User calls GET /api/users/profile with JWT token
2. User Service extracts userId from token
3. User Service retrieves user data from its database
4. User Service calls Reservation Service:
   - GET http://localhost:5003/api/reservations/statistics/{userId}
5. Reservation Service returns:
   ```json
   {
     "userId": "uuid-123",
     "activeReservations": 2,
     "borrowingHistory": 45
   }
   ```
6. User Service combines user data with statistics
7. User Service returns complete profile to user

### User Validation Flow (used by Reservation Service):
1. Reservation Service receives reservation creation request
2. Reservation Service extracts userId from JWT token
3. Reservation Service calls User Service:
   - GET http://localhost:5001/api/users/{userId}/validate
4. User Service validates user and returns:
   ```json
   {
     "userId": "uuid-123",
     "email": "john.doe@example.com",
     "firstName": "John",
     "lastName": "Doe",
     "role": "PATRON",
     "membershipStatus": "ACTIVE",
     "activeReservationsCount": 4
   }
   ```
5. Reservation Service checks if activeReservationsCount < 5
6. If valid, Reservation Service proceeds with reservation creation

---

## Suggested Approach

1. Create User Service ASP.NET Core project
2. Configure service to run on port 5001
3. Create User entity with all required properties
4. Define Role and MembershipStatus enums
5. Create UserServiceContext with proper configuration
6. Configure Entity Framework and database connection
7. Install required NuGet packages:
   - BCrypt.Net-Next (for password hashing)
   - Microsoft.AspNetCore.Authentication.JwtBearer
   - Microsoft.IdentityModel.Tokens
8. Implement password hashing service using BCrypt
9. Implement JWT token generation service
10. Create registration endpoint with validation
11. Create login endpoint with authentication logic
12. Configure JWT authentication middleware
13. Create profile endpoint with inter-service call to Reservation Service
14. Create user validation endpoint for other services
15. Configure HttpClient for calling Reservation Service
16. Set up Swagger documentation
17. Test all authentication flows
18. Test inter-service communication

**Note:** You have flexibility in how you structure your services, organize your code, and implement authentication logic. Focus on security best practices and proper error handling.

---

## Configuration Requirements

Configure in `appsettings.json`:

```json
{
  "urls": "http://localhost:5001",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=UserServiceDb;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Secret": "your-secure-secret-key-at-least-32-characters-long",
    "Issuer": "LibraryManagementSystem",
    "Audience": "LibraryUsers",
    "ExpiresInSeconds": 86400
  },
  "ServiceUrls": {
    "ReservationService": "http://localhost:5003"
  }
}
```

**Security Note:** 
- JWT Secret must be at least 32 characters for HS256
- Never commit secrets to source control
- Use environment variables for production secrets
- Same JWT Secret must be used across all services for token validation

---

## Password Validation Requirements

Implement validation to enforce password requirements:

```csharp
// Password must contain:
// - At least 8 characters
// - At least one uppercase letter (A-Z)
// - At least one lowercase letter (a-z)
// - At least one digit (0-9)
// - At least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)
```

Return descriptive error message if password fails validation.

---

## Resources

- Refer to `user-stories.md` for US-001, US-002, US-003 details
- Refer to `api-contracts.md` for exact request/response formats
- Refer to `milestone-1` for microservices architecture and entity definitions
- ASP.NET Core Authentication documentation
- JWT Bearer authentication documentation
- BCrypt.Net documentation for password hashing
- HttpClient documentation for inter-service communication
