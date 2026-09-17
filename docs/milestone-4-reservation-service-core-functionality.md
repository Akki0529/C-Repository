# Milestone 4: Reservation Service - Core Functionality

**Goal:** Implement reservation lifecycle management

**Related User Stories:** US-007 (Reserve Available Book), US-008 (View Active Reservations), US-009
(Checkout Book), US-010 (Return Book), US-011 (View Borrowing History), US-012 (Join Waitlist), US-013
(View My Waitlist), US-014 (Leave Waitlist)

---

## Business Requirements

### Reservation Creation (US-007)
- Patrons can reserve books that have available copies
- Users are limited to 5 active reservations (Reserved or CheckedOut status)
- Reservations expire after 7 days if not picked up
- When a book is reserved:
  - Available copies count decreases by 1
  - Reservation status is set to Reserved
  - Expiration date is set to 7 days from reservation
- Error conditions:
  - Attempting to reserve when at 5 active reservations
  - Attempting to reserve a book with no available copies

### Active Reservations View (US-008)
- Patrons can view all their active reservations (Reserved or CheckedOut)
- For Reserved books: show days until pickup deadline expires
- For CheckedOut books: show days until due date
- Display book information (title and author) with each reservation
- Show total count of active reservations

### Checkout Process (US-009)
- Only Librarian role can process checkouts
- Can only checkout reservations with Reserved status
- Checkout period is 14 days from checkout date
- Optional notes can be recorded about book condition at checkout
- Returns formatted message with due date

### Return Processing (US-010)
- Only Librarian role can process returns
- Can only return reservations with CheckedOut status
- Book condition must be recorded (Good, Fair, Poor, Damaged)
- Late fees are calculated if returned after due date:
  - Rate: $1.00 per day late
- When book is returned:
  - Reservation status is set to Returned
  - **If the book has an active waitlist:** skip the availableCopies increment entirely. Instead, find
    the longest-waiting eligible entry (see Waitlist Claim Eligibility below), auto-create a Reserved
    reservation for that patron, and mark their waitlist entry Notified with a 48-hour ClaimDeadline
  - **If the book has no waitlist, or everyone waiting is over their reservation limit:** available
    copies count increases by 1, same as before
- Optional notes can be recorded

### Waitlist Claim Eligibility
- A waitlist entry is only eligible to claim a returned copy if that patron currently has fewer than 5
  active reservations (Reserved or CheckedOut)
- This is checked at the moment their turn comes up, not when they originally joined the waitlist - a
  patron's eligibility can change between joining and their turn arriving
- If the longest-waiting entry is ineligible, expire that entry (status = Expired) and check the next
  entry in the queue; repeat until an eligible patron is found or the queue is exhausted
- If the queue is exhausted with no eligible patron, release the copy back to general availability
  (increment availableCopies), same as the no-waitlist case

### Join Waitlist (US-012)
- Patrons can join the waitlist for a book with availableCopies = 0
- Attempting to join when the book actually has available copies returns an error (BOOK_AVAILABLE) -
  the patron should reserve directly instead
- A patron can only have one active (Waiting) entry per book at a time
- Queue position is computed from JoinedAt order, not stored as a field

### View My Waitlist (US-013)
- Patrons can view their own Waiting and Notified waitlist entries
- Waiting entries show computed queue position
- Notified entries show the claim deadline

### Leave Waitlist (US-014)
- Patrons can cancel their own Waiting or Notified entry at any time
- Cancelling a Notified entry (one currently holding a claim) immediately cascades the held copy to the
  next eligible entry in that book's queue - the same logic as a natural expiry, just triggered
  immediately instead of waiting for the 48-hour deadline

### Waitlist Expiry Background Job
- A background process (ASP.NET Core `BackgroundService`/`IHostedService`) runs on an interval (e.g.
  hourly) within Reservation Service
- Finds all Notified waitlist entries whose ClaimDeadline has passed
- For each: marks the entry Expired, then applies the same cascade logic described above - offer the
  copy to the next eligible Waiting entry, or release it back to general availability if none exists
- This is the first genuinely asynchronous, non-request-driven process in the system - unlike every other
  piece of business logic so far, it isn't triggered by an incoming HTTP request at all

