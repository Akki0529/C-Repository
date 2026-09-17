namespace ReservationService.Models;

// Full reservation lifecycle: Reserved → CheckedOut → Returned.
// Cancelled covers reservations the patron cancels before pickup.
public enum ReservationStatus
{
    Reserved,
    CheckedOut,
    Returned,
    Cancelled
}

// Condition is recorded by the Librarian when a book is returned.
// Allows the library to track wear and flag damaged copies for repair.
public enum BookCondition
{
    Good,
    Fair,
    Poor,
    Damaged
}

// Waitlist lifecycle for a single entry:
// Waiting  → patron is queued; has not been offered a copy yet.
// Notified → a returned copy was auto-reserved for this patron;
//            they have 48 hours to claim it (pick up at the desk).
// Claimed  → the patron checked out the auto-created reservation
//            (set for record-keeping; currently not used in active logic).
// Expired  → the 48-hour claim window passed; copy cascaded to next in queue.
// Cancelled→ the patron voluntarily left the waitlist.
public enum WaitlistStatus
{
    Waiting,
    Notified,
    Claimed,
    Expired,
    Cancelled
}

public static class ReservationStatusExtensions
{
    // api-contracts.md represents CheckedOut as "CHECKED_OUT" (with an underscore) on
    // the wire, not "CHECKEDOUT" — plain ToString().ToUpperInvariant() would produce the
    // latter, since C# PascalCase has no word boundary marker once uppercased. Every
    // other ReservationStatus value happens to be a single word, so this is the only
    // case that needs explicit handling.
    public static string ToWireString(this ReservationStatus status) => status switch
    {
        ReservationStatus.CheckedOut => "CHECKED_OUT",
        _ => status.ToString().ToUpperInvariant()
    };
}
