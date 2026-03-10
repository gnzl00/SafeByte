# 06 - Render checklist operativo completo

Fecha de referencia: 2026-03-09.

Checklist para desplegar SafeByte en Render sin errores tipicos de primer arranque.

## 1. Preflight local (antes de tocar Render)

1. Confirmar build local:

```bash
dotnet restore
dotnet build
```

2. Confirmar que existen en raiz:
- `Dockerfile`
- `.dockerignore`
- `render.yaml` (opcional, recomendado)

3. Confirmar que no hay secretos versionados:
- buscar `.env`, JSON de service account, claves API.

4. Confirmar rama objetivo actualizada con remoto.

## 2. Crear el servicio en Render

1. Tipo: `Web Service`.
2. Source: repo GitHub correcto.
3. Branch: `main` o rama de prueba controlada.
4. Runtime: `Docker`.
5. Region: la mas cercana a usuarios finales.
6. Plan: `Free` para demo o superior para uso estable.

## 3. Configuracion Build y Runtime

En Render, revisar:

1. `Docker Build Context Directory`: `.`
2. `Dockerfile Path`: `./Dockerfile`
3. `Docker Command`: vacio
4. `Pre-Deploy Command`: vacio

Motivo:
- La imagen ya define todo lo necesario.

## 4. Variables de entorno obligatorias

Configurar como secret env vars:

1. `ASPNETCORE_ENVIRONMENT=Production`
2. `FIRESTORE__PROJECTID=<tu_project_id>`
3. `FIREBASE_CREDENTIALS=<json_service_account_en_una_linea>`
4. `GITHUB_MODELS_API_KEY=<tu_key>` (o `GITHUB_TOKEN`)
5. `CORS_ALLOWED_ORIGINS=https://<service>.onrender.com`

Notas importantes:

1. No envolver todo el JSON en comillas dobles extras.
2. Mantener `\n` en `private_key`.
3. Si tienes frontend externo, agregar su dominio a CORS separado por coma.

## 5. Health check y red

1. `Health Check Path`: `/`
2. Confirmar que el servicio responde HTML de login en raiz.
3. Render inyecta `PORT`; el backend ya lo soporta.

## 6. Smoke test post deploy (obligatorio)

Ejecutar en este orden:

1. `GET /`
2. `POST /api/Auth/Register`
3. `POST /api/Auth/Login`
4. `GET /api/Allergens/User?email=...`
5. `PUT /api/Allergens/User`
6. `POST /api/IANutri/Reformulate`
7. `POST /api/IANutri/GenerateSuggestions`
8. `POST /api/IANutri/CookingAssistant`
9. `GET /api/IANutri/History?email=...`
10. `DELETE /api/IANutri/History?email=...`

Tambien validar UI:

1. Navbar con menu de usuario y logout.
2. Guardado de alergenos desde Home.
3. Flujo completo de IANutri.
4. Scanner en movil (inicio, lectura y cierre sin bloqueo).

## 7. Errores frecuentes y accion inmediata

### 7.1 `failed to read dockerfile`

Causa:
- ruta `Dockerfile Path` mal configurada o branch incorrecta.

Accion:
- revisar path y confirmar que archivo existe en la rama desplegada.

### 7.2 `Your default credentials were not found`

Causa:
- falta o mal formato de `FIREBASE_CREDENTIALS`.

Accion:
- regenerar JSON compactado y pegarlo de nuevo.

### 7.3 `No se encontro API key para IANutri`

Causa:
- variable de API key ausente o mal nombrada.

Accion:
- definir `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.

### 7.4 `unknown_model`

Causa:
- modelo no disponible para la key/proveedor.

Accion:
- revisar configuracion de modelos permitidos; el servicio ya intenta fallback.

### 7.5 Scanner bloquea navegador movil

Comprobaciones:

1. Verificar que esta desplegada version con `decodeOnceFromVideoDevice`.
2. Verificar timeout activo.
3. Verificar `stopScanner()` en `visibilitychange` y `beforeunload`.

## 8. Rollback rapido

Si un deploy rompe funcionalidad critica:

1. Abrir Render Deploy History.
2. Re-deploy de build anterior estable.
3. Congelar auto-deploy temporalmente.
4. Corregir en rama y volver a desplegar.

## 9. Checklist de cierre de release

1. Logs sin excepciones criticas al arrancar.
2. Auth, Allergens e IANutri validados manualmente.
3. CORS restringido a dominios reales.
4. Credenciales rotadas si hubo riesgo de exposicion.
5. Documentacion actualizada con fecha de referencia.

## 10. Referencias

1. `DEPLOY.md`
2. `docs/05-pendientes-despliegue-cloud.md`
3. `README.md`
