using SafeByte.Models;

namespace SafeByte.Services;

public interface IIANutriService
{
    Task<IANutriReformulateResponse> ReformulateAsync(
        string userInput,
        string optionLabel,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default);

    Task<IANutriGenerateSuggestionsResponse> GenerateSuggestionsAsync(
        IANutriGenerateSuggestionsRequest request,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default);

    Task<IANutriCookingAssistantResponse> BuildCookingAssistantAsync(
        IANutriCookingAssistantRequest request,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default);
}
