namespace SafeByte.Models;

public class IANutriReformulateRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
}

public class IANutriGenerateSuggestionsRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public string ReformulatedPrompt { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
}

public class IANutriCookingAssistantRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
    public string HistoryId { get; set; } = string.Empty;
    public IANutriRecipeSuggestion? Recipe { get; set; }
}
