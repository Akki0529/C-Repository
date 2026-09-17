## API Contracts

### Authentication Endpoints

#### POST /api/auth/register

Register a new user account with role defaulting to PATRON.

**Access:** Public (no authentication required)

**Request Body:**

```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1-555-0123"
}
```

**Success Response (201 Created):**

```json
{
  "userId": "uuid-123",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "PATRON",
  "membershipStatus": "ACTIVE",
  "createdAt": "2025-09-29T10:30:00Z",
  "message": "Registration successful"
}
```

**Error Response (400 Bad Request):**

```json
{
  "error": "VALIDATION_ERROR",
  "message": "Email already exists",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**Validation Rules:**

- Email must be valid format and unique
- Password minimum 8 characters with uppercase, lowercase, number, special character
- All fields are required
- Phone number must be valid format

---

#### POST /api/auth/login

Authenticate user credentials and receive JWT access token.

**Access:** Public (no authentication required)

**Request Body:**

```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass123!"
}
```

**Success Response (200 OK):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "user": {
    "userId": "uuid-123",
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "PATRON"
  }
}
```

**Error Response (401 Unauthorized):**

```json
{
  "error": "AUTHENTICATION_FAILED",
  "message": "Invalid email or password",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**Business Logic:**

- Verify email exists in database
- Compare password hash using BCrypt
- Generate JWT token with userId, email, role in claims
- Set token expiration to 24 hours (86400 seconds) from issue time
- Return user profile data along with token

### User Profile Endpoints

#### GET /api/users/profile

Retrieve complete profile for currently authenticated user.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Success Response (200 OK):**

```json
{
  "userId": "uuid-123",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1-555-0123",
  "role": "PATRON",
  "membershipStatus": "ACTIVE",
  "memberSince": "2025-01-15T00:00:00Z",
  "activeReservations": 2,
  "borrowingHistory": 45
}
```

**Business Logic:**

- Extract userId from JWT token claims
- Fetch user record from database
- Calculate activeReservations count (status = RESERVED or CHECKED_OUT)
- Calculate borrowingHistory count (total completed reservations)
- Return complete profile with calculated statistics

---

### Catalog Endpoints

#### GET /api/catalog/books

Retrieve paginated and sortable list of all books in the catalog. Supports filtering by combining with search
parameters.

**Access:** Public (no authentication required)

**Query Parameters:**

- `page` (integer, default: 0) - Zero-based page number
- `size` (integer, default: 20) - Number of books per page
- `sortBy` (string, default: "title") - Field to sort by (title, author, publicationYear)
- `sortOrder` (string, default: "asc") - Sort direction (asc, desc)
- `query` (string, optional) - Search term for title/author full-text search
- `genre` (string, optional) - Filter by specific genre
- `isbn` (string, optional) - Filter by exact ISBN match
- `availableOnly` (boolean, default: false) - If true, show only books with availableCopies > 0

**Example Request:**

```
GET /api/catalog/books?page=0&size=20&sortBy=title&sortOrder=asc
GET /api/catalog/books?query=clean&genre=Technology&availableOnly=true
```

**Success Response (200 OK):**

```json
{
  "content": [
    {
      "bookId": "uuid-book-1",
      "isbn": "978-0-13-468599-1",
      "title": "Clean Code",
      "author": "Robert C. Martin",
      "genre": "Technology",
      "publicationYear": 2008,
      "description": "A handbook of agile software craftsmanship",
      "totalCopies": 5,
      "availableCopies": 2,
      "status": "AVAILABLE"
    },
    {
      "bookId": "uuid-book-2",
      "isbn": "978-0-13-475759-9",
      "title": "Refactoring",
      "author": "Martin Fowler",
      "genre": "Technology",
      "publicationYear": 2018,
      "description": "Improving the design of existing code",
      "totalCopies": 3,
      "availableCopies": 0,
      "status": "CHECKED_OUT"
    }
  ],
  "page": 0,
  "size": 20,
  "totalElements": 245,
  "totalPages": 13,
  "last": false
}
```

**Business Logic:**

- If `query` parameter provided, perform full-text search on title and author fields
- Apply genre filter if provided
- Apply ISBN exact match filter if provided
- If `availableOnly=true`, filter books where availableCopies > 0
- Apply sorting based on sortBy and sortOrder parameters
- Return paginated results with metadata (page, size, totalElements, totalPages, last)
- Status is determined by: AVAILABLE (availableCopies > 0), CHECKED_OUT (availableCopies = 0)

---

#### GET /api/catalog/books/{bookId}

Retrieve detailed information for a specific book by its unique identifier.

**Access:** Public (no authentication required)

**Path Parameters:**

- `bookId` (UUID, required) - Unique identifier of the book

**Success Response (200 OK):**

```json
{
  "bookId": "uuid-book-1",
  "isbn": "978-0-13-468599-1",
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "genre": "Technology",
  "publicationYear": 2008,
  "description": "A handbook of agile software craftsmanship",
  "publisher": "Prentice Hall",
  "pageCount": 464,
  "language": "English",
  "totalCopies": 5,
  "availableCopies": 2,
  "status": "AVAILABLE",
  "createdAt": "2025-01-10T09:00:00Z",
  "updatedAt": "2025-09-15T14:22:00Z"
}
```

**Error Response (404 Not Found):**

```json
{
  "error": "NOT_FOUND",
  "message": "Book not found with ID: uuid-book-1",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**Business Logic:**

- Fetch book record by bookId from database
- Return complete book details including metadata (publisher, pageCount, language)
- Include availability information (totalCopies, availableCopies, status)
- Include audit timestamps (createdAt, updatedAt)

---

### Reservation Endpoints

#### POST /api/reservations

Create a new reservation for an available book. User can have maximum 5 active reservations.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Request Body:**

```json
{
  "bookId": "uuid-book-1"
}
```

**Success Response (201 Created):**

```json
{
  "reservationId": "uuid-res-1",
  "bookId": "uuid-book-1",
  "userId": "uuid-123",
  "bookTitle": "Clean Code",
  "status": "RESERVED",
  "reservedAt": "2025-09-29T10:30:00Z",
  "expiresAt": "2025-10-06T10:30:00Z",
  "message": "Book reserved successfully. Please pick up within 7 days."
}
```

**Error Response (400 Bad Request - Limit Exceeded):**

```json
{
  "error": "RESERVATION_LIMIT_EXCEEDED",
  "message": "You have reached the maximum of 5 active reservations",
  "currentReservations": 5
}
```

**Error Response (400 Bad Request - Book Unavailable):**

```json
{
  "error": "BOOK_UNAVAILABLE",
  "message": "No copies available for reservation",
  "availableCopies": 0
}
```

**Business Logic:**

- Extract userId from JWT token
- Validate user has fewer than 5 active reservations (status = RESERVED or CHECKED_OUT)
- Verify book has availableCopies > 0
- Create reservation with status = RESERVED
- Set reservedAt to current timestamp
- Set expiresAt to 7 days from reservedAt
- Decrement book's availableCopies by 1
- Return reservation details with success message

---

#### GET /api/reservations

Retrieve all active reservations for the currently authenticated user.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Success Response (200 OK):**

```json
{
  "reservations": [
    {
      "reservationId": "uuid-res-1",
      "bookId": "uuid-book-1",
      "bookTitle": "Clean Code",
      "bookAuthor": "Robert C. Martin",
      "status": "RESERVED",
      "reservedAt": "2025-09-29T10:30:00Z",
      "expiresAt": "2025-10-06T10:30:00Z",
      "daysUntilExpiry": 7
    },
    {
      "reservationId": "uuid-res-2",
      "bookId": "uuid-book-2",
      "bookTitle": "Refactoring",
      "bookAuthor": "Martin Fowler",
      "status": "CHECKED_OUT",
      "checkedOutAt": "2025-09-20T14:00:00Z",
      "dueDate": "2025-10-04T14:00:00Z",
      "daysUntilDue": 5
    }
  ],
  "totalActive": 2
}
```

**Business Logic:**

- Extract userId from JWT token
- Fetch all reservations where userId matches and status IN (RESERVED, CHECKED_OUT)
- For RESERVED status: calculate daysUntilExpiry (expiresAt - current date)
- For CHECKED_OUT status: calculate daysUntilDue (dueDate - current date)
- Join with book data to include bookTitle and bookAuthor
- Return array of active reservations with totalActive count

---

#### POST /api/reservations/{reservationId}/checkout

Process book checkout at library desk. Converts RESERVED status to CHECKED_OUT and sets due date.

**Access:** Requires LIBRARIAN role

**Headers:**

```
Authorization: Bearer {token}
```

**Path Parameters:**

- `reservationId` (UUID, required) - Unique identifier of the reservation

**Request Body:**

```json
{
  "notes": "Book condition: Good"
}
```

**Success Response (200 OK):**

```json
{
  "reservationId": "uuid-res-1",
  "status": "CHECKED_OUT",
  "checkedOutAt": "2025-09-29T15:00:00Z",
  "dueDate": "2025-10-13T15:00:00Z",
  "message": "Book checked out successfully. Due date: October 13, 2025"
}
```

**Error Response (403 Forbidden):**

```json
{
  "error": "FORBIDDEN",
  "message": "Only librarians can checkout books",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**Error Response (400 Bad Request):**

```json
{
  "error": "INVALID_STATUS",
  "message": "Can only checkout reservations with RESERVED status",
  "currentStatus": "CHECKED_OUT"
}
```

**Business Logic:**

- Verify user has LIBRARIAN role
- Fetch reservation by reservationId
- Validate reservation status is RESERVED
- Update reservation:
  - Set status = CHECKED_OUT
  - Set checkedOutAt = current timestamp
  - Set dueDate = checkedOutAt + 14 days
  - Store optional notes
- Return updated reservation with formatted due date message

---

#### POST /api/reservations/{reservationId}/return

Process book return at library desk. Calculates late fees if overdue.

**Access:** Requires LIBRARIAN role

**Headers:**

```
Authorization: Bearer {token}
```

**Path Parameters:**

- `reservationId` (UUID, required) - Unique identifier of the reservation

**Request Body:**

```json
{
  "condition": "GOOD",
  "notes": "Returned in good condition"
}
```

**Success Response (200 OK - On Time):**

```json
{
  "reservationId": "uuid-res-1",
  "returnedAt": "2025-10-10T10:00:00Z",
  "lateDays": 0,
  "lateFee": 0.00,
  "message": "Book returned successfully"
}
```

**Success Response (200 OK - Late Return):**

```json
{
  "reservationId": "uuid-res-1",
  "returnedAt": "2025-10-15T10:00:00Z",
  "dueDate": "2025-10-13T15:00:00Z",
  "lateDays": 2,
  "lateFee": 2.00,
  "message": "Book returned. Late fee of $2.00 applied to account."
}
```

**Error Response (403 Forbidden):**

```json
{
  "error": "FORBIDDEN",
  "message": "Only librarians can process returns",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**Error Response (400 Bad Request):**

```json
{
  "error": "INVALID_STATUS",
  "message": "Can only return books with CHECKED_OUT status",
  "currentStatus": "RESERVED"
}
```

**Business Logic:**

- Verify user has LIBRARIAN role
- Fetch reservation by reservationId
- Validate reservation status is CHECKED_OUT
- Update reservation:
  - Set status = RETURNED
  - Set returnedAt = current timestamp
  - Store condition (GOOD, FAIR, POOR, DAMAGED)
  - Store optional notes
- Calculate late fees:
  - If returnedAt > dueDate: lateDays = days between dueDate and returnedAt
  - lateFee = lateDays × $1.00 per day
  - Store lateDays and lateFee in reservation
- Check for an eligible waitlist entry on this book (see `POST /api/reservations/waitlist` section below
  for what "eligible" means):
  - If one exists: do NOT increment availableCopies. Instead, auto-create a new Reservation (status
    RESERVED) for that patron, and update their waitlist entry to status NOTIFIED with
    claimDeadline = now + 48 hours
  - If none exists: increment book's availableCopies by 1, same as before
- Return response with late fee details if applicable

---

#### GET /api/reservations/history

Retrieve complete borrowing history for the currently authenticated user with pagination.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Query Parameters:**

- `page` (integer, default: 0) - Zero-based page number
- `size` (integer, default: 20) - Number of history records per page

**Success Response (200 OK):**

```json
{
  "content": [
    {
      "reservationId": "uuid-res-100",
      "bookTitle": "Clean Code",
      "bookAuthor": "Robert C. Martin",
      "reservedAt": "2025-08-15T10:00:00Z",
      "checkedOutAt": "2025-08-16T14:00:00Z",
      "returnedAt": "2025-08-30T09:00:00Z",
      "dueDate": "2025-08-30T14:00:00Z",
      "status": "RETURNED",
      "wasLate": false
    },
    {
      "reservationId": "uuid-res-99",
      "bookTitle": "Refactoring",
      "bookAuthor": "Martin Fowler",
      "reservedAt": "2025-07-20T10:00:00Z",
      "checkedOutAt": "2025-07-21T14:00:00Z",
      "returnedAt": "2025-08-10T09:00:00Z",
      "dueDate": "2025-08-04T14:00:00Z",
      "status": "RETURNED",
      "wasLate": true
    }
  ],
  "page": 0,
  "size": 20,
  "totalElements": 45,
  "totalPages": 3,
  "last": false
}
```

**Business Logic:**

- Extract userId from JWT token
- Fetch all reservations where userId matches (all statuses including RETURNED, CANCELLED)
- Sort by returnedAt or reservedAt descending (most recent first)
- For each record, calculate wasLate flag: wasLate = (returnedAt > dueDate)
- Join with book data to include bookTitle and bookAuthor
- Return paginated history with metadata (page, size, totalElements, totalPages, last)

---

#### POST /api/reservations/waitlist

Join the waitlist for a book that currently has no available copies.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Request Body:**

```json
{
  "bookId": "uuid-book-1"
}
```

**Success Response (201 Created):**

```json
{
  "waitlistId": "uuid-wait-1",
  "bookId": "uuid-book-1",
  "bookTitle": "Clean Code",
  "status": "WAITING",
  "joinedAt": "2025-10-10T10:00:00Z",
  "position": 3
}
```

**Error Response (400 Bad Request - Book Available):**

```json
{
  "error": "BOOK_AVAILABLE",
  "message": "This book currently has available copies - reserve it directly instead of joining the waitlist",
  "timestamp": "2025-10-10T10:00:00Z"
}
```

**Error Response (400 Bad Request - Already Waitlisted):**

```json
{
  "error": "ALREADY_WAITLISTED",
  "message": "You are already on the waitlist for this book",
  "timestamp": "2025-10-10T10:00:00Z"
}
```

**Business Logic:**

- Extract userId from JWT token
- Verify the book's availableCopies = 0 (via Catalog Service); return BOOK_AVAILABLE if not
- Verify the user doesn't already have a WAITING entry for this book; return ALREADY_WAITLISTED if they do
- Create waitlist entry with status = WAITING and joinedAt = current timestamp
- Compute position as the entry's 1-indexed rank among WAITING entries for this book, ordered by joinedAt
- Return the created entry with computed position

---

#### GET /api/reservations/waitlist

Retrieve the currently authenticated user's active waitlist entries.

**Access:** Requires authentication (Bearer token)

**Headers:**

```
Authorization: Bearer {token}
```

**Success Response (200 OK):**

```json
{
  "entries": [
    {
      "waitlistId": "uuid-wait-1",
      "bookId": "uuid-book-1",
      "bookTitle": "Clean Code",
      "bookAuthor": "Robert C. Martin",
      "status": "WAITING",
      "joinedAt": "2025-10-10T10:00:00Z",
      "position": 3
    },
    {
      "waitlistId": "uuid-wait-2",
      "bookId": "uuid-book-5",
      "bookTitle": "Refactoring",
      "bookAuthor": "Martin Fowler",
      "status": "NOTIFIED",
      "joinedAt": "2025-09-28T10:00:00Z",
      "notifiedAt": "2025-10-09T09:00:00Z",
      "claimDeadline": "2025-10-11T09:00:00Z"
    }
  ]
}
```

**Business Logic:**

- Extract userId from JWT token
- Fetch all WAITING and NOTIFIED entries belonging to this user
- For WAITING entries, compute current queue position
- For NOTIFIED entries, include the claimDeadline instead of a position
- Join with book data to include bookTitle and bookAuthor

---

#### DELETE /api/reservations/waitlist/{waitlistId}

Leave a waitlist voluntarily.

**Access:** Requires authentication (Bearer token) - can only cancel your own entry

**Headers:**

```
Authorization: Bearer {token}
```

**Path Parameters:**

- `waitlistId` (UUID, required) - Unique identifier of the waitlist entry

**Success Response (200 OK):**

```json
{
  "waitlistId": "uuid-wait-1",
  "status": "CANCELLED",
  "message": "You have been removed from the waitlist"
}
```

**Error Response (404 Not Found):**

```json
{
  "error": "NOT_FOUND",
  "message": "Waitlist entry not found",
  "timestamp": "2025-10-10T10:00:00Z"
}
```

**Business Logic:**

- Extract userId from JWT token
- Fetch waitlist entry by waitlistId; return 404 if it doesn't exist or doesn't belong to this user
- Set status = CANCELLED
- If the entry's prior status was NOTIFIED (it was actively holding a claim), immediately run the same
  cascade logic the expiry background job uses: offer the copy to the next eligible WAITING entry for
  that book, or release it back to general availability if none exists

---

## Common Error Responses

All endpoints may return the following error responses:

**401 Unauthorized (Missing/Invalid Token):**

```json
{
  "error": "UNAUTHORIZED",
  "message": "Authentication required",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**403 Forbidden (Insufficient Permissions):**

```json
{
  "error": "FORBIDDEN",
  "message": "You do not have permission to access this resource",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

**500 Internal Server Error:**

```json
{
  "error": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred",
  "timestamp": "2025-09-29T10:30:00Z"
}
```

## Notes

- All timestamps are in ISO 8601 format (UTC)
- All UUIDs are RFC 4122 compliant
- Bearer tokens must be included in Authorization header for protected endpoints
- Token expiration is 86400 seconds (24 hours)
- Maximum active reservations per user: 5
- Reservation expiry period: 7 days from reservation
- Checkout period: 14 days from checkout
- Late fee rate: $1.00 per day
- Waitlist claim window: 48 hours from notification
- Waitlist eligibility (the 5-reservation limit) is checked at claim time, not join time - a patron
  under the limit when they join can still be skipped later if they've since hit the limit
- Roles: PATRON (regular users), LIBRARIAN (can checkout/return books)