### Borrowing History (US-011)
- Patrons can view their complete borrowing history
- History includes all reservation statuses (Reserved, CheckedOut, Returned, Cancelled)
- Results are paginated (default: page 0, size 20)
- Sorted by most recent first
- Each record indicates if book was returned late
- Includes book information (title and author)

---

## General Technical Requirements

**Business Rules:**
- Maximum active reservations per user: 5
- Reservation expiry period: 7 days from reservation date
- Checkout period: 14 days from checkout date
- Late fee rate: $1.00 per day
- Book condition options: Good, Fair, Poor, Damaged
- Waitlist claim window: 48 hours from notification
- Waitlist eligibility (the 5-reservation limit) is enforced strictly, even for waitlist claims - a patron
  over the limit is skipped, not exempted

**Data Consistency:**
- Reservation operations must maintain data integrity
- Available copies count must stay synchronized with reservations
- Date/time calculations must be accurate and consistent

**Authorization:**
- Checkout and return operations restricted to Librarian role
- Users can only view their own reservations and history

---

## Deliverables

### 1. Reservation Creation
Implement endpoint that:
- Validates user has fewer than 5 active reservations
- Validates book has available copies
- Creates reservation with Reserved status
- Sets reservation and expiration timestamps
- Updates book's available copies count
- Returns reservation details with success message
- Handles error conditions appropriately

### 2. Active Reservations View
Implement endpoint that:
- Retrieves user's active reservations (Reserved and CheckedOut)
- Calculates time-based fields (days until expiry/due)
- Includes book information
- Returns total active count

### 3. Checkout Process
Implement endpoint that:
- Validates user has Librarian role
- Validates reservation is in Reserved status
- Updates reservation to CheckedOut status
- Records checkout timestamp
- Calculates and sets due date (14 days)
- Stores optional notes
- Returns formatted response with due date

### 4. Return Processing
Implement endpoint that:
- Validates user has Librarian role
- Validates reservation is in CheckedOut status
- Updates reservation to Returned status
- Records return timestamp and book condition
- Calculates late days and fees (if applicable)
- Checks for an eligible waitlist entry on the book before deciding whether to increment availableCopies
  or auto-create a claim reservation instead (see Waitlist Claim Eligibility above)
- Stores optional notes
- Returns response with late fee details if applicable

### 5. Borrowing History
Implement endpoint that:
- Retrieves user's complete borrowing history
- Includes all reservation statuses
- Paginates results
- Sorts by most recent first
- Calculates late return flag for each record
- Includes book information
- Returns pagination metadata

### 6. Waitlist Management
Implement three endpoints that:
- Allow a patron to join a book's waitlist (only when availableCopies = 0, only one active entry per
  book per patron)
- Allow a patron to view their own waitlist entries, with computed queue position for Waiting entries
  and claim deadline for Notified entries
- Allow a patron to leave a waitlist voluntarily, cascading the held copy immediately if the cancelled
  entry was Notified

### 7. Waitlist Expiry Background Job
Implement a `BackgroundService` (or `IHostedService`) that:
- Runs on a recurring interval within Reservation Service (does not need its own separate deployment -
  it runs in-process alongside the web application)
- Finds Notified entries past their ClaimDeadline
- Expires them and cascades the copy to the next eligible entry, or releases it back to general
  availability if the queue is empty or exhausted
- Logs its activity (how many entries it processed, what it did with each) so its behavior is observable

---

## API Endpoints to Implement

Based on `api-contracts.md`, implement these endpoints:

### POST /api/reservations
- **Access:** Requires authentication (Patron or Librarian)
- **Request:** bookId
- **Success (201):** reservationId, bookId, userId, bookTitle, status, reservedAt, expiresAt, message
- **Error (400):** RESERVATION_LIMIT_EXCEEDED or BOOK_UNAVAILABLE

### GET /api/reservations
- **Access:** Requires authentication (Patron or Librarian)
- **Success (200):** Array of active reservations with daysUntilExpiry/daysUntilDue, totalActive count

### POST /api/reservations/{reservationId}/checkout
- **Access:** Requires Librarian role
- **Request:** notes (optional)
- **Success (200):** reservationId, status, checkedOutAt, dueDate, message
- **Error (403):** FORBIDDEN (non-librarian)
- **Error (400):** INVALID_STATUS (not Reserved)

