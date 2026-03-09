# 01 - Setup

## 1. Requisitos

Obligatorio:
1. `.NET 8 SDK`
2. Proyecto Firebase con Firestore habilitado
3. API key para IANutri (`GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`)

Opcional:
1. `git`
2. VS Code o Visual Studio

## 2. Clonar y entrar al repo

```bash
git clone <url-del-repo>
cd SafeByte
```

## 3. Configurar variables de entorno

1. Copia `.env.example` a `.env` solo para local (no lo subas a git).
2. Configura como minimo:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS` (JSON completo de Service Account)
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS` (en produccion, ej. `https://tu-frontend.com`)

Ejemplo de `FIREBASE_CREDENTIALS`:

```text
{"type":"service_account","project_id":"tu-project-id","private_key_id":"...","private_key":"-----BEGIN PRIVATE KEY-----\\n...\\n-----END PRIVATE KEY-----\\n","client_email":"...","client_id":"...","token_uri":"https://oauth2.googleapis.com/token"}
```

## 4. Configurar appsettings

`appsettings.json`:

```json
{
  "Firestore": {
    "ProjectId": "tu-project-id"
  },
  "IANutri": {
    "BaseUrl": "https://models.github.ai/inference",
    "ApiKey": "${GITHUB_MODELS_API_KEY}",
    "ReformulationModel": "gpt-4.1-nano",
    "SuggestionModel": "gpt-4.1",
    "CookingAssistantModel": "gpt-4.1",
    "TimeoutSeconds": 60
  }
}
```

## 5. Restaurar y ejecutar

```bash
dotnet restore
dotnet build
dotnet run
```

Si te aparece `Your default credentials were not found`, define `FIREBASE_CREDENTIALS` antes de `dotnet run`.

PowerShell (ejemplo):

```powershell
$env:FIREBASE_CREDENTIALS = (Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
dotnet run
```

URL local de desarrollo (launch profile):
1. `http://localhost:5188`

## 6. Verificacion minima

1. Abrir app en navegador.
2. Registrar usuario e iniciar sesion.
3. Guardar alergenos y confirmar en Firestore.
4. Abrir `IANutri` y generar sugerencia.

## 7. Troubleshooting

Error: `Firestore ProjectId is not configured`
1. Revisar `Firestore:ProjectId` o `FIRESTORE__PROJECTID`.

Error: `FIREBASE_CREDENTIALS is not valid JSON`
1. Revisar formato JSON completo y escapes (`\\n`) en `private_key`.

Error: `Your default credentials were not found`
1. Definir `FIREBASE_CREDENTIALS` o configurar ADC con:
2. `gcloud auth application-default login`

Error: `No se encontro API key para IANutri`
1. Configurar `GITHUB_MODELS_API_KEY`/`GITHUB_TOKEN` o `IANutri:ApiKey`.

Error: `address already in use`
1. Cerrar proceso previo o cambiar puerto local.

Cloud:
1. El backend soporta `PORT` dinamico en `0.0.0.0:{PORT}`.
