using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.HttpOverrides;
using SafeByte.Data;
using SafeByte.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var portFromEnvironment = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(portFromEnvironment, out var port) && port > 0)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var firestoreProjectId =
    builder.Configuration["Firestore:ProjectId"] ??
    Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ??
    Environment.GetEnvironmentVariable("GCLOUD_PROJECT");

if (string.IsNullOrWhiteSpace(firestoreProjectId))
{
    throw new InvalidOperationException(
        "Firestore ProjectId is not configured. Set Firestore:ProjectId or GOOGLE_CLOUD_PROJECT.");
}

builder.Services.AddSingleton(_ =>
{
    var firebaseCredentialsJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
    if (!string.IsNullOrWhiteSpace(firebaseCredentialsJson))
    {
        try
        {
            using var document = JsonDocument.Parse(firebaseCredentialsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("FIREBASE_CREDENTIALS must be a JSON object.");
            }

            var credential = GoogleCredential.FromJson(firebaseCredentialsJson);
            var dbBuilder = new FirestoreDbBuilder
            {
                ProjectId = firestoreProjectId,
                Credential = credential
            };
            return dbBuilder.Build();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "FIREBASE_CREDENTIALS is not valid JSON. Set the full Firebase service-account JSON string.",
                ex);
        }
    }

    // Cloud-native fallback: managed identity/workload identity via ADC.
    try
    {
        return FirestoreDb.Create(firestoreProjectId);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "No se pudieron inicializar credenciales de Firestore. " +
            "Configura FIREBASE_CREDENTIALS (JSON completo) o ADC local con " +
            "'gcloud auth application-default login'.",
            ex);
    }
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        var corsOriginsRaw = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        var corsOrigins = corsOriginsRaw?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

builder.Services.AddControllersWithViews();
builder.Services.Configure<IANutriOptions>(builder.Configuration.GetSection("IANutri"));
builder.Services.AddHttpClient<IIANutriService, IANutriService>();

var app = builder.Build();

try
{
    await SeedFirestoreAsync(app.Services, app.Configuration, app.Environment);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogWarning(ex, "Firestore seed failed on startup. Continuing without seed.");
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();
app.UseCors("AppCors");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();

static async Task SeedFirestoreAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
{
    if (!environment.IsDevelopment())
    {
        return;
    }

    var seedOnStartup = configuration.GetValue<bool>("Firestore:SeedOnStartup");
    if (!seedOnStartup)
    {
        return;
    }

    using var scope = services.CreateScope();
    var firestore = scope.ServiceProvider.GetRequiredService<FirestoreDb>();
    var users = FileDatabase.LoadUsers();

    foreach (var user in users)
    {
        var email = NormalizeEmail(user.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            continue;
        }

        var docRef = firestore.Collection("users").Document(email);
        var snapshot = await docRef.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            continue;
        }

        var data = new Dictionary<string, object>
        {
            ["username"] = user.Username,
            ["email"] = email,
            ["passwordHash"] = PasswordHasher.HashPassword(user.Password),
            ["createdAt"] = Timestamp.GetCurrentTimestamp(),
            ["seeded"] = true,
            ["allergens"] = new List<string>(),
            ["allergensUpdatedAt"] = Timestamp.GetCurrentTimestamp()
        };

        await docRef.SetAsync(data);
    }
}

static string NormalizeEmail(string? email)
{
    return email?.Trim().ToLowerInvariant() ?? string.Empty;
}
