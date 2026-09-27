namespace WeddingApi.Functions.Models;

public sealed class HouseholdAccount
{
    public string Id { get; set; } = string.Empty;
    public string HouseholdName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<HouseholdGuest> Adults { get; set; } = new();
    public List<HouseholdGuest> Children { get; set; } = new();
    public Dictionary<string, string> Rsvp { get; set; } = new();
}

public sealed class HouseholdGuest
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "ADULT";
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Attendance { get; set; } = "NOT_ANSWERED";
    public string? DietaryRestrictions { get; set; }
    public string? Allergies { get; set; }
    public string? AccessibilityRequirements { get; set; }
    public string? Note { get; set; }
}
