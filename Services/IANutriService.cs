using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafeByte.Models;

namespace SafeByte.Services;

public class IANutriService : IIANutriService
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IOptions<IANutriOptions> _options;

    public IANutriService(HttpClient httpClient, IOptions<IANutriOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IANutriReformulateResponse> ReformulateAsync(
        string userInput,
        string optionLabel,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default)
    {
        var safeUserInput = SanitizeInput(userInput, 600);
        if (string.IsNullOrWhiteSpace(safeUserInput))
        {
            throw new InvalidOperationException("El texto de entrada no puede estar vacio.");
        }

        var safeOptionLabel = string.IsNullOrWhiteSpace(optionLabel)
            ? "Personalizado"
            : optionLabel.Trim();
        var safeAllergens = NormalizeList(allergens, 10);

        var systemPrompt = """
            Eres IANutri, un asistente experto en menus seguros para alergias alimentarias.
            Tu tarea es REFORMULAR la peticion del usuario para que otro modelo genere recetas.
            Reglas estrictas:
            - Responde siempre en espanol.
            - No inventes ingredientes ni restricciones nuevas.
            - Conserva ingredientes, objetivo, contexto y modo.
            - Trata los alergenos como restriccion obligatoria de seguridad.
            - Si hay conflicto probable con alergenos, indicalo en notas.
            - Prohibido mencionar modelos, proveedores o tecnologia (ej. GPT, OpenAI, nano, mini).
            - Prohibido incluir encabezados, etiquetas, listas o metadatos dentro de reformulatedPrompt.
            - Sin markdown, sin texto fuera del JSON.
            Devuelve SOLO JSON valido con este esquema exacto:
            {
              "reformulatedPrompt": "string",
              "focus": "string",
              "notes": ["string"]
            }
            Restricciones de formato:
            - reformulatedPrompt: una sola frase operativa (maximo 220 caracteres), limpia y accionable.
            - focus: una etiqueta corta ("rapido", "ligero", "plan-semanal" o "personalizado").
            - notes: maximo 3 notas breves de seguridad, sin referencias a modelos.
            """;

        var userPrompt = $"""
            User input:
            {safeUserInput}

            Mode selected:
            {safeOptionLabel}

            Allergens to avoid:
            {FormatListForPrompt(safeAllergens)}
            """;

        var reformulationCandidates = BuildModelCandidates(
            _options.Value.ReformulationModel,
            "gpt-4.1-nano",
            "gpt-4.1-mini",
            "gpt-4o-mini");

        var (raw, _) = await SendCompletionAsync(
            reformulationCandidates,
            systemPrompt,
            userPrompt,
            temperature: 0.3,
            maxTokens: 420,
            cancellationToken);

        var parsed = TryParseModelJson<ReformulateModelOutput>(raw);
        var reformulatedPrompt = NormalizeReformulatedPrompt(parsed?.ReformulatedPrompt);
        if (string.IsNullOrWhiteSpace(reformulatedPrompt))
        {
            reformulatedPrompt = BuildFallbackReformulatedPrompt(safeUserInput, safeOptionLabel, safeAllergens);
        }

        return new IANutriReformulateResponse
        {
            OptionLabel = safeOptionLabel,
            ReformulatedPrompt = reformulatedPrompt.Trim(),
            Allergens = safeAllergens,
            Notes = BuildReformulationNotes(parsed, safeAllergens)
        };
    }

    public async Task<IANutriGenerateSuggestionsResponse> GenerateSuggestionsAsync(
        IANutriGenerateSuggestionsRequest request,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default)
    {
        var safeUserInput = SanitizeInput(request.UserInput, 600);
        if (string.IsNullOrWhiteSpace(safeUserInput))
        {
            throw new InvalidOperationException("Debes incluir ingredientes o preferencia de menu.");
        }

        var safeAllergens = NormalizeList(allergens, 10);
        var safeOptionLabel = string.IsNullOrWhiteSpace(request.Option)
            ? "Personalizado"
            : request.Option.Trim();
        var reformulatedPrompt = SanitizeInput(request.ReformulatedPrompt, 1000);
        if (string.IsNullOrWhiteSpace(reformulatedPrompt))
        {
            reformulatedPrompt = BuildFallbackReformulatedPrompt(safeUserInput, safeOptionLabel, safeAllergens);
        }

        var systemPrompt = """
            Eres IANutri, chef-nutricionista experto en recetas seguras para alergias.
            Genera sugerencias consistentes y accionables.
            Reglas obligatorias:
            - Responde solo en espanol.
            - Salida exclusivamente JSON valido (sin markdown).
            - Respeta estrictamente los alergenos del usuario.
            - Usa solo dificultad: "Baja", "Media" o "Alta".
            - Tiempo estimado en formato "NN min" o "N1-N2 min".
            - Devuelve entre 1 y 3 recetas.
            - Cada receta con 4-12 ingredientes y 3-8 pasos claros.
            - Si hay conflicto con alergenos (ej. pizza y gluten), incluye advertencia y sustituciones concretas.
            - En allergensDetected solo usa alergenos relevantes detectados en la receta.
            Esquema JSON exacto:
            {
              "summary": "string",
              "recipes": [
                {
                  "title": "string",
                  "description": "string",
                  "estimatedTime": "string",
                  "difficulty": "string",
                  "ingredients": ["string"],
                  "steps": ["string"],
                  "allergensDetected": ["string"],
                  "safeSubstitutions": ["string"],
                  "allergyWarning": "string"
                }
              ],
              "globalWarnings": ["string"],
              "generalSubstitutions": ["string"]
            }
            Si faltan datos, asume cantidades practicas para 1-2 porciones sin romper restricciones.
            """;

        var userPrompt = $"""
            Original user input:
            {safeUserInput}

            Reformulated prompt:
            {reformulatedPrompt}

            Mode selected:
            {safeOptionLabel}

            Allergens to avoid:
            {FormatListForPrompt(safeAllergens)}
            """;

        var suggestionCandidates = BuildModelCandidates(
            _options.Value.SuggestionModel,
            "gpt-4.1",
            "gpt-4.1-mini",
            "gpt-4o");

        var (raw, _) = await SendCompletionAsync(
            suggestionCandidates,
            systemPrompt,
            userPrompt,
            temperature: 0.45,
            maxTokens: 1600,
            cancellationToken);

        var parsed = TryParseModelJson<SuggestionsModelOutput>(raw);
        var recipes = parsed?.Recipes?
            .Select((recipe) => MapSuggestionRecipe(recipe, safeAllergens))
            .Where((recipe) => !string.IsNullOrWhiteSpace(recipe.Title))
            .Take(3)
            .ToList()
            ?? new List<IANutriRecipeSuggestion>();

        if (recipes.Count == 0)
        {
            recipes.Add(BuildFallbackSuggestion(raw, safeAllergens));
        }

        var globalWarnings = NormalizeList(parsed?.GlobalWarnings, 6);
        var generalSubstitutions = NormalizeList(parsed?.GeneralSubstitutions, 8);

        foreach (var recipe in recipes)
        {
            ApplyAllergenConflicts(recipe, safeAllergens);
        }

        if (safeAllergens.Count > 0 && globalWarnings.Count == 0)
        {
            globalWarnings.Add($"Revisa trazas y contaminacion cruzada para: {string.Join(", ", safeAllergens)}.");
        }

        if (safeAllergens.Count > 0 && generalSubstitutions.Count == 0)
        {
            generalSubstitutions.Add("Usa versiones certificadas sin alergenos y revisa etiquetas antes de cocinar.");
        }

        return new IANutriGenerateSuggestionsResponse
        {
            Summary = string.IsNullOrWhiteSpace(parsed?.Summary)
                ? "Sugerencias generadas segun tu preferencia y alergenos."
                : parsed!.Summary.Trim(),
            ReformulatedPrompt = reformulatedPrompt,
            Allergens = safeAllergens,
            GlobalWarnings = globalWarnings,
            GeneralSubstitutions = generalSubstitutions,
            Suggestions = recipes
        };
    }

    public async Task<IANutriCookingAssistantResponse> BuildCookingAssistantAsync(
        IANutriCookingAssistantRequest request,
        IReadOnlyList<string> allergens,
        CancellationToken cancellationToken = default)
    {
        if (request.Recipe is null || string.IsNullOrWhiteSpace(request.Recipe.Title))
        {
            throw new InvalidOperationException("Debes seleccionar una sugerencia valida.");
        }

        var safeAllergens = NormalizeList(allergens, 10);
        var recipeTitle = SanitizeInput(request.Recipe.Title, 120);
        var recipeDescription = SanitizeInput(request.Recipe.Description, 400);
        var recipeIngredients = NormalizeList(request.Recipe.Ingredients, 20);
        var recipeSteps = NormalizeList(request.Recipe.Steps, 20);

        var systemPrompt = """
            Eres un asistente de cocina profesional en espanol.
            Debes guiar paso a paso la ejecucion segura de una receta.
            Reglas obligatorias:
            - Salida solo JSON valido, sin markdown.
            - 4-10 elementos en requiredItems.
            - 5-10 pasos concretos en steps.
            - safetyNotes debe incluir higiene y control de contaminacion cruzada.
            - Nunca contradigas restricciones de alergenos.
            Esquema exacto:
            {
              "recipeTitle": "string",
              "intro": "string",
              "requiredItems": ["string"],
              "steps": ["string"],
              "safetyNotes": ["string"]
            }
            """;

        var userPrompt = $"""
            Recipe title:
            {recipeTitle}

            Recipe description:
            {recipeDescription}

            Ingredients:
            {FormatListForPrompt(recipeIngredients)}

            Existing steps:
            {FormatListForPrompt(recipeSteps)}

            Allergens to avoid:
            {FormatListForPrompt(safeAllergens)}
            """;

        var assistantCandidates = BuildModelCandidates(
            _options.Value.CookingAssistantModel,
            "gpt-4.1",
            "gpt-4.1-mini",
            "gpt-4o");

        var (raw, _) = await SendCompletionAsync(
            assistantCandidates,
            systemPrompt,
            userPrompt,
            temperature: 0.35,
            maxTokens: 900,
            cancellationToken);

        var parsed = TryParseModelJson<CookingAssistantModelOutput>(raw);
        var requiredItems = NormalizeList(parsed?.RequiredItems, 14);
        if (requiredItems.Count == 0)
        {
            requiredItems = BuildFallbackRequiredItems(recipeIngredients);
        }

        var stepByStep = NormalizeList(parsed?.Steps, 16);
        if (stepByStep.Count == 0)
        {
            stepByStep = recipeSteps.Count > 0
                ? recipeSteps
                : new List<string>
                {
                    "Prepara y pesa los ingredientes.",
                    "Cocina segun el metodo indicado manteniendo fuego medio.",
                    "Ajusta condimentos y sirve revisando seguridad de alergenos."
                };
        }

        var safetyNotes = NormalizeList(parsed?.SafetyNotes, 8);
        if (safeAllergens.Count > 0)
        {
            var safetyHint = $"Evita contacto con: {string.Join(", ", safeAllergens)}.";
            if (!ContainsIgnoreCase(safetyNotes, safetyHint))
            {
                safetyNotes.Insert(0, safetyHint);
            }
        }

        if (safetyNotes.Count == 0)
        {
            safetyNotes.Add("Verifica etiquetas y evita contaminacion cruzada.");
        }

        return new IANutriCookingAssistantResponse
        {
            RecipeTitle = string.IsNullOrWhiteSpace(parsed?.RecipeTitle) ? recipeTitle : parsed!.RecipeTitle.Trim(),
            Intro = string.IsNullOrWhiteSpace(parsed?.Intro)
                ? $"Vamos a cocinar {recipeTitle} paso a paso."
                : parsed!.Intro.Trim(),
            RequiredItems = requiredItems,
            StepByStep = stepByStep,
            SafetyNotes = safetyNotes
        };
    }

    private async Task<(string Content, string ModelUsed)> SendCompletionAsync(
        IEnumerable<string> modelCandidates,
        string systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var preferGitHubModels = IsGitHubModelsEndpoint(options.BaseUrl);
        var apiKey = ResolveApiKey(options);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var expected = preferGitHubModels
                ? "GITHUB_MODELS_API_KEY o GITHUB_TOKEN"
                : "OPENAI_API_KEY";
            throw new InvalidOperationException($"No se encontro API key para IANutri. Configura {expected}.");
        }

        var timeoutSeconds = Math.Clamp(options.TimeoutSeconds, 10, 180);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var candidates = modelCandidates
            .Where((model) => !string.IsNullOrWhiteSpace(model))
            .Select((model) => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No hay modelos configurados para ejecutar IANutri.");
        }

        var completionsUrl = BuildCompletionsUrl(options.BaseUrl);
        string lastUnknownModelError = string.Empty;

        foreach (var model in candidates)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, completionsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            if (preferGitHubModels)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
            }
            else
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            request.Content = JsonContent.Create(new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (IsUnknownModelError((int)response.StatusCode, payload))
                {
                    lastUnknownModelError = TrimForError(payload);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Fallo la llamada al modelo ({(int)response.StatusCode}). {TrimForError(payload)}");
            }

            var content = ExtractAssistantMessage(payload);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("El modelo no devolvio contenido.");
            }

            return (content, model);
        }

        var modelList = string.Join(", ", candidates);
        throw new InvalidOperationException(
            $"No se encontro un modelo disponible. Intentados: {modelList}. Ultimo detalle: {lastUnknownModelError}");
    }

    private static string ResolveApiKey(IANutriOptions options)
    {
        var preferGitHubModels = IsGitHubModelsEndpoint(options.BaseUrl);

        var configuredApiKey = ResolveConfiguredApiKey(options.ApiKey);
        if (!string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return configuredApiKey;
        }

        if (preferGitHubModels)
        {
            return FirstNonEmpty(
                Environment.GetEnvironmentVariable("GITHUB_MODELS_API_KEY"),
                Environment.GetEnvironmentVariable("GITHUB_TOKEN"),
                Environment.GetEnvironmentVariable("IANUTRI_API_KEY"),
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        }

        return FirstNonEmpty(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            Environment.GetEnvironmentVariable("IANUTRI_API_KEY"),
            Environment.GetEnvironmentVariable("GITHUB_MODELS_API_KEY"),
            Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
    }

    private static string ResolveConfiguredApiKey(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return string.Empty;
        }

        var trimmed = configuredValue.Trim();
        if (trimmed.StartsWith("${", StringComparison.Ordinal) &&
            trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            var variableName = trimmed[2..^1].Trim();
            return string.IsNullOrWhiteSpace(variableName)
                ? string.Empty
                : Environment.GetEnvironmentVariable(variableName)?.Trim() ?? string.Empty;
        }

        if (trimmed.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var variableName = trimmed["env:".Length..].Trim();
            return string.IsNullOrWhiteSpace(variableName)
                ? string.Empty
                : Environment.GetEnvironmentVariable(variableName)?.Trim() ?? string.Empty;
        }

        return trimmed;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<string> BuildModelCandidates(string? configuredModel, params string[] fallbackModels)
    {
        var candidates = new List<string>();

        AppendModelVariants(candidates, configuredModel);
        foreach (var fallback in fallbackModels)
        {
            AppendModelVariants(candidates, fallback);
        }

        return candidates;
    }

    private static void AppendModelVariants(List<string> candidates, string? rawModel)
    {
        if (string.IsNullOrWhiteSpace(rawModel))
        {
            return;
        }

        var model = rawModel.Trim();
        AddModelCandidate(candidates, model);

        if (model.StartsWith("openai/", StringComparison.OrdinalIgnoreCase))
        {
            AddModelCandidate(candidates, model["openai/".Length..]);
        }
        else
        {
            AddModelCandidate(candidates, $"openai/{model}");
        }
    }

    private static void AddModelCandidate(List<string> candidates, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        if (candidates.Contains(model, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        candidates.Add(model.Trim());
    }

    private static bool IsUnknownModelError(int statusCode, string payload)
    {
        if (statusCode != 400 && statusCode != 404)
        {
            return false;
        }

        if (payload.Contains("unknown model", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("code", out var code) &&
                code.ValueKind == JsonValueKind.String &&
                string.Equals(code.GetString(), "unknown_model", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private static string BuildCompletionsUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "https://models.github.ai/inference/chat/completions";
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed}/chat/completions";
    }

    private static bool IsGitHubModelsEndpoint(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return true;
        }

        return baseUrl.Contains("models.github.ai", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractAssistantMessage(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return string.Empty;
        }

        return ReadContentValue(content);
    }

    private static string ReadContentValue(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var chunk in content.EnumerateArray())
            {
                if (chunk.ValueKind == JsonValueKind.String)
                {
                    sb.AppendLine(chunk.GetString());
                    continue;
                }

                if (chunk.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    sb.AppendLine(text.GetString());
                    continue;
                }

                if (chunk.TryGetProperty("content", out var nested) && nested.ValueKind == JsonValueKind.String)
                {
                    sb.AppendLine(nested.GetString());
                }
            }

            return sb.ToString().Trim();
        }

        return content.ToString();
    }

    private static T? TryParseModelJson<T>(string modelText)
    {
        if (string.IsNullOrWhiteSpace(modelText))
        {
            return default;
        }

        var maybeJson = ExtractJsonObject(modelText);
        try
        {
            return JsonSerializer.Deserialize<T>(maybeJson, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
            {
                trimmed = trimmed[(firstLineEnd + 1)..lastFence].Trim();
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }

        return trimmed;
    }

    private static string NormalizeReformulatedPrompt(string? rawPrompt)
    {
        if (string.IsNullOrWhiteSpace(rawPrompt))
        {
            return string.Empty;
        }

        var lines = rawPrompt
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select((line) => StripListPrefix(line.Trim()))
            .Where((line) => !string.IsNullOrWhiteSpace(line))
            .Where((line) => !StartsWithAny(line, "modelo", "model", "enfoque", "focus", "nota", "notes"))
            .Where((line) => !ContainsAny(line, "gpt", "openai", "nano", "mini"))
            .ToList();

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var merged = string.Join(" ", lines);
        return SanitizeInput(merged, 420);
    }

    private static string StripListPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var clean = value.Trim();
        while (clean.StartsWith("-", StringComparison.Ordinal) ||
               clean.StartsWith("*", StringComparison.Ordinal) ||
               clean.StartsWith("•", StringComparison.Ordinal))
        {
            clean = clean[1..].TrimStart();
        }

        var index = 0;
        while (index < clean.Length && char.IsDigit(clean[index]))
        {
            index++;
        }

        if (index > 0 && index < clean.Length &&
            (clean[index] == '.' || clean[index] == ')' || clean[index] == '-'))
        {
            clean = clean[(index + 1)..].TrimStart();
        }

        return clean;
    }

    private static bool StartsWithAny(string value, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildFallbackReformulatedPrompt(
        string safeUserInput,
        string safeOptionLabel,
        IReadOnlyList<string> safeAllergens)
    {
        var allergenRule = safeAllergens.Count == 0
            ? "Sin restricciones adicionales de alergenos."
            : $"Evitar estrictamente: {string.Join(", ", safeAllergens)}.";

        return $"Modo {safeOptionLabel}. {safeUserInput}. {allergenRule}";
    }

    private static List<string> BuildReformulationNotes(
        ReformulateModelOutput? parsed,
        IReadOnlyList<string> allergens)
    {
        var notes = NormalizeList(parsed?.Notes, 5)
            .Where((note) => !ContainsAny(note, "gpt", "openai", "modelo usado", "model used"))
            .ToList();
        if (!string.IsNullOrWhiteSpace(parsed?.Focus))
        {
            notes.Insert(0, $"Enfoque: {parsed.Focus.Trim()}");
        }

        if (allergens.Count > 0 && !notes.Any((note) => note.Contains("alerg", StringComparison.OrdinalIgnoreCase)))
        {
            notes.Add($"Se aplicaran restricciones por alergenos: {string.Join(", ", allergens)}.");
        }

        return notes;
    }

    private static IANutriRecipeSuggestion MapSuggestionRecipe(
        SuggestionsRecipeModelOutput recipe,
        IReadOnlyList<string> allergens)
    {
        var title = SanitizeInput(recipe.Title, 120);
        var description = SanitizeInput(recipe.Description, 360);
        var estimatedTime = SanitizeInput(recipe.EstimatedTime, 40);
        var difficulty = SanitizeInput(recipe.Difficulty, 30);

        var suggestion = new IANutriRecipeSuggestion
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Plato sugerido" : title,
            Description = string.IsNullOrWhiteSpace(description) ? "Sin descripcion adicional." : description,
            EstimatedTime = string.IsNullOrWhiteSpace(estimatedTime) ? "20-30 min" : estimatedTime,
            Difficulty = string.IsNullOrWhiteSpace(difficulty) ? "Media" : difficulty,
            Ingredients = NormalizeList(recipe.Ingredients, 20),
            Steps = NormalizeList(recipe.Steps, 20),
            AllergensDetected = NormalizeList(recipe.AllergensDetected, 8),
            SafeSubstitutions = NormalizeList(recipe.SafeSubstitutions, 8),
            AllergyWarning = SanitizeInput(recipe.AllergyWarning, 240)
        };

        if (suggestion.Ingredients.Count == 0)
        {
            suggestion.Ingredients.Add("Ajusta los ingredientes segun disponibilidad en casa.");
        }

        if (suggestion.Steps.Count == 0)
        {
            suggestion.Steps.Add("Prepara ingredientes, cocina y ajusta condimentos al gusto.");
        }

        ApplyAllergenConflicts(suggestion, allergens);
        return suggestion;
    }

    private static IANutriRecipeSuggestion BuildFallbackSuggestion(string rawModelText, IReadOnlyList<string> allergens)
    {
        var summary = SanitizeInput(rawModelText, 420);
        var recipe = new IANutriRecipeSuggestion
        {
            Title = "Sugerencia generada",
            Description = string.IsNullOrWhiteSpace(summary)
                ? "No se pudo estructurar la respuesta del modelo."
                : summary,
            EstimatedTime = "20-30 min",
            Difficulty = "Media",
            Ingredients = new List<string> { "Adapta los ingredientes segun disponibilidad." },
            Steps = new List<string>
            {
                "Define cantidades para 1-2 porciones.",
                "Cocina con fuego medio y revisa textura.",
                "Sirve y ajusta condimentos."
            },
            AllergensDetected = new List<string>(),
            SafeSubstitutions = new List<string>(),
            AllergyWarning = string.Empty
        };

        ApplyAllergenConflicts(recipe, allergens);
        if (allergens.Count > 0 && recipe.SafeSubstitutions.Count == 0)
        {
            recipe.SafeSubstitutions.Add("Usa ingredientes certificados sin alergenos.");
        }

        return recipe;
    }

    private static void ApplyAllergenConflicts(IANutriRecipeSuggestion recipe, IReadOnlyList<string> allergens)
    {
        if (allergens.Count == 0)
        {
            return;
        }

        var userAllergenSet = new HashSet<string>(allergens, StringComparer.OrdinalIgnoreCase);
        var conflicts = recipe.AllergensDetected
            .Where((allergen) => userAllergenSet.Contains(allergen))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (conflicts.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(recipe.AllergyWarning))
        {
            recipe.AllergyWarning = $"Atencion: incluye {string.Join(", ", conflicts)}, y esta en tus alergenos.";
        }

        if (recipe.SafeSubstitutions.Count == 0)
        {
            recipe.SafeSubstitutions.Add(
                $"Pide o prepara una version sin {string.Join(", ", conflicts)} con ingredientes alternativos seguros.");
        }
    }

    private static List<string> BuildFallbackRequiredItems(IReadOnlyList<string> ingredients)
    {
        var items = new List<string>
        {
            "Tabla de cortar",
            "Cuchillo",
            "Sarten u olla mediana",
            "Espatula o cuchara"
        };

        if (ingredients.Count > 0)
        {
            items.Add("Boles para preparar ingredientes.");
        }

        return items;
    }

    private static string FormatListForPrompt(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return "- none";
        }

        var sb = new StringBuilder();
        foreach (var value in values)
        {
            sb.Append("- ");
            sb.AppendLine(value);
        }

        return sb.ToString().TrimEnd();
    }

    private static List<string> NormalizeList(IEnumerable<string>? values, int maxItems)
    {
        var result = new List<string>();
        if (values is null)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var clean = SanitizeInput(value, 160);
            if (string.IsNullOrWhiteSpace(clean))
            {
                continue;
            }

            if (!seen.Add(clean))
            {
                continue;
            }

            result.Add(clean);
            if (result.Count >= maxItems)
            {
                break;
            }
        }

        return result;
    }

    private static string SanitizeInput(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength];
    }

    private static bool ContainsIgnoreCase(IEnumerable<string> values, string expected)
    {
        return values.Any((value) => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static string TrimForError(string payload)
    {
        var clean = SanitizeInput(payload, 280);
        return string.IsNullOrWhiteSpace(clean) ? "Respuesta vacia del proveedor." : clean;
    }

    private sealed class ReformulateModelOutput
    {
        public string ReformulatedPrompt { get; set; } = string.Empty;
        public string Focus { get; set; } = string.Empty;
        public List<string> Notes { get; set; } = new List<string>();
    }

    private sealed class SuggestionsModelOutput
    {
        public string Summary { get; set; } = string.Empty;
        public List<SuggestionsRecipeModelOutput> Recipes { get; set; } = new List<SuggestionsRecipeModelOutput>();
        public List<string> GlobalWarnings { get; set; } = new List<string>();
        public List<string> GeneralSubstitutions { get; set; } = new List<string>();
    }

    private sealed class SuggestionsRecipeModelOutput
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

    private sealed class CookingAssistantModelOutput
    {
        public string RecipeTitle { get; set; } = string.Empty;
        public string Intro { get; set; } = string.Empty;
        public List<string> RequiredItems { get; set; } = new List<string>();
        public List<string> Steps { get; set; } = new List<string>();
        public List<string> SafetyNotes { get; set; } = new List<string>();
    }
}
