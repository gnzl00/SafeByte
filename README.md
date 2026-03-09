# SafeByte

Aplicacion web ASP.NET Core MVC + API para recomendar comidas seguras segun alergenos del usuario.

## Stack real del backend

- Backend: ASP.NET Core 8 (`net8.0`, C#).
- Persistencia: Firebase Firestore (`Google.Cloud.Firestore`).
- Frontend: Razor + JavaScript en `wwwroot/src/ventanas`.
- `package.json`: utilitario opcional para lanzar comandos `dotnet` desde npm. No es el runtime principal del backend.

## Configuracion segura (produccion)

Variables de entorno requeridas:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS` (JSON completo de Service Account en una sola variable)
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN` (prioritaria para IANutri)
- `CORS_ALLOWED_ORIGINS` (recomendado en produccion, separado por comas)

Variables compatibles secundarias para API key:

- `IANUTRI_API_KEY`
- `OPENAI_API_KEY` (si cambias endpoint a OpenAI)

Notas:

- El backend ya no lee credenciales Firebase desde `secrets/service-account.json`.
- El backend soporta puerto dinamico cloud con `PORT` y escucha en `0.0.0.0:{PORT}`.
- En produccion, CORS se controla por `CORS_ALLOWED_ORIGINS`; en desarrollo se permite `AllowAnyOrigin`.

## Ejecucion local

```bash
dotnet restore
dotnet build
dotnet run
```

URL local de desarrollo (por `launchSettings.json`):

- `http://localhost:5188`

## Scripts npm opcionales

```bash
npm run dev
npm run build
npm run start
npm run publish
```

Estos scripts solo envuelven comandos `dotnet`.

## Archivos de despliegue incluidos

- `Dockerfile`
- `render.yaml`
- `DEPLOY.md`
- `.env.example`

## Endpoints principales

- `POST /api/Auth/Register`
- `POST /api/Auth/Login`
- `GET /api/Allergens/Catalog`
- `GET /api/Allergens/User?email=usuario@dominio.com`
- `PUT /api/Allergens/User`
- `POST /api/IANutri/Reformulate`
- `POST /api/IANutri/GenerateSuggestions`
- `POST /api/IANutri/CookingAssistant`
- `GET /api/IANutri/History?email=usuario@dominio.com`
- `DELETE /api/IANutri/History?email=usuario@dominio.com`

## Documentacion

- Indice: [docs/00-indice.md](docs/00-indice.md)
- Setup: [docs/01-setup.md](docs/01-setup.md)
- Estructura y flujos: [docs/02-estructura-y-flujos.md](docs/02-estructura-y-flujos.md)
- Persistencia alergenos: [docs/03-alergenos-mvc-y-persistencia.md](docs/03-alergenos-mvc-y-persistencia.md)
- IANutri: [docs/04-ianutri-arquitectura-y-flujo-e2e.md](docs/04-ianutri-arquitectura-y-flujo-e2e.md)
