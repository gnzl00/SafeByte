# 05 - Pendientes manuales para despliegue cloud

Este documento lista lo que ya quedo implementado en codigo y lo que debes hacer manualmente fuera del repositorio.

## 1. Lo que ya quedo implementado

1. Firebase ya no se inicializa con archivo local (`secrets/service-account.json`).
2. Firebase usa `FIREBASE_CREDENTIALS` (JSON string en variable de entorno).
3. IANutri prioriza `GITHUB_MODELS_API_KEY`/`GITHUB_TOKEN` (compatible con `OPENAI_API_KEY`).
4. El backend soporta `PORT` dinamico y bind en `0.0.0.0:{PORT}`.
5. CORS de produccion usa `CORS_ALLOWED_ORIGINS`.
6. Se eliminaron credenciales locales detectadas del arbol de trabajo (`.env`, `secrets/service-account.json`).
7. Se agregaron artefactos de despliegue: `Dockerfile`, `render.yaml`, `.env.example`, `DEPLOY.md`.

## 2. Pendientes que debes hacer tu (no automatizables desde aqui)

### 2.1 Configurar secretos en tu proveedor cloud

Debes crear estas variables en Render/Railway/Azure App Service:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS`

Pasos:

1. Abre el panel del servicio cloud.
2. Ve a configuracion de entorno (Environment / App Settings).
3. Crea cada variable sin comillas extra.
4. Guarda y reinicia el servicio.

### 2.2 Crear el valor correcto de FIREBASE_CREDENTIALS

1. En Firebase Console, genera una nueva Service Account key para produccion.
2. No subas el JSON al repo.
3. Convierte el JSON en una sola linea (manteniendo `\\n` en `private_key`).
4. Pega esa linea en la variable `FIREBASE_CREDENTIALS` del proveedor cloud.

### 2.3 Rotar credenciales si hubo exposicion previa

Se detectaron credenciales locales en el arbol antes de la limpieza. Debes rotarlas fuera del codigo:

1. Revoca la API key/token anterior del proveedor de IA.
2. Genera una nueva key y actualiza `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.
3. Revoca la Service Account key anterior de Firebase.
4. Genera una nueva key y actualiza `FIREBASE_CREDENTIALS`.

### 2.4 Configurar CORS con dominios reales

1. Define dominios frontend en produccion, separados por comas.
2. Ejemplo:
   `https://app.tudominio.com,https://admin.tudominio.com`
3. Guarda en `CORS_ALLOWED_ORIGINS`.

### 2.5 Actualizar cliente Android con URL backend cloud

No se detecto codigo Android Kotlin en este repositorio para cambiar la base URL.

Debes hacerlo en el repo/app movil:

1. Cambia la URL base de API desde localhost a la URL publica del backend cloud.
2. Recompila la app Android.
3. Prueba login, alergenos e IANutri contra el backend desplegado.

### 2.6 Despliegue efectivo (pasos minimos)

1. Sube estos cambios al repositorio remoto.
2. Crea servicio en Render/Railway/Azure desde repo o Dockerfile.
3. Configura variables del punto 2.1.
4. Despliega.
5. Revisa logs de arranque y health endpoint (`/`).

### 2.7 Si `git push origin main` es rechazado (non-fast-forward)

1. Trae cambios remotos:
   `git fetch origin`
2. Actualiza tu `main` local:
   `git checkout main`
   `git pull --rebase origin main`
3. Integra tu rama de trabajo:
   `git merge --no-ff <tu-rama>`
4. Sube `main`:
   `git push origin main`

Alternativa de prueba rapida:
1. Subir rama al fork y desplegar esa rama en Render.

### 2.8 Campos Render que no debes olvidar

En `Advanced`:
1. `Health Check Path`: `/`
2. `Docker Build Context Directory`: `.`
3. `Dockerfile Path`: `./Dockerfile`

Si `Dockerfile Path` apunta mal, Render falla con:
1. `failed to read dockerfile: no such file or directory`

## 3. Verificacion recomendada post-despliegue

1. `POST /api/Auth/Register`.
2. `POST /api/Auth/Login`.
3. `GET/PUT /api/Allergens/User`.
4. `POST /api/IANutri/Reformulate`.
5. `POST /api/IANutri/GenerateSuggestions`.

Si falla Firebase:

1. Revisar `FIRESTORE__PROJECTID`.
2. Revisar formato JSON de `FIREBASE_CREDENTIALS`.
3. Revisar permisos IAM de la Service Account.
4. Si el error es `Your default credentials were not found`, define `FIREBASE_CREDENTIALS` en local o usa ADC (`gcloud auth application-default login`).

Si falla IANutri:

1. Revisar `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.
2. Revisar conectividad saliente del proveedor cloud.

## 4. Nota sobre ZIP

No se genero ZIP final por tu instruccion explicita en esta iteracion.
