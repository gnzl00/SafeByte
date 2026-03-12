using System.Globalization;
using System.Text;

namespace SafeByte.Services;

public static class AllergenCatalog
{
    private static readonly Dictionary<string, string> CanonicalByKey = new(StringComparer.Ordinal)
    {
        ["gluten"] = "Gluten",
        ["lacteos"] = "Lácteos",
        ["huevo"] = "Huevo",
        ["frutos secos"] = "Frutos secos",
        ["mariscos"] = "Mariscos",
        ["soja"] = "Soja"
    };

    public static IReadOnlyList<string> Allowed { get; } = new[]
    {
        "Gluten",
        "Lácteos",
        "Huevo",
        "Frutos secos",
        "Mariscos",
        "Soja"
    };

    public static bool TryNormalize(string? allergen, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(allergen))
        {
            return false;
        }

        var key = NormalizeKey(allergen);
        return CanonicalByKey.TryGetValue(key, out canonical!);
    }

    public static bool TryNormalizeKey(string? allergen, out string normalizedKey)
    {
        normalizedKey = string.Empty;
        if (string.IsNullOrWhiteSpace(allergen))
        {
            return false;
        }

        var key = NormalizeKey(allergen);
        if (!CanonicalByKey.ContainsKey(key))
        {
            return false;
        }

        normalizedKey = key;
        return true;
    }

    public static List<string> NormalizeMany(
        IEnumerable<string>? input,
        out List<string> invalidAllergens)
    {
        invalidAllergens = new List<string>();
        var result = new List<string>();

        if (input is null)
        {
            return result;
        }

        foreach (var allergen in input)
        {
            if (TryNormalize(allergen, out var canonical))
            {
                if (!result.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(canonical);
                }
            }
            else if (!string.IsNullOrWhiteSpace(allergen))
            {
                invalidAllergens.Add(allergen.Trim());
            }
        }

        return result;
    }

    public static List<string> NormalizeManyKeys(
        IEnumerable<string>? input,
        out List<string> invalidAllergens)
    {
        invalidAllergens = new List<string>();
        var result = new List<string>();

        if (input is null)
        {
            return result;
        }

        foreach (var allergen in input)
        {
            if (TryNormalizeKey(allergen, out var normalizedKey))
            {
                if (!result.Contains(normalizedKey, StringComparer.Ordinal))
                {
                    result.Add(normalizedKey);
                }
            }
            else if (!string.IsNullOrWhiteSpace(allergen))
            {
                invalidAllergens.Add(allergen.Trim());
            }
        }

        return result;
    }

    private static string NormalizeKey(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var formD = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
        }

        return string.Join(
            " ",
            sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
