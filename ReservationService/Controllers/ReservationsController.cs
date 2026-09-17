using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Dtos;
using ReservationService.Models;
using ReservationService.Services;

namespace ReservationService.Controllers;

// Route ordering note (full rationale in Program.cs): literal segments like "history"
// and "waitlist" always take precedence over the "{reservationId}" parameter segment,
// so these routes don't need to be declared in any particular order in this file.
[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly ReservationServiceContext _context;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IWaitlistCascadeService _cascadeService;

    public ReservationsController(
        ReservationServiceContext context,
        IUserServiceClient userServiceClient,
        ICatalogServiceClient catalogServiceClient,
        IWaitlistCascadeService cascadeService)
    {
        _context = context;
        _userServiceClient = userServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _cascadeService = cascadeService;
    }

    // Every non-internal action needs the caller's userId; centralized here so each
    // action doesn't repeat the claim lookup and null-check.
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/reservations
    [HttpPost]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var userId = GetUserId();

        // Per milestone-2's documented flow, the 5-reservation-limit check goes through
        // UserService's /validate endpoint (which itself calls back into our own
        // /statistics endpoint) rather than querying our own table directly — this also
        // confirms the user exists and is Active, which we have no other way to check.
        var validation = await _userServiceClient.ValidateUserAsync(userId);
        if (validation is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("INTERNAL_SERVER_ERROR", "Unable to validate user account"));
        }

        if (validation.ActiveReservationsCount >= ReservationRules.MaxActiveReservations)
        {
            return BadRequest(new ReservationLimitExceededResponse
            {
                Message = $"You have reached the maximum of {ReservationRules.MaxActiveReservations} active reservations",
                CurrentReservations = validation.ActiveReservationsCount
            });
        }

        var book = await _catalogServiceClient.GetBookAsync(request.BookId);
        if (book is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Book not found with ID: {request.BookId}"));
        }

        if (book.AvailableCopies <= 0)
        {
            return BadRequest(new BookUnavailableResponse
            {
                Message = "No copies available for reservation",
                AvailableCopies = book.AvailableCopies
            });
        }

        var updateResult = await _catalogServiceClient.UpdateAvailabilityAsync(request.BookId, delta: -1);
        if (updateResult is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("INTERNAL_SERVER_ERROR", "Unable to update book availability"));
        }

        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            BookId = request.BookId,
            UserId = userId,
            Status = ReservationStatus.Reserved,
            ReservedAt = now,
            ExpiresAt = now.AddDays(ReservationRules.ReservationExpiryDays),
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ReservationResponse.FromReservation(reservation));
    }

    // GET /api/reservations
    [HttpGet]
    public async Task<IActionResult> GetActiveReservations()
    {
        var userId = GetUserId();

        var reservations = await _context.Reservations
            .Where(r => r.UserId == userId &&
                (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut))
            .ToListAsync();

        return Ok(new ActiveReservationsResponse
        {
            Reservations = reservations.Select(ActiveReservationDto.FromReservation).ToList(),
            TotalActive = reservations.Count
        });
    }

    // POST /api/reservations/{reservationId}/checkout
    [HttpPost("{reservationId:guid}/checkout")]
    public async Task<IActionResult> CheckoutReservation(Guid reservationId, [FromBody] CheckoutRequest request)
    {
        // Manual role check (rather than [Authorize(Roles = "Librarian")]) so the 403
        // body can carry this endpoint's exact contract message, distinct from return's.
        if (User.FindFirstValue(ClaimTypes.Role) != "Librarian")
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiErrorResponse("FORBIDDEN", "Only librarians can checkout books"));
        }

        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Reservation not found with ID: {reservationId}"));
        }

        if (reservation.Status != ReservationStatus.Reserved)
        {
            return BadRequest(new InvalidStatusResponse
            {
                Message = "Can only checkout reservations with RESERVED status",
                CurrentStatus = reservation.Status.ToWireString()
            });
        }

        reservation.CheckedOutAt = DateTime.UtcNow;
        reservation.DueDate = reservation.CheckedOutAt.Value.AddDays(ReservationRules.CheckoutPeriodDays);
        reservation.Status = ReservationStatus.CheckedOut;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            reservation.Notes = request.Notes;
        }

        await _context.SaveChangesAsync();

        return Ok(new CheckoutResponse
        {
            ReservationId = reservation.ReservationId,
            Status = reservation.Status.ToWireString(),
            CheckedOutAt = reservation.CheckedOutAt.Value,
            DueDate = reservation.DueDate.Value,
            Message = $"Book checked out successfully. Due date: {reservation.DueDate.Value:MMMM d, yyyy}"
        });
    }

    // POST /api/reservations/{reservationId}/return
    [HttpPost("{reservationId:guid}/return")]
    public async Task<IActionResult> ReturnReservation(Guid reservationId, [FromBody] ReturnRequest request)
    {
        if (User.FindFirstValue(ClaimTypes.Role) != "Librarian")
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiErrorResponse("FORBIDDEN", "Only librarians can process returns"));
        }

        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Reservation not found with ID: {reservationId}"));
        }

        if (reservation.Status != ReservationStatus.CheckedOut)
        {
            return BadRequest(new InvalidStatusResponse
            {
                Message = "Can only return books with CHECKED_OUT status",
                CurrentStatus = reservation.Status.ToWireString()
            });
        }

        var returnedAt = DateTime.UtcNow;
        var lateDays = 0;
        // dueDate is always set by this point (checkout always sets it), so this is safe.
        if (returnedAt > reservation.DueDate!.Value)
        {
            lateDays = (int)Math.Ceiling((returnedAt - reservation.DueDate.Value).TotalDays);
        }
        var lateFee = lateDays * ReservationRules.LateFeePerDay;

        reservation.Status = ReservationStatus.Returned;
        reservation.ReturnedAt = returnedAt;
        reservation.Condition = request.Condition;
        reservation.LateDays = lateDays;
        reservation.LateFee = lateFee;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            reservation.Notes = request.Notes;
        }

        await _context.SaveChangesAsync();

        // Decides whether the freed copy goes to the next waitlisted patron or back to
        // the general pool — see WaitlistCascadeService for the eligibility logic.
        await _cascadeService.ReleaseOrCascadeAsync(reservation.BookId);

        var message = lateDays > 0
            ? $"Book returned. Late fee of ${lateFee:F2} applied to account."
            : "Book returned successfully";

        return Ok(new ReturnResponse
        {
            ReservationId = reservation.ReservationId,
            ReturnedAt = returnedAt,
            DueDate = lateDays > 0 ? reservation.DueDate : null,
            LateDays = lateDays,
            LateFee = lateFee,
            Message = message
        });
    }

    // GET /api/reservations/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var userId = GetUserId();
        page = Math.Max(page, 0);
        size = Math.Clamp(size, 1, 100);

        var query = _context.Reservations.Where(r => r.UserId == userId);

        var totalElements = await query.CountAsync();
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)size);

        // Most-recent-first by whichever timestamp is the latest meaningful one for
        // that record: Returned rows sort by ReturnedAt, still-open ones by
        // CheckedOutAt, and never-picked-up ones by ReservedAt.
        var reservations = await query
            .OrderByDescending(r => r.ReturnedAt ?? r.CheckedOutAt ?? r.ReservedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return Ok(new PagedHistoryResponse
        {
            Content = reservations.Select(HistoryEntryDto.FromReservation).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = page >= totalPages - 1
        });
    }

    // POST /api/reservations/waitlist
    [HttpPost("waitlist")]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
    {
        var userId = GetUserId();

        var book = await _catalogServiceClient.GetBookAsync(request.BookId);
        if (book is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Book not found with ID: {request.BookId}"));
        }

        if (book.AvailableCopies > 0)
        {
            return BadRequest(new ApiErrorResponse("BOOK_AVAILABLE",
                "This book currently has available copies - reserve it directly instead of joining the waitlist"));
        }

        var alreadyWaiting = await _context.WaitlistEntries.AnyAsync(w =>
            w.BookId == request.BookId && w.UserId == userId && w.Status == WaitlistStatus.Waiting);
        if (alreadyWaiting)
        {
            return BadRequest(new ApiErrorResponse("ALREADY_WAITLISTED",
                "You are already on the waitlist for this book"));
        }

        var entry = new WaitlistEntry
        {
            WaitlistId = Guid.NewGuid(),
            BookId = request.BookId,
            UserId = userId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        // Position is computed BEFORE inserting the new row: count of existing Waiting
        // entries ahead of it, plus 1 for itself.
        var position = await _context.WaitlistEntries.CountAsync(w =>
            w.BookId == request.BookId && w.Status == WaitlistStatus.Waiting) + 1;

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Built directly (not via WaitlistEntryResponse.FromEntry) because the create
        // response's contract shape omits bookAuthor, unlike the GET /waitlist list.
        return StatusCode(StatusCodes.Status201Created, new WaitlistEntryResponse
        {
            WaitlistId = entry.WaitlistId,
            BookId = entry.BookId,
            BookTitle = entry.BookTitle,
            Status = entry.Status.ToString().ToUpperInvariant(),
            JoinedAt = entry.JoinedAt,
            Position = position
        });
    }

    // GET /api/reservations/waitlist
    [HttpGet("waitlist")]
    public async Task<IActionResult> GetMyWaitlist()
    {
        var userId = GetUserId();

        var entries = await _context.WaitlistEntries
            .Where(w => w.UserId == userId &&
                (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified))
            .ToListAsync();

        var responses = new List<WaitlistEntryResponse>();
        foreach (var entry in entries)
        {
            var dto = WaitlistEntryResponse.FromEntry(entry);
            if (entry.Status == WaitlistStatus.Waiting)
            {
                // Recompute position live: entries ahead of this one, still Waiting,
                // for the same book, ordered by JoinedAt.
                dto.Position = await _context.WaitlistEntries.CountAsync(w =>
                    w.BookId == entry.BookId &&
                    w.Status == WaitlistStatus.Waiting &&
                    w.JoinedAt < entry.JoinedAt) + 1;
            }
            responses.Add(dto);
        }

        return Ok(new WaitlistListResponse { Entries = responses });
    }

    // DELETE /api/reservations/waitlist/{waitlistId}
    [HttpDelete("waitlist/{waitlistId:guid}")]
    public async Task<IActionResult> CancelWaitlistEntry(Guid waitlistId)
    {
        var userId = GetUserId();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.WaitlistId == waitlistId && w.UserId == userId);
        if (entry is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Waitlist entry not found"));
        }

        var wasNotified = entry.Status == WaitlistStatus.Notified;
        entry.Status = WaitlistStatus.Cancelled;
        await _context.SaveChangesAsync();

        if (wasNotified)
        {
            // This entry was actively holding a claim on a copy — cancelling it must
            // immediately hand that copy to the next eligible patron (or release it),
            // not leave it in limbo until the background job's next tick.
            await _cascadeService.ReleaseOrCascadeAsync(entry.BookId);
        }

        return Ok(new CancelWaitlistResponse { WaitlistId = entry.WaitlistId });
    }

    // GET /api/reservations/statistics/{userId}  (internal — called by UserService's
    // /profile and /validate endpoints; see UserService/Controllers/UsersController.cs)
    [AllowAnonymous]
    [HttpGet("statistics/{userId:guid}")]
    public async Task<IActionResult> GetStatistics(Guid userId)
    {
        var activeReservations = await _context.Reservations.CountAsync(r =>
            r.UserId == userId && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut));

        // "Completed" borrowing history per milestone-2/api-contracts.md's wording —
        // Returned only, not every historical status (e.g. Cancelled isn't a completed loan).
        var borrowingHistory = await _context.Reservations.CountAsync(r =>
            r.UserId == userId && r.Status == ReservationStatus.Returned);

        return Ok(new ReservationStatisticsResponse
        {
            UserId = userId,
            ActiveReservations = activeReservations,
            BorrowingHistory = borrowingHistory
        });
    }
}
