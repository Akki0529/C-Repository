## User Stories

### Epic 1: User Management & Authentication

**US-001: User Registration**
- **As a** library patron
- **I want to** create an account with my email and personal information
- **So that** I can access library services and reserve books
- **Acceptance Criteria**:
  - User provides email, password, firstName, lastName, and phoneNumber
  - Email must be unique and validated
  - Password must meet security requirements (min 8 chars, uppercase, lowercase, number, special char)
  - Phone number must be in valid format
  - Account is created with PATRON role by default
  - membershipStatus is set to ACTIVE by default
  - Returns 201 with userId, profile info, and success message
  - Returns 400 if email already exists

**US-002: User Authentication**
- **As a** registered user
- **I want to** log in securely with my credentials
- **So that** I can access my account and library services
- **Acceptance Criteria**:
  - User logs in with email and password
  - JWT token is issued upon successful authentication (Bearer type)
  - Token expires after 24 hours (86400 seconds)
  - Returns 200 with accessToken, tokenType, expiresIn, and user object
  - Returns 401 for invalid credentials
  - Failed login attempts are logged for security

**US-003: View Profile**
- **As a** logged-in user
- **I want to** view my profile information
- **So that** I can see my account details and library activity
- **Acceptance Criteria**:
  - User can view complete profile: userId, email, firstName, lastName, phoneNumber, role, membershipStatus, memberSince
  - Profile shows activeReservations count (RESERVED or CHECKED_OUT status)
  - Profile shows borrowingHistory count (total completed reservations)
  - Requires valid JWT token in Authorization header
  - Returns 200 with complete profile data
  - Returns 401 if not authenticated

### Epic 2: Catalog Management

**US-004: Browse Book Catalog**
- **As a** library patron
- **I want to** browse the complete book catalog with pagination and sorting
- **So that** I can discover available books
- **Acceptance Criteria**:
  - Catalog displays books with: bookId, isbn, title, author, genre, publicationYear, description, totalCopies, availableCopies, status
  - Results are paginated (default: 20 books per page, page 0)
  - Can sort by: title, author, publicationYear
  - Can sort order: asc or desc (default: asc)
  - Status shows: AVAILABLE (availableCopies > 0) or CHECKED_OUT (availableCopies = 0)
  - No authentication required (public endpoint)
  - Returns pagination metadata: page, size, totalElements, totalPages, last

**US-005: Search and Filter Books**
- **As a** library patron
- **I want to** search books by title/author and filter by genre, ISBN, or availability
- **So that** I can quickly find specific books
- **Acceptance Criteria**:
  - Search accepts query parameter for full-text search on title and author
  - Can filter by genre (exact match)
  - Can filter by ISBN (exact match)
  - Can filter by availableOnly (boolean, shows only books with availableCopies > 0)
  - Multiple filters can be combined simultaneously
  - Results are paginated and sortable
  - No authentication required (public endpoint)
  - Empty results return empty content array

**US-006: View Book Details**
- **As a** library patron
- **I want to** view complete information about a specific book
- **So that** I can decide whether to reserve it
- **Acceptance Criteria**:
  - Displays complete book information: isbn, title, author, genre, publicationYear, description, publisher, pageCount, language
  - Shows availability: totalCopies, availableCopies, status
  - Shows audit information: createdAt, updatedAt
  - No authentication required (public endpoint)
  - Returns 200 with complete book details
  - Returns 404 if book not found

### Epic 3: Reservation & Borrowing

**US-007: Reserve Available Book**
- **As a** library patron
- **I want to** reserve an available book
- **So that** I can pick it up from the library
- **Acceptance Criteria**:
  - Patron can reserve books with availableCopies > 0
  - Reservation is valid for 7 days for pickup (expiresAt = reservedAt + 7 days)
  - System decreases availableCopies by 1
  - Status is set to RESERVED
  - Patron can have maximum 5 active reservations (RESERVED or CHECKED_OUT)
  - Returns 201 with reservationId, bookId, userId, bookTitle, status, reservedAt, expiresAt, message
  - Returns 400 if limit exceeded (RESERVATION_LIMIT_EXCEEDED)
  - Returns 400 if book unavailable (BOOK_UNAVAILABLE)
  - Requires authentication

**US-008: View Active Reservations**
- **As a** library patron
- **I want to** see all my active reservations
- **So that** I can track my borrowed books and pickup deadlines
- **Acceptance Criteria**:
  - Shows all reservations with status RESERVED or CHECKED_OUT
  - For RESERVED: displays daysUntilExpiry (expiresAt - current date)
  - For CHECKED_OUT: displays daysUntilDue (dueDate - current date)
  - Includes book details: bookId, bookTitle, bookAuthor
  - Shows totalActive count
  - Returns 200 with reservations array
  - Requires authentication

