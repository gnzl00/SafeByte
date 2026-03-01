# SafeByte

Aplicacion web ASP.NET Core MVC + API para recomendar comidas seguras segun alergenos del usuario.

## Estado actual (resumen rapido)
- Backend: ASP.NET Core 8.
- Base de datos: Firebase Firestore (no MySQL/XAMPP).
- Frontend: Razor + JavaScript plano en `wwwroot/src/ventanas`.
- Login/registro: API en `AuthController`.
- Alergenos por usuario: API en `AllergensController`, persistidos en Firestore.
- IANutri: reformulacion + sugerencias + asistente de cocina + historial persistente en Firestore.

## Cambio de arquitectura (antes vs ahora)
- Antes: almacenamiento local temporal en navegador para alergenos.
- Ahora: alergenos guardados en Firestore por usuario (`users/{email}`), con lectura y escritura via API.
- Resultado: las preferencias no se pierden al cerrar sesion o cambiar de dispositivo (si el usuario inicia sesion con su cuenta).

## Ejecucion rapida
1. Instala `.NET 8 SDK`.
2. Configura Firestore y credenciales (ver `docs/01-setup.md`).
3. Ejecuta:
```bash
dotnet restore
dotnet run
```
4. Abre:
- `http://localhost:5188`

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

## Configuracion IANutri (GPT)
Configura la API key en `appsettings.*.json` o con variable de entorno.

`appsettings.json`:
```json
"IANutri": {
  "BaseUrl": "https://models.inference.ai.azure.com",
  "ApiKey": "",
  "ReformulationModel": "gpt-4.1-nano",
  "SuggestionModel": "gpt-4.1",
  "CookingAssistantModel": "gpt-4.1",
  "TimeoutSeconds": 60
}
```

Variables de entorno soportadas (fallback):
- `IANUTRI_API_KEY`
- `GITHUB_MODELS_API_KEY`
- `GITHUB_TOKEN`
- `OPENAI_API_KEY`

Recomendacion:
- No commitear API keys reales en el repositorio.

## Documentacion detallada
- Indice general: [docs/00-indice.md](docs/00-indice.md)
- Setup completo: [docs/01-setup.md](docs/01-setup.md)
- Estructura y flujos: [docs/02-estructura-y-flujos.md](docs/02-estructura-y-flujos.md)
- MVC de alergenos y persistencia: [docs/03-alergenos-mvc-y-persistencia.md](docs/03-alergenos-mvc-y-persistencia.md)
- IANutri (documentacion unificada): [docs/04-ianutri-arquitectura-y-flujo-e2e.md](docs/04-ianutri-arquitectura-y-flujo-e2e.md)
