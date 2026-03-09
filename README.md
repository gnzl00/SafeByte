# SafeByte

Aplicacion web ASP.NET Core MVC + API para recomendaciones de comidas seguras segun alergenos.

## Stack real

- Backend: ASP.NET Core 8 (`net8.0`, C#).
- Persistencia: Firebase Firestore (`Google.Cloud.Firestore`).
- Frontend: Razor + JavaScript en `wwwroot/src/ventanas`.
- IA (por defecto): GitHub Models.
- `package.json`: scripts opcionales para ejecutar comandos `dotnet`; no es el runtime principal.

## Estado funcional actual

- El backend no usa archivo local de Firebase (`secrets/service-account.json`).
- Firebase se inicializa por `FIREBASE_CREDENTIALS` (JSON completo) o por ADC.
- El servicio escucha puerto cloud dinamico por `PORT` en `0.0.0.0:{PORT}`.
- CORS en produccion se controla por `CORS_ALLOWED_ORIGINS`.
- El login es obligatorio (se elimino el boton `Skip Login`).

## Variables de entorno

Requeridas:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
- `CORS_ALLOWED_ORIGINS`

Compatibilidad:

- `IANUTRI_API_KEY`
- `OPENAI_API_KEY` (solo si cambias `IANutri:BaseUrl` a OpenAI)

## Ejecucion local

```bash
dotnet restore
dotnet build
dotnet run
```

Si en local aparece `Your default credentials were not found`, define `FIREBASE_CREDENTIALS` antes de ejecutar.

PowerShell:

```powershell
$env:FIRESTORE__PROJECTID = "fooddna-b91c1"
$env:FIREBASE_CREDENTIALS = (Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
$env:GITHUB_MODELS_API_KEY = "tu_key"
$env:CORS_ALLOWED_ORIGINS = "http://localhost:5188"
dotnet run
```

URL local por perfil de lanzamiento:

- `http://localhost:5188`

## Despliegue rapido en Render

1. Asegura `Dockerfile` en la raiz del repo.
2. Crea `Web Service` con runtime `Docker`.
3. Configura:
- `Health Check Path`: `/`
- `Docker Build Context Directory`: `.`
- `Dockerfile Path`: `./Dockerfile`
4. Carga variables de entorno listadas arriba.
5. Deploy.

## Scripts npm opcionales

```bash
npm run dev
npm run build
npm run start
npm run publish
```

## Documentacion

- Indice: [docs/00-indice.md](docs/00-indice.md)
- Setup local/cloud: [docs/01-setup.md](docs/01-setup.md)
- Estructura y flujos: [docs/02-estructura-y-flujos.md](docs/02-estructura-y-flujos.md)
- Persistencia de alergenos: [docs/03-alergenos-mvc-y-persistencia.md](docs/03-alergenos-mvc-y-persistencia.md)
- IANutri: [docs/04-ianutri-arquitectura-y-flujo-e2e.md](docs/04-ianutri-arquitectura-y-flujo-e2e.md)
- Pendientes manuales: [docs/05-pendientes-despliegue-cloud.md](docs/05-pendientes-despliegue-cloud.md)
- Checklist Render: [docs/06-render-checklist.md](docs/06-render-checklist.md)
