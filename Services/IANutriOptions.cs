namespace SafeByte.Services;

public class IANutriOptions
{
    public string BaseUrl { get; set; } = "https://models.inference.ai.azure.com";
    public string ApiKey { get; set; } = string.Empty;
    public string ReformulationModel { get; set; } = "gpt-4.1-nano";
    public string SuggestionModel { get; set; } = "gpt-4.1";
    public string CookingAssistantModel { get; set; } = "gpt-4.1";
    public int TimeoutSeconds { get; set; } = 60;
}
