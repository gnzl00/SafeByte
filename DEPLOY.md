# DEPLOY - SafeByte backend ASP.NET Core 8

Fecha de referencia: 2026-03-09.

Este documento describe como desplegar SafeByte en cloud de forma reproducible, segura y alineada con la arquitectura actual del proyecto.

## 1. Arquitectura de despliegue

SafeByte se despliega como una aplicacion web unica (backend MVC + API):

1. Runtime: contenedor Docker con `dotnet` ASP.NET 8.
2. Entrada HTTP: puerto inyectado por proveedor (`PORT`) o `8080` por defecto.
3. Persistencia externa: Firestore.
4. Proveedor IA externo: GitHub Models (por defecto) u OpenAI compatible.

No hay base de datos local en contenedor ni secretos en repo.

## 2. Variables de entorno

## 2.1 Requeridas

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
- `CORS_ALLOWED_ORIGINS`

## 2.2 Opcionales

- `PORT` (la inyecta el proveedor normalmente)
- `ASPNETCORE_ENVIRONMENT` (recomendado `Production`)
- `IANUTRI_API_KEY` (compatibilidad)
- `OPENAI_API_KEY` (si usas base URL OpenAI)

## 2.3 Orden de resolucion de API key IA

`IANutriService` resuelve key de forma distinta segun endpoint configurado:

1. Si `IANutri:ApiKey` viene definido y valido, se usa primero.
2. Si endpoint es GitHub Models:
- `GITHUB_MODELS_API_KEY`
- `GITHUB_TOKEN`
- `IANUTRI_API_KEY`
- `OPENAI_API_KEY`
3. Si endpoint es OpenAI:
- `OPENAI_API_KEY`
- `IANUTRI_API_KEY`
- `GITHUB_MODELS_API_KEY`
- `GITHUB_TOKEN`

Esto permite failover operacional sin recompilar.

## 3. FIREBASE_CREDENTIALS correcto

`FIREBASE_CREDENTIALS` debe contener el JSON completo de service account en una sola linea.

Ejemplo (PowerShell):

```powershell
(Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
```

Recomendaciones:

1. No pegar comillas adicionales alrededor de todo el JSON.
2. Mantener `\\n` en `private_key`.
3. No versionar ese JSON en git.

## 4. Docker local (prueba previa al cloud)

```bash
docker build -t safebyte-backend .
docker run -p 8080:8080 \
  -e FIRESTORE__PROJECTID="your-project-id" \
  -e FIREBASE_CREDENTIALS='{"type":"service_account",...}' \
  -e GITHUB_MODELS_API_KEY="your-key" \
  -e CORS_ALLOWED_ORIGINS="http://localhost:5188" \
  safebyte-backend
```

Validar en `http://localhost:8080/`.

## 5. Render (recomendado para demo)

## 5.1 Configuracion base

1. Tipo: `Web Service`.
2. Runtime: `Docker`.
3. `Docker Build Context Directory`: `.`
4. `Dockerfile Path`: `./Dockerfile`
5. `Health Check Path`: `/`
6. `Auto-Deploy`: segun politica de equipo.

## 5.2 Variables en Render

Cargar como secret env vars:

- `ASPNETCORE_ENVIRONMENT=Production`
- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS`

## 5.3 CORS recomendado

Durante pruebas:

```text
https://<service-name>.onrender.com,http://localhost:5188
```

En produccion real, dejar solo dominios reales de frontend.

## 6. Railway y Azure

## 6.1 Railway

1. Deploy por Docker o deteccion .NET.
2. Configurar mismas env vars.
3. Railway inyecta `PORT` automaticamente.

## 6.2 Azure App Service

1. Deploy por contenedor o publish.
2. Definir variables en App Settings.
3. Habilitar HTTPS only.
4. Restringir CORS en portal y en variable.

## 7. Verificacion post deploy (smoke test)

Ejecucion minima:

1. `GET /` -> debe responder vista de login.
2. `POST /api/Auth/Register`.
3. `POST /api/Auth/Login`.
4. `GET /api/Allergens/User?email=...`.
5. `PUT /api/Allergens/User`.
6. `POST /api/IANutri/Reformulate`.
7. `POST /api/IANutri/GenerateSuggestions`.
8. `POST /api/IANutri/CookingAssistant`.

## 8. Troubleshooting operativo

## 8.1 `No se encontro API key para IANutri`

- Revisar keys IA cargadas en proveedor.
- Verificar que no hay typo en nombres de variables.

## 8.2 `Your default credentials were not found`

- En cloud: revisar `FIREBASE_CREDENTIALS`.
- En local: definir `FIREBASE_CREDENTIALS` o usar ADC (`gcloud auth application-default login`).

## 8.3 `unknown_model`

- Verificar modelos permitidos por tu key.
- El servicio ya intenta variantes con y sin prefijo `openai/`.

## 8.4 `failed to read dockerfile`

- Revisar ruta `Dockerfile Path` y branch desplegada.

## 8.5 freeze o consumo alto en scanner movil

- Asegurar version actual de Home con:
- `decodeOnceFromVideoDevice` (no loop infinito)
- timeout de lectura
- `stopScanner()` en `visibilitychange` y `beforeunload`

## 9. Seguridad minima obligatoria

1. Rotar cualquier secreto potencialmente expuesto.
2. No subir `.env`, `service-account*.json` ni claves privadas.
3. Limitar `CORS_ALLOWED_ORIGINS` a dominios necesarios.
4. Revisar permisos IAM de service account con principio de minimo privilegio.

## 10. Endurecimiento recomendado (siguiente fase)

1. Migrar auth a JWT/Firebase Auth.
2. Migrar hash de password a Argon2id/PBKDF2 con salt por usuario.
3. Agregar observabilidad (logs estructurados + metricas).
4. Separar frontend y backend si se busca escalado independiente.