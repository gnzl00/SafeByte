## 1. Que necesitas instalar

Obligatorio:
1. `.NET 8 SDK`
2. Proyecto Firebase con Firestore habilitado
3. Archivo JSON de Service Account de Firebase

Opcional:
1. `git` para clonar
2. VS Code / Visual Studio para desarrollo

## 2. Clonar y entrar al proyecto

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

## 4. Crear y preparar Firestore

1. Entra a Firebase Console.
2. Abre tu proyecto.
3. Ve a `Firestore Database`.
4. Crea la base de datos (ID por defecto: `(default)`).
5. Elige la region mas cercana a tus usuarios.

## 5. Descargar credenciales de backend (Service Account)

1. Firebase Console -> `Project Settings`.
2. Pestaña `Service Accounts`.
3. Pulsa `Generate new private key`.
4. Descarga el `.json`.

## 6. Guardar el JSON en el proyecto

1. Crea la carpeta `secrets` si no existe.
2. Mueve el archivo descargado a:
- `secrets/service-account.json`

Importante:
- Este archivo **no** se sube a git.
- Ya esta ignorado por `.gitignore`.

## 7. Configurar `appsettings`

Archivo: `appsettings.json`
```json
{
  "Firestore": {
    "ProjectId": "tu-project-id"
  }
}
```

Archivo: `appsettings.Development.json`
```json
{
  "Firestore": {
    "CredentialsPath": "secrets/service-account.json",
    "SeedOnStartup": true
  }
}
```

Notas:
- `CredentialsPath` puede ser relativa al repo o absoluta.
- Si no quieres usar `CredentialsPath`, exporta `GOOGLE_APPLICATION_CREDENTIALS`.

## 8. Restaurar y ejecutar

```bash
dotnet restore
dotnet run
```

Urls por defecto (segun `launchSettings.json`):
- `http://localhost:5113`
- `https://localhost:7113`

## 9. Comprobar que todo funciona

1. Abre `http://localhost:5113`.
2. Registra un usuario nuevo.
3. Inicia sesion con ese usuario.
4. En Firestore revisa la coleccion `users`:
- Debe existir el documento con ID = email en minusculas.
- Debe tener campos `username`, `email`, `passwordHash`, `createdAt`, `allergens`, `allergensUpdatedAt`.

## 10. Problemas tipicos y solucion

Error: `Firestore ProjectId is not configured`
- Solucion: rellena `Firestore:ProjectId` en `appsettings.json`.

Error: `credentials file not found`
- Solucion: revisa la ruta de `Firestore:CredentialsPath`.

Error al iniciar por HTTPS local
- Solucion:
```bash
dotnet dev-certs https --trust
```

No aparecen usuarios en Firestore tras registrar
- Solucion:
1. Verifica que Firestore esta creado en Firebase.
2. Verifica que el backend arranco sin excepciones.
3. Revisa que usas el mismo `ProjectId` que en Firebase.
