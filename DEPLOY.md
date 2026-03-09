# DEPLOY - Backend ASP.NET Core 8

Guia de despliegue del backend SafeByte en proveedores cloud (Render, Railway, Azure).

## 1) Variables de entorno

Minimas requeridas:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
- `CORS_ALLOWED_ORIGINS`

Opcionales:

- `PORT` (inyectada por proveedor)
- `IANUTRI_API_KEY`
- `OPENAI_API_KEY` (solo si cambias `IANutri:BaseUrl` a OpenAI)

## 2) Formato de FIREBASE_CREDENTIALS

Debe ser el JSON completo de service-account en una sola linea, con `\\n` en `private_key`.

Ejemplo de conversion en PowerShell:

```powershell
(Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
```

## 3) Docker (local)

```bash
docker build -t safebyte-backend .
docker run -p 8080:8080 \
  -e FIRESTORE__PROJECTID="your-project-id" \
  -e FIREBASE_CREDENTIALS='{"type":"service_account",...}' \
  -e GITHUB_MODELS_API_KEY="your-key" \
  -e CORS_ALLOWED_ORIGINS="http://localhost:5188" \
  safebyte-backend
```

## 4) Render (recomendado para pruebas gratis)

Configuracion recomendada del servicio:

- Runtime: `Docker`
- `Health Check Path`: `/`
- `Docker Build Context Directory`: `.`
- `Dockerfile Path`: `./Dockerfile`
- `Auto-Deploy`: `On Commit`

Variables en Render:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS`

Notas:

- En free tier puede haber cold starts tras inactividad.
- El dominio inicial sera `https://<service-name>.onrender.com`.
- Si no tienes dominio propio, usa ese dominio en `CORS_ALLOWED_ORIGINS`.

## 5) Railway / Azure

- Railway: mismo set de variables; Railway inyecta `PORT`.
- Azure App Service: mismo set de variables en App Settings; no subir JSON al repo.

## 6) Seguridad basica

- No subir `.env`, `secrets/`, ni `service-account*.json`.
- Guardar secretos solo en entorno del proveedor.
- Rotar claves expuestas.
- Restringir CORS a orígenes reales de frontend.

## 7) Verificacion post-deploy

- `POST /api/Auth/Register`
- `POST /api/Auth/Login`
- `GET /api/Allergens/User`
- `PUT /api/Allergens/User`
- `POST /api/IANutri/Reformulate`
- `POST /api/IANutri/GenerateSuggestions`
