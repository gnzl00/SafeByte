using Google.Cloud.Firestore;
using System.IO;
using SafeByte.Data;
using SafeByte.Services;

LoadDotEnvIfExists(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configurar Firestore
var firestoreProjectId =
    builder.Configuration["Firestore:ProjectId"] ??
    Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ??
    Environment.GetEnvironmentVariable("GCLOUD_PROJECT");
if (string.IsNullOrWhiteSpace(firestoreProjectId))
{
    throw new InvalidOperationException("Firestore ProjectId is not configured. Set Firestore:ProjectId in appsettings.json.");
}
builder.Services.AddSingleton(_ =>
{
    var credentialsPath = builder.Configuration["Firestore:CredentialsPath"];
    if (!string.IsNullOrWhiteSpace(credentialsPath))
    {
        var fullPath = Path.IsPathRooted(credentialsPath)
            ? credentialsPath
            : Path.Combine(builder.Environment.ContentRootPath, credentialsPath);

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Firestore credentials file not found at '{fullPath}'. " +
                "Place the service account JSON there or set GOOGLE_APPLICATION_CREDENTIALS.");
        }

        var dbBuilder = new FirestoreDbBuilder
        {
            ProjectId = firestoreProjectId,
            CredentialsPath = fullPath
        };
        return dbBuilder.Build();
    }

    return FirestoreDb.Create(firestoreProjectId);
});

// 🔹 Habilitar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 🔹 MVC + API controllers
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");
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

static void LoadDotEnvIfExists(string envFilePath)
{
    if (!File.Exists(envFilePath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(envFilePath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            continue;
        }

        var value = line[(separatorIndex + 1)..].Trim();
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
