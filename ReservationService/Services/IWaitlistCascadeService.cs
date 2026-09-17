namespace ReservationService.Services;

public interface IWaitlistCascadeService
{
    // Called whenever a copy of a book becomes free without going through the normal
    // "reserve" flow: a book return, a voluntary cancellation of a Notified entry, or
    // the background job expiring a Notified entry. In every one of those cases the
    // copy needs to go to the next eligible waitlisted patron, or — if the queue is
    // empty or everyone in it is over their reservation limit — back to the general
    // available pool via CatalogService. See milestone-4's "Waitlist Claim Eligibility"
    // section for the exact rule this implements.
    Task ReleaseOrCascadeAsync(Guid bookId);
}
