namespace ReservationService.Services;

// Single source of truth for the business-rule constants from milestone-4's
// "General Technical Requirements" section, so they're not magic numbers scattered
// across the controller, cascade service, and background job.
public static class ReservationRules
{
    public const int MaxActiveReservations = 5;
    public const int ReservationExpiryDays = 7;
    public const int CheckoutPeriodDays = 14;
    public const decimal LateFeePerDay = 1.00m;
    public const int WaitlistClaimWindowHours = 48;
}
