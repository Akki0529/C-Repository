# Milestone 3: Catalog Service

**Goal:** Build book catalog browsing and search system

**Related User Stories:** US-004 (Browse Book Catalog), US-005 (Search and Filter Books), US-006 (View Book Details)

---

## Business Requirements

### Book Browsing (US-004)
- Library patrons must be able to browse the complete book catalog
- Results must be paginated for performance
  - Default: 20 books per page, starting at page 0
- Results must be sortable by:
  - Title
  - Author
  - Publication year
- Sort direction can be ascending or descending (default: ascending)
- Each book displays: bookId, isbn, title, author, genre, publicationYear, description, totalCopies, availableCopies, status
- Book status is determined by availability:
  - Available: availableCopies > 0
  - CheckedOut: availableCopies = 0
- No authentication required (public access)

### Search and Filtering (US-005)
- Patrons must be able to search books by title and/or author
- Patrons can filter by:
  - Genre (exact match)
  - ISBN (exact match)
  - Availability (show only books with copies available)
- Multiple filters can be combined simultaneously
- Search results maintain pagination and sorting capabilities
- Empty searches return empty results (not an error)
- No authentication required (public access)

### Book Details (US-006)
- Patrons must be able to view complete information about a specific book
- Details include all metadata: isbn, title, author, genre, publicationYear, description, publisher, pageCount, language
- Details show availability: totalCopies, availableCopies, status
- Details include audit information: createdAt, updatedAt
- Returns 404 error if book not found
- No authentication required (public access)

### Inventory Management
- Book status is calculated dynamically (not stored)
- availableCopies automatically updates when:
  - Reservation created (decrements by 1)
  - Book returned, AND no one is waiting on that book's waitlist (increments by 1)
- totalCopies represents physical inventory

> **Note:** As of Milestone 4, a returned book with an active waitlist does *not* increment
> availableCopies - the copy is handed directly to the next waitlisted patron instead. This logic lives
> entirely in Reservation Service; Catalog Service's role is unchanged (it still just exposes the
> availability-update endpoint and doesn't need to know *why* an update request was or wasn't made).

---

## General Technical Requirements

**Performance Requirements:**
- Search and filter operations must complete within reasonable time for catalogs up to 1000+ books
- Efficient querying and indexing strategy required

**Data Requirements:**
- Support pagination for large result sets
- Return pagination metadata (page number, size, total elements, total pages, last page flag)
- Support dynamic filtering and sorting combinations

**Public Access:**
- All catalog endpoints are publicly accessible (no authentication required)

---

## Deliverables

### 1. Paginated Book Listing
Implement endpoint that:
- Returns books in paginated format
- Accepts pagination parameters (page number, page size)
- Accepts sort parameters (field, direction)
- Applies default pagination and sorting when not specified
- Returns metadata about pagination state

### 2. Search and Filter Functionality
Implement search/filter capabilities that:
- Searches across title and author fields
- Filters by genre
- Filters by ISBN
- Filters by availability (books with available copies)
- Supports combining multiple filters
- Maintains pagination for filtered results
- Performs efficiently on large datasets

### 3. Book Details Retrieval
Implement endpoint that:
- Retrieves complete book information by unique identifier
- Returns all book metadata fields
- Calculates and includes current availability status
- Returns appropriate error for non-existent books

### 4. Status Calculation
Implement logic that:
- Dynamically determines book status based on availableCopies
- Does not store status as a database field
- Consistently calculates status across all endpoints

---

## API Endpoints to Implement

Based on `api-contracts.md`, implement these endpoints:

### GET /api/catalog/books
- **Access:** Public (no authentication)
- **Query Parameters:**
  - page (integer, default: 0)
  - size (integer, default: 20)
  - sortBy (string, default: "title") - options: title, author, publicationYear
  - sortOrder (string, default: "asc") - options: asc, desc
  - query (string, optional) - search term for title/author
  - genre (string, optional) - filter by genre
  - isbn (string, optional) - filter by ISBN
  - availableOnly (boolean, default: false)
- **Success (200):** Paginated list with content array and metadata
- **Response includes:** page, size, totalElements, totalPages, last

### GET /api/catalog/books/{bookId}
- **Access:** Public (no authentication)
- **Path Parameter:** bookId (Guid)
- **Success (200):** Complete book details including all metadata
- **Error (404):** Book not found

---

## Acceptance Criteria

- [ ] GET /api/catalog/books returns paginated results with defaults (page=0, size=20, sortBy=title, sortOrder=asc)
- [ ] Pagination metadata included: content, page, size, totalElements, totalPages, last
- [ ] Query parameter performs search on title and author fields
- [ ] Genre filter returns only books matching exact genre
- [ ] ISBN filter returns only book matching exact ISBN
- [ ] availableOnly filter returns only books with availableCopies > 0
- [ ] Multiple filters work together (query + genre + availableOnly)
- [ ] Sorting works for title, author, and publicationYear
- [ ] Sort order (asc/desc) works correctly
- [ ] GET /api/catalog/books/{bookId} returns complete book details
- [ ] Invalid bookId returns 404 with appropriate error response
- [ ] Book status correctly calculated (Available vs CheckedOut)
- [ ] Empty search results return empty content array (not error)
- [ ] Search performs efficiently (< 1 second for 1000+ books)

---

## Suggested Approach

1. Design efficient database queries for filtering and sorting
2. Implement pagination mechanism
3. Create book listing endpoint with pagination
4. Add search functionality across title and author
5. Implement individual filter capabilities
6. Combine filters to work together
7. Add sorting functionality
8. Implement book details endpoint
9. Add status calculation logic
10. Optimize query performance with appropriate indexing

**Note:** You have flexibility in how you implement search and filtering logic, structure your queries, and optimize performance. Consider using appropriate Entity Framework Core features and query techniques to meet performance requirements.

---

## Resources

- Refer to `user-stories.md` for US-004, US-005, US-006 details
- Refer to `api-contracts.md` for exact request/response formats
- Entity Framework Core documentation for pagination and query techniques