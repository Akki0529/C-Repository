using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

public static class DataSeeder
{
    // Seeds a realistic catalog so browse, search, and filter endpoints can be tested
    // immediately after startup without manual data entry.
    public static async Task SeedAsync(CatalogServiceContext context)
    {
        if (await context.Books.AnyAsync())
            return;

        var books = new List<Book>
        {
            // ── Technology ──────────────────────────────────────────────────────────
            new()
            {
                BookId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                Isbn = "978-0-13-468599-1",
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Genre = "Technology",
                PublicationYear = 2008,
                Description = "A handbook of agile software craftsmanship.",
                Publisher = "Prentice Hall",
                PageCount = 464,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 2   // 3 currently checked out
            },
            new()
            {
                BookId = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"),
                Isbn = "978-0-13-475759-9",
                Title = "Refactoring",
                Author = "Martin Fowler",
                Genre = "Technology",
                PublicationYear = 2018,
                Description = "Improving the design of existing code.",
                Publisher = "Addison-Wesley",
                PageCount = 448,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 0   // fully checked out; triggers waitlist scenario in tests
            },
            new()
            {
                BookId = Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"),
                Isbn = "978-0-13-595705-9",
                Title = "The Pragmatic Programmer",
                Author = "David Thomas & Andrew Hunt",
                Genre = "Technology",
                PublicationYear = 2019,
                Description = "Your journey to mastery, 20th Anniversary Edition.",
                Publisher = "Addison-Wesley",
                PageCount = 352,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            },
            new()
            {
                BookId = Guid.Parse("f6a7b8c9-d0e1-2345-fabc-456789012345"),
                Isbn = "978-0-20-163361-0",
                Title = "Design Patterns",
                Author = "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides",
                Genre = "Technology",
                PublicationYear = 1994,
                Description = "Elements of Reusable Object-Oriented Software.",
                Publisher = "Addison-Wesley",
                PageCount = 395,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 1
            },
            new()
            {
                BookId = Guid.Parse("a7b8c9d0-e1f2-3456-abcd-567890123456"),
                Isbn = "978-0-32-112521-7",
                Title = "Domain-Driven Design",
                Author = "Eric Evans",
                Genre = "Technology",
                PublicationYear = 2003,
                Description = "Tackling complexity in the heart of software.",
                Publisher = "Addison-Wesley",
                PageCount = 560,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 3
            },

            // ── Fiction ─────────────────────────────────────────────────────────────
            new()
            {
                BookId = Guid.Parse("b8c9d0e1-f2a3-4567-bcde-678901234567"),
                Isbn = "978-0-74-327356-5",
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Genre = "Fiction",
                PublicationYear = 1925,
                Description = "A portrait of the Jazz Age in all of its decadence and excess.",
                Publisher = "Scribner",
                PageCount = 180,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 5
            },
            new()
            {
                BookId = Guid.Parse("c9d0e1f2-a3b4-5678-cdef-789012345678"),
                Isbn = "978-0-44-631078-9",
                Title = "To Kill a Mockingbird",
                Author = "Harper Lee",
                Genre = "Fiction",
                PublicationYear = 1960,
                Description = "A novel about the serious issues of rape and racial inequality.",
                Publisher = "J. B. Lippincott & Co.",
                PageCount = 281,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 2
            },

            // ── Dystopian ───────────────────────────────────────────────────────────
            new()
            {
                BookId = Guid.Parse("d0e1f2a3-b4c5-6789-defa-890123456789"),
                Isbn = "978-0-45-228423-4",
                Title = "1984",
                Author = "George Orwell",
                Genre = "Dystopian",
                PublicationYear = 1949,
                Description = "A cautionary tale about totalitarianism and surveillance.",
                Publisher = "Secker & Warburg",
                PageCount = 328,
                Language = "English",
                TotalCopies = 6,
                AvailableCopies = 6
            },

            // ── Non-Fiction ─────────────────────────────────────────────────────────
            new()
            {
                BookId = Guid.Parse("e1f2a3b4-c5d6-7890-efab-901234567890"),
                Isbn = "978-0-06-231609-7",
                Title = "Sapiens",
                Author = "Yuval Noah Harari",
                Genre = "History",
                PublicationYear = 2011,
                Description = "A brief history of humankind.",
                Publisher = "Harper",
                PageCount = 443,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 1
            },

            // ── Philosophy ──────────────────────────────────────────────────────────
            new()
            {
                BookId = Guid.Parse("f2a3b4c5-d6e7-8901-fabc-012345678901"),
                Isbn = "978-1-59-030762-4",
                Title = "The Art of War",
                Author = "Sun Tzu",
                Genre = "Philosophy",
                PublicationYear = -500,  // circa 500 BC; negative year supported by .NET DateTime
                Description = "An ancient Chinese military treatise.",
                Publisher = "Shambhala",
                PageCount = 273,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 2
            }
        };

        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
    }
}
