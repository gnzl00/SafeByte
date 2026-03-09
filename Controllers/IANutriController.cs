using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using SafeByte.Models;
using SafeByte.Services;

namespace SafeByte.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IANutriController : ControllerBase
{
    private const int HistoryLimit = 40;
    private readonly IIANutriService _ianutriService;
    private readonly CollectionReference _users;

    public IANutriController(IIANutriService ianutriService, FirestoreDb firestoreDb)
    {
        _ianutriService = ianutriService;
        _users = firestoreDb.Collection("users");
    }

    [HttpPost("Reformulate")]
    public async Task<IActionResult> Reformulate(
        [FromBody] IANutriReformulateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest(new { Message = "Debes describir que quieres comer o que ingredientes tienes." });
        }

        var optionLabel = NormalizeOptionLabel(request.Option);
        var allergens = await ResolveAllergensAsync(request.Email, request.Allergens, cancellationToken);

        try
        {
            var response = await _ianutriService.ReformulateAsync(
                request.UserInput,
                optionLabel,
                allergens,
                cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
    }

    [HttpPost("GenerateSuggestions")]
    public async Task<IActionResult> GenerateSuggestions(
        [FromBody] IANutriGenerateSuggestionsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest(new { Message = "Debes indicar ingredientes o una preferencia para generar sugerencias." });
        }

        request.Option = NormalizeOptionLabel(request.Option);
        var normalizedEmail = NormalizeEmail(request.Email);
        var allergens = await ResolveAllergensAsync(normalizedEmail, request.Allergens, cancellationToken);

        try
        {
            var response = await _ianutriService.GenerateSuggestionsAsync(request, allergens, cancellationToken);
            response.HistoryId = await SaveHistoryAsync(normalizedEmail, request, response, allergens);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
    }

    [HttpPost("CookingAssistant")]
    public async Task<IActionResult> CookingAssistant(
        [FromBody] IANutriCookingAssistantRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Recipe is null || string.IsNullOrWhiteSpace(request.Recipe.Title))
        {
            return BadRequest(new { Message = "Selecciona una receta valida para abrir el asistente de cocina." });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var allergens = await ResolveAllergensAsync(normalizedEmail, request.Allergens, cancellationToken);

        try
        {
            var response = await _ianutriService.BuildCookingAssistantAsync(request, allergens, cancellationToken);
            await UpdateHistoryWithAssistantAsync(normalizedEmail, request, response);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
        }
    }

    [HttpGet("History")]
    public async Task<IActionResult> GetHistory([FromQuery] string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Ok(new { History = new List<IANutriHistoryItem>() });
        }

        try
        {
            var snapshots = await GetHistoryCollection(normalizedEmail)
                .OrderByDescending("createdAt")
                .Limit(HistoryLimit)
                .GetSnapshotAsync();

            var history = snapshots.Documents
                .Select(MapHistoryItem)
                .ToList();

            return Ok(new { History = history });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"No se pudo cargar el historial: {ex.Message}" });
        }
    }

    [HttpDelete("History")]
    public async Task<IActionResult> DeleteHistory([FromQuery] string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest(new { Message = "Email invalido." });
        }

        try
        {
            var snapshots = await GetHistoryCollection(normalizedEmail).GetSnapshotAsync();
            var deleted = 0;

            foreach (var doc in snapshots.Documents)
            {
                await doc.Reference.DeleteAsync();
                deleted++;
            }

            return Ok(new
            {
                Message = "Historial de IANutri eliminado correctamente.",
                Deleted = deleted
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"No se pudo borrar el historial: {ex.Message}" });
        }
    }

    private async Task<List<string>> ResolveAllergensAsync(
        string? email,
        IEnumerable<string>? fallbackAllergens,
        CancellationToken cancellationToken)
    {
        var fallback = AllergenCatalog.NormalizeMany(fallbackAllergens, out _);
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return fallback;
        }

        try
        {
            var snapshot = await _users.Document(normalizedEmail).GetSnapshotAsync(cancellationToken);
            if (!snapshot.Exists)
            {
                return fallback;
            }

            if (!snapshot.TryGetValue<List<string>>("allergens", out var remoteAllergens))
            {
                return fallback;
            }

            return AllergenCatalog.NormalizeMany(remoteAllergens, out _);
        }
        catch
        {
            return fallback;
        }
    }

    private async Task<string> SaveHistoryAsync(
        string normalizedEmail,
        IANutriGenerateSuggestionsRequest request,
        IANutriGenerateSuggestionsResponse response,
        IReadOnlyList<string> allergens)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return string.Empty;
        }

        try
        {
            var now = Timestamp.GetCurrentTimestamp();
            var docRef = GetHistoryCollection(normalizedEmail).Document();
            var historyPayload = new Dictionary<string, object>
            {
                ["email"] = normalizedEmail,
                ["userInput"] = request.UserInput.Trim(),
                ["option"] = request.Option.Trim(),
                ["reformulatedPrompt"] = response.ReformulatedPrompt,
                ["summary"] = response.Summary,
                ["allergens"] = allergens.ToList(),
                ["globalWarnings"] = response.GlobalWarnings,
                ["generalSubstitutions"] = response.GeneralSubstitutions,
                ["suggestions"] = response.Suggestions.Select(MapSuggestionToDictionary).ToList(),
                ["createdAt"] = now,
                ["updatedAt"] = now
            };

            await docRef.SetAsync(historyPayload);
            return docRef.Id;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task UpdateHistoryWithAssistantAsync(
        string normalizedEmail,
        IANutriCookingAssistantRequest request,
        IANutriCookingAssistantResponse assistant)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.HistoryId))
        {
            return;
        }

        try
        {
            var now = Timestamp.GetCurrentTimestamp();
            var historyRef = GetHistoryCollection(normalizedEmail).Document(request.HistoryId);
            var snapshot = await historyRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["assistant"] = new Dictionary<string, object>
                {
                    ["recipeTitle"] = assistant.RecipeTitle,
                    ["intro"] = assistant.Intro,
                    ["requiredItems"] = assistant.RequiredItems,
                    ["stepByStep"] = assistant.StepByStep,
                    ["safetyNotes"] = assistant.SafetyNotes,
                    ["sourceRecipeTitle"] = request.Recipe?.Title ?? string.Empty
                },
                ["updatedAt"] = now
            };

            await historyRef.UpdateAsync(payload);
        }
        catch
        {
            // Ignorado: no bloquea la respuesta principal.
        }
    }

    private CollectionReference GetHistoryCollection(string normalizedEmail)
    {
        return _users.Document(normalizedEmail).Collection("ianutriHistory");
    }

    private static Dictionary<string, object> MapSuggestionToDictionary(IANutriRecipeSuggestion suggestion)
    {
        return new Dictionary<string, object>
        {
            ["title"] = suggestion.Title,
            ["description"] = suggestion.Description,
            ["estimatedTime"] = suggestion.EstimatedTime,
            ["difficulty"] = suggestion.Difficulty,
            ["ingredients"] = suggestion.Ingredients,
            ["steps"] = suggestion.Steps,
            ["allergensDetected"] = suggestion.AllergensDetected,
            ["safeSubstitutions"] = suggestion.SafeSubstitutions,
            ["allergyWarning"] = suggestion.AllergyWarning
        };
    }

    private static IANutriHistoryItem MapHistoryItem(DocumentSnapshot snapshot)
    {
        var data = snapshot.ToDictionary();
        return new IANutriHistoryItem
        {
            Id = snapshot.Id,
            Email = ReadString(data, "email"),
            UserInput = ReadString(data, "userInput"),
            Option = ReadString(data, "option"),
            ReformulatedPrompt = ReadString(data, "reformulatedPrompt"),
            Summary = ReadString(data, "summary"),
            Allergens = ReadStringList(data, "allergens"),
            GlobalWarnings = ReadStringList(data, "globalWarnings"),
            GeneralSubstitutions = ReadStringList(data, "generalSubstitutions"),
            Suggestions = ReadSuggestions(data),
            CreatedAtUtc = ReadTimestamp(data, "createdAt"),
            UpdatedAtUtc = ReadTimestamp(data, "updatedAt")
        };
    }

    private static string ReadString(IDictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var raw) || raw is null)
        {
            return string.Empty;
        }

        return raw.ToString()?.Trim() ?? string.Empty;
    }

    private static List<string> ReadStringList(IDictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var raw) || raw is null)
        {
            return new List<string>();
        }

        if (raw is IEnumerable<string> typed)
        {
            return typed
                .Where((item) => !string.IsNullOrWhiteSpace(item))
                .Select((item) => item.Trim())
                .ToList();
        }

        if (raw is IEnumerable<object> objects)
        {
            return objects
                .Select((item) => item?.ToString()?.Trim())
                .Where((item) => !string.IsNullOrWhiteSpace(item))
                .Select((item) => item!)
                .ToList();
        }

        return new List<string>();
    }

    private static DateTime? ReadTimestamp(IDictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var raw) || raw is null)
        {
            return null;
        }

        if (raw is Timestamp timestamp)
        {
            return timestamp.ToDateTime().ToUniversalTime();
        }

        if (raw is DateTime dateTime)
        {
            return dateTime.ToUniversalTime();
        }

        return null;
    }

    private static List<IANutriRecipeSuggestion> ReadSuggestions(IDictionary<string, object> data)
    {
        if (!data.TryGetValue("suggestions", out var raw) || raw is not IEnumerable<object> list)
        {
            return new List<IANutriRecipeSuggestion>();
        }

        var result = new List<IANutriRecipeSuggestion>();
        foreach (var item in list)
        {
            if (item is not IDictionary<string, object> map)
            {
                continue;
            }

            result.Add(new IANutriRecipeSuggestion
            {
                Title = ReadString(map, "title"),
                Description = ReadString(map, "description"),
                EstimatedTime = ReadString(map, "estimatedTime"),
                Difficulty = ReadString(map, "difficulty"),
                Ingredients = ReadStringList(map, "ingredients"),
                Steps = ReadStringList(map, "steps"),
                AllergensDetected = ReadStringList(map, "allergensDetected"),
                SafeSubstitutions = ReadStringList(map, "safeSubstitutions"),
                AllergyWarning = ReadString(map, "allergyWarning")
            });
        }

        return result;
    }

    private static string NormalizeOptionLabel(string? option)
    {
        var normalized = option?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "rapido-basico" => "Rapido y basico",
            "rapido y basico" => "Rapido y basico",
            "cena-ligera" => "Cena ligera",
            "cena ligera" => "Cena ligera",
            "menu-semanal" => "Menu semanal",
            "menu semanal" => "Menu semanal",
            _ => "Personalizado"
        };
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
