namespace SafeByte.Models;

public class UserAllergenPreferencesResponse
{
    public string Email { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
    public List<string> AllergenKeys { get; set; } = new List<string>();
    public DateTime? UpdatedAtUtc { get; set; }
}
