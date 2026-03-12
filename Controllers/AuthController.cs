using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using SafeByte.Models;
using SafeByte.Services;

namespace SafeByte.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CollectionReference _users;

    public AuthController(FirestoreDb firestoreDb)
    {
        _users = firestoreDb.Collection("users");
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] User newUser)
    {
        var email = NormalizeEmail(newUser.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email inválido.");
        }

        var docRef = _users.Document(email);
        var snapshot = await docRef.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            return BadRequest("El usuario con ese Email ya existe.");
        }

        var passwordHash = PasswordHasher.HashPassword(newUser.Password);
        var now = Timestamp.GetCurrentTimestamp();

        var userData = new Dictionary<string, object>
        {
            ["username"] = newUser.Username,
            ["email"] = email,
            ["passwordHash"] = passwordHash,
            ["createdAt"] = now,
            ["allergens"] = new List<string>(),
            ["allergensUpdatedAt"] = now
        };

        await docRef.SetAsync(userData);

        return Ok(new
        {
            Message = "Usuario registrado con éxito.",
            User = new
            {
                Username = newUser.Username,
                Email = email,
                Allergens = Array.Empty<string>(),
                AllergenKeys = Array.Empty<string>()
            }
        });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] User loginData)
    {
        var email = NormalizeEmail(loginData.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Credenciales inválidas (usuario no encontrado).");
        }

        var docRef = _users.Document(email);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return BadRequest("Credenciales inválidas (usuario no encontrado).");
        }

        var hashedInputPassword = PasswordHasher.HashPassword(loginData.Password);
        var storedPasswordHash = snapshot.GetValue<string>("passwordHash");
        if (storedPasswordHash != hashedInputPassword)
        {
            return BadRequest("Credenciales inválidas (contraseña incorrecta).");
        }

        var username = snapshot.ContainsField("username")
            ? snapshot.GetValue<string>("username")
            : email;
        var allergens = GetNormalizedAllergens(snapshot);
        var allergenKeys = AllergenCatalog.NormalizeManyKeys(allergens, out _);

        return Ok(new
        {
            Message = "Login correcto",
            User = new
            {
                Username = username,
                Email = email,
                Allergens = allergens,
                AllergenKeys = allergenKeys
            }
        });
    }

    private static List<string> GetNormalizedAllergens(DocumentSnapshot snapshot)
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