**US-009: Checkout Book**
- **As a** librarian
- **I want to** process book checkouts at the library desk
- **So that** patrons can take books home
- **Acceptance Criteria**:
  - Only LIBRARIAN role can checkout books
  - Can only checkout reservations with RESERVED status
  - Checkout period is 14 days from checkout date (dueDate = checkedOutAt + 14 days)
  - Status changes from RESERVED to CHECKED_OUT
  - Can include optional notes about book condition
  - Returns 200 with reservationId, status, checkedOutAt, dueDate, message
  - Returns 403 if not LIBRARIAN
  - Returns 400 if status is not RESERVED

**US-010: Return Book**
- **As a** librarian
- **I want to** process book returns at the library desk
- **So that** books become available for other patrons
- **Acceptance Criteria**:
  - Only LIBRARIAN role can process returns
  - Can only return reservations with CHECKED_OUT status
  - System calculates lateDays if returnedAt > dueDate
  - Late fee calculated at $1.00 per day
  - Book condition must be recorded (GOOD, FAIR, POOR, DAMAGED)
  - Can include optional notes
  - Status changes from CHECKED_OUT to RETURNED
  - availableCopies increases by 1
  - Returns 200 with reservationId, returnedAt, lateDays, lateFee, message
  - Returns 403 if not LIBRARIAN
  - Returns 400 if status is not CHECKED_OUT

**US-011: View Borrowing History**
- **As a** library patron
- **I want to** see my complete borrowing history
- **So that** I can track books I've borrowed
- **Acceptance Criteria**:
  - History shows all past reservations (all statuses: RESERVED, CHECKED_OUT, RETURNED, CANCELLED)
  - Includes: reservationId, bookTitle, bookAuthor, reservedAt, checkedOutAt, returnedAt, dueDate, status
  - Shows wasLate flag (true if returnedAt > dueDate)
  - Results are paginated (default: page 0, size 20)
  - Sorted by most recent first (returnedAt or reservedAt descending)
  - Returns pagination metadata: page, size, totalElements, totalPages, last
  - Returns 200 with paginated history
  - Requires authentication

**US-012: Join a Book's Waitlist**
- **As a** library patron
- **I want to** join the waitlist for a book that has no available copies
- **So that** I'm notified and get first claim when a copy is returned
- **Acceptance Criteria**:
  - Patron can only join a waitlist when availableCopies = 0 (otherwise, they should reserve directly)
  - Patron can only have one active (Waiting) waitlist entry per book at a time
  - Waitlist entry is created with status WAITING and joinedAt timestamp
  - Returns 201 with waitlistId, bookId, bookTitle, status, joinedAt, position (queue position, 1-indexed)
  - Returns 400 if the book actually has available copies (BOOK_AVAILABLE - should reserve instead)
  - Returns 400 if the user already has an active waitlist entry for this book (ALREADY_WAITLISTED)
  - Requires authentication

**US-013: View My Waitlist Entries**
- **As a** library patron
- **I want to** see every waitlist I've joined and my position in each queue
- **So that** I know roughly how long I'll wait for each book
- **Acceptance Criteria**:
  - Shows all of the user's waitlist entries with status WAITING or NOTIFIED
  - For WAITING entries: shows current queue position (computed, not stored)
  - For NOTIFIED entries: shows the claim deadline (48 hours from notification)
  - Includes book details: bookId, bookTitle, bookAuthor
  - Returns 200 with waitlist entries array
  - Requires authentication

**US-014: Leave a Waitlist**
- **As a** library patron
- **I want to** remove myself from a book's waitlist
- **So that** I'm not offered a book I no longer want, and others move up the queue
- **Acceptance Criteria**:
  - Patron can cancel their own WAITING or NOTIFIED entry
  - Status changes to CANCELLED
  - If the cancelled entry was NOTIFIED (i.e., currently holding a claim window), the held copy is
    immediately offered to the next WAITING entry in that book's queue, same as a normal expiry
  - Returns 200 with confirmation message
  - Returns 404 if the waitlist entry doesn't belong to the requesting user or doesn't exist
  - Requires authentication

---

## Summary

**Total User Stories: 14**

- **Epic 1 (Authentication):** 3 user stories
- **Epic 2 (Catalog):** 3 user stories
- **Epic 3 (Reservations & Waitlist):** 8 user stories

**Endpoints Coverage (13 Total):**
1. POST /api/auth/register → US-001
2. POST /api/auth/login → US-002
3. GET /api/users/profile → US-003
4. GET /api/catalog/books → US-004, US-005
5. GET /api/catalog/books/{bookId} → US-006
6. POST /api/reservations → US-007
7. GET /api/reservations → US-008
8. POST /api/reservations/{reservationId}/checkout → US-009
9. POST /api/reservations/{reservationId}/return → US-010
10. GET /api/reservations/history → US-011
11. POST /api/reservations/waitlist → US-012
12. GET /api/reservations/waitlist → US-013
13. DELETE /api/reservations/waitlist/{waitlistId} → US-014

**System behavior (no direct endpoint):** A background job in Reservation Service periodically expires
NOTIFIED waitlist entries whose claim deadline has passed, then cascades the held copy to the next WAITING
entry in that book's queue (or releases it back to general availability if the queue is empty). See
Milestone 4 for details.