### POST /api/reservations/{reservationId}/return
- **Access:** Requires Librarian role
- **Request:** condition (Good, Fair, Poor, Damaged), notes (optional)
- **Success (200):** reservationId, returnedAt, lateDays, lateFee, message
- **Error (403):** FORBIDDEN (non-librarian)
- **Error (400):** INVALID_STATUS (not CheckedOut)

### GET /api/reservations/history
- **Access:** Requires authentication (Patron or Librarian)
- **Query Parameters:** page (default: 0), size (default: 20)
- **Success (200):** Paginated history with wasLate flag, includes pagination metadata

### POST /api/reservations/waitlist
- **Access:** Requires authentication (Patron or Librarian)
- **Request:** bookId
- **Success (201):** waitlistId, bookId, bookTitle, status, joinedAt, position
- **Error (400):** BOOK_AVAILABLE (book isn't out of copies) or ALREADY_WAITLISTED

### GET /api/reservations/waitlist
- **Access:** Requires authentication (Patron or Librarian)
- **Success (200):** Array of the user's Waiting/Notified entries, with position or claimDeadline

### DELETE /api/reservations/waitlist/{waitlistId}
- **Access:** Requires authentication (Patron or Librarian) - can only cancel your own entry
- **Success (200):** Confirmation message
- **Error (404):** Entry not found, or doesn't belong to the requesting user

---

## Acceptance Criteria

- [ ] Patron can reserve books when fewer than 5 active reservations
- [ ] Reservation creation returns 400 when limit of 5 reached
- [ ] Reservation creation returns 400 when book has no available copies
- [ ] Expiration date set to 7 days from reservation date
- [ ] Active reservations show days until expiry (Reserved status)
- [ ] Active reservations show days until due (CheckedOut status)
- [ ] Checkout sets due date to 14 days from checkout date
- [ ] Only Librarian can access checkout endpoint (403 for Patron)
- [ ] Only Librarian can access return endpoint (403 for Patron)
- [ ] Late fees calculated at $1.00 per day
- [ ] Available copies decrements on reservation creation
- [ ] Available copies increments on book return
- [ ] Borrowing history shows all reservations with pagination
- [ ] wasLate flag correctly calculated in history
- [ ] Cannot checkout reservation that is not Reserved (400 error)
- [ ] Cannot return reservation that is not CheckedOut (400 error)
- [ ] Returning a book with no waitlist increments availableCopies (existing behavior unchanged)
- [ ] Returning a book with an eligible waitlisted patron does NOT increment availableCopies - instead
  auto-creates a Reserved reservation for that patron and sets their waitlist entry to Notified
- [ ] Returning a book skips waitlist entries belonging to patrons already at their 5-reservation limit
- [ ] Patron can join a waitlist only when availableCopies = 0
- [ ] Patron cannot join the same book's waitlist twice while already Waiting
- [ ] Patron can view their own waitlist entries with computed position
- [ ] Patron can leave their own waitlist entry
- [ ] Leaving a Notified entry immediately cascades the copy to the next eligible entry
- [ ] Background job expires Notified entries past their 48-hour ClaimDeadline
- [ ] Background job cascades expired claims to the next eligible waitlist entry, or releases the copy
  back to general availability if none exists

---

## Suggested Approach

1. Implement reservation creation with validation logic
2. Add available copies update mechanism
3. Implement active reservations retrieval with calculations
4. Create checkout endpoint with role validation
5. Create return endpoint with late fee calculation
6. Implement borrowing history with pagination
7. Add date/time calculation utilities
8. Ensure atomic operations for data consistency
9. Test all business rules and edge cases

**Note:** You have flexibility in how you structure your services, implement date calculations, handle transactions, and organize your business logic. Focus on meeting the business requirements and maintaining data consistency.

---

## Resources

- Refer to `user-stories.md` for US-007, US-008, US-009, US-010, US-011, US-012, US-013, US-014 details
- Refer to `api-contracts.md` for exact request/response formats
- DateTime and TimeSpan documentation for date/time calculations
- `BackgroundService` documentation for implementing the waitlist expiry job