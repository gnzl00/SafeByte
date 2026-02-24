namespace SafeByte.Models;

public class UpdateUserAllergensRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
}
