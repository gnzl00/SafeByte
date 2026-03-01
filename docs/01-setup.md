# 01 - Setup

## 1. Requisitos

Obligatorio:
1. `.NET 8 SDK`
2. Proyecto Firebase con Firestore habilitado
3. JSON de Service Account de Firebase

Opcional:
1. `git`
2. VS Code o Visual Studio

## 2. Clonar y entrar al repo

```bash
git clone <url-del-repo>
cd SafeByte
```

## 3. Instalar .NET 8

Windows (PowerShell):
```powershell
winget install Microsoft.DotNet.SDK.8
dotnet --version
```

macOS (Homebrew):
```bash
brew update
brew install dotnet@8
dotnet --version
```

Linux (Ubuntu/Debian):
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
dotnet --version
```

## 4. Configurar Firestore

1. Firebase Console -> proyecto
2. `Firestore Database` -> crear base
3. Crear/descargar Service Account key

## 5. Guardar credenciales

1. Crear carpeta `secrets` si no existe.
2. Guardar archivo en:
- `secrets/service-account.json`

Importante:
1. No subir este archivo a git.
2. Alternativa: usar variable `GOOGLE_APPLICATION_CREDENTIALS`.

## 6. Configurar `appsettings`

`appsettings.json`:
```json
{
  "Firestore": {
    "ProjectId": "tu-project-id"
  }
}
```

`appsettings.Development.json`:
```json
{
  "Firestore": {
    "CredentialsPath": "secrets/service-account.json",
    "SeedOnStartup": false
  }
}
```

## 7. Configurar IANutri

`appsettings.json` o `appsettings.Development.json`:
```json
{
  "IANutri": {
    "BaseUrl": "https://models.inference.ai.azure.com",
    "ApiKey": "",
    "ReformulationModel": "gpt-4.1-nano",
    "SuggestionModel": "gpt-4.1",
    "CookingAssistantModel": "gpt-4.1",
    "TimeoutSeconds": 60
  }
}
```

Variables de entorno soportadas para API key:
1. `IANUTRI_API_KEY`
2. `GITHUB_MODELS_API_KEY`
3. `GITHUB_TOKEN`
4. `OPENAI_API_KEY`

## 8. Restaurar y ejecutar

```bash
dotnet restore
dotnet run
```

URL local por defecto (`launchSettings.json`):
1. `http://localhost:5188`

## 9. Verificacion minima

1. Abrir app en navegador.
2. Registrar usuario e iniciar sesion.
3. Guardar alergenos y confirmar en Firestore.
4. Abrir `IANutri` y generar una sugerencia.

## 10. Problemas comunes

Error: `Firestore ProjectId is not configured`
1. Revisar `Firestore:ProjectId`.

Error: `credentials file not found`
1. Revisar `Firestore:CredentialsPath`.

Error: `No se encontro API key para IANutri`
1. Configurar `IANutri:ApiKey` o variable de entorno.

Error: `address already in use`
1. Cerrar proceso previo o cambiar puerto.
