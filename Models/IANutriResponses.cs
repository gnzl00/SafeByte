namespace SafeByte.Models;

public class IANutriReformulateResponse
{
    public string OptionLabel { get; set; } = string.Empty;
    public string ReformulatedPrompt { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
    public List<string> Notes { get; set; } = new List<string>();
}

public class IANutriGenerateSuggestionsResponse
{
    public string HistoryId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ReformulatedPrompt { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
    public List<string> GlobalWarnings { get; set; } = new List<string>();
    public List<string> GeneralSubstitutions { get; set; } = new List<string>();
    public List<IANutriRecipeSuggestion> Suggestions { get; set; } = new List<IANutriRecipeSuggestion>();
}

public class IANutriRecipeSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EstimatedTime { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new List<string>();
    public List<string> Steps { get; set; } = new List<string>();
    public List<string> AllergensDetected { get; set; } = new List<string>();
    public List<string> SafeSubstitutions { get; set; } = new List<string>();
    public string AllergyWarning { get; set; } = string.Empty;
}

public class IANutriCookingAssistantResponse
{
    public string RecipeTitle { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public List<string> RequiredItems { get; set; } = new List<string>();
    public List<string> StepByStep { get; set; } = new List<string>();
    public List<string> SafetyNotes { get; set; } = new List<string>();
}

public class IANutriHistoryItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public string ReformulatedPrompt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Allergens { get; set; } = new List<string>();
    public List<string> GlobalWarnings { get; set; } = new List<string>();
    public List<string> GeneralSubstitutions { get; set; } = new List<string>();
    public List<IANutriRecipeSuggestion> Suggestions { get; set; } = new List<IANutriRecipeSuggestion>();
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
