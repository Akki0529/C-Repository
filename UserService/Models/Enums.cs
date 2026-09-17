namespace UserService.Models;

// Role controls what a user can do in the system.
// Patron: browse, reserve, view personal history.
// Librarian: additionally process checkouts and returns at the desk.
// New registrations always default to Patron; Librarian accounts are
// created directly in the database by an administrator.
public enum UserRole
{
    Patron,
    Librarian
}

// MembershipStatus gates access to reservation features.
// A Suspended user can still log in but ReservationService will
// reject reservation attempts when it validates the user.
public enum MembershipStatus
{
    Active,
    Suspended
}
