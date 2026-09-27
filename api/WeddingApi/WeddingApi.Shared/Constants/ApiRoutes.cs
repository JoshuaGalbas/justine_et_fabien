namespace WeddingApi.Shared.Constants;

public static class ApiRoutes
{
    public const string Health = "health";
    public const string PublicContent = "public/content";
    public const string AuthRegister = "auth/register";
    public const string AuthLogin = "auth/login";
    public const string AuthLogout = "auth/logout";
    public const string HouseholdMe = "household/me";
    public const string HouseholdProfile = "household/profile";
    public const string RsvpMyHousehold = "rsvp/my-household";
    public const string RsvpSave = "rsvp/save";
    public const string RsvpGuest = "rsvp/guest";
    public const string AdminDashboard = "organizer/dashboard";
    public const string AdminHouseholds = "organizer/households";
    public const string AuditLog = "organizer/audit-log";
    public const string GalleryUpload = "organizer/gallery/upload";
    public const string AdminHouseholdStatus = "organizer/household/{householdId}/status";
    public const string AdminGalleryApprove = "organizer/gallery/approve/{photoId}";
    public const string AdminGalleryDelete = "organizer/gallery/{photoId}";
}
