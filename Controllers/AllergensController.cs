using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using SafeByte.Models;
using SafeByte.Services;

namespace SafeByte.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AllergensController : ControllerBase
{
    private readonly CollectionReference _users;

    public AllergensController(FirestoreDb firestoreDb)
    {
        _users = firestoreDb.Collection("users");
    }

    [HttpGet("Catalog")]
    public IActionResult GetCatalog()
    {
        return Ok(new
        {
            Allergens = AllergenCatalog.Allowed
        });
    }

    [HttpGet("User")]
    public async Task<IActionResult> GetUserAllergens([FromQuery] string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest("Email inválido.");
        }

        var docRef = _users.Document(normalizedEmail);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return NotFound("Usuario no encontrado.");
        }

        var allergens = ExtractAndNormalizeAllergens(snapshot);
        var allergenKeys = AllergenCatalog.NormalizeManyKeys(allergens, out _);
        var updatedAt = snapshot.TryGetValue<Timestamp>("allergensUpdatedAt", out var timestamp)
            ? timestamp.ToDateTime().ToUniversalTime()
            : (DateTime?)null;

        return Ok(new UserAllergenPreferencesResponse
        {
            Email = normalizedEmail,
            Allergens = allergens,
            AllergenKeys = allergenKeys,
            UpdatedAtUtc = updatedAt
        });
    }

    [HttpPut("User")]
    public async Task<IActionResult> SaveUserAllergens([FromBody] UpdateUserAllergensRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest("Email inválido.");
        }

        var normalizedAllergens = AllergenCatalog.NormalizeMany(
            request.Allergens,
            out var invalidAllergens);
        if (invalidAllergens.Count > 0)
        {
            return BadRequest(new
            {
                Message = "Se enviaron alérgenos no válidos.",
                InvalidAllergens = invalidAllergens,
                AllowedAllergens = AllergenCatalog.Allowed
            });
        }

        var docRef = _users.Document(normalizedEmail);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return NotFound("Usuario no encontrado.");
        }

        var now = Timestamp.GetCurrentTimestamp();
        var updates = new Dictionary<string, object>
        {
            ["allergens"] = normalizedAllergens,
            ["allergensUpdatedAt"] = now
        };
        await docRef.UpdateAsync(updates);

        var allergenKeys = AllergenCatalog.NormalizeManyKeys(normalizedAllergens, out _);
        return Ok(new UserAllergenPreferencesResponse
        {
            Email = normalizedEmail,
            Allergens = normalizedAllergens,
            AllergenKeys = allergenKeys,
            UpdatedAtUtc = now.ToDateTime().ToUniversalTime()
        });
    }

    private static List<string> ExtractAndNormalizeAllergens(DocumentSnapshot snapshot)
    {
        if (!snapshot.TryGetValue<List<string>>("allergens", out var rawAllergens))
        {
            return new List<string>();
        }

        return AllergenCatalog.NormalizeMany(rawAllergens, out _);
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
