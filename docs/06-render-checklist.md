# 06 - Render checklist (paso a paso)

Ultima actualizacion: 2026-03-09

Checklist corto para evitar errores tipicos en el primer deploy.

## 1. Antes de crear el servicio

1. Confirmar en el repo (rama objetivo) que existen en raiz:
- `Dockerfile`
- `.dockerignore`
- `render.yaml` (opcional, pero recomendado)

2. Confirmar que no hay secretos en archivos versionados.

## 2. Crear Web Service

1. Provider: GitHub repo (padre o fork).
2. Runtime/Language: `Docker`.
3. Branch: la rama que quieres desplegar (`main` o rama de prueba).
4. Region: la mas cercana a tus usuarios.
5. Instance type: `Free` para pruebas.

## 3. Environment Variables

Configurar:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS`

Valor recomendado de `CORS_ALLOWED_ORIGINS` durante pruebas:

```text
https://<service-name>.onrender.com,http://localhost:5188
```

## 4. Advanced

1. `Health Check Path`: `/`
2. `Docker Build Context Directory`: `.`
3. `Dockerfile Path`: `./Dockerfile`
4. `Docker Command`: vacio
5. `Pre-Deploy Command`: vacio

## 5. Errores frecuentes

Error: `failed to read dockerfile: no such file or directory`
1. Revisar `Dockerfile Path`.
2. Revisar que `Dockerfile` este en la rama desplegada.

Error: `Your default credentials were not found`
1. Revisar `FIREBASE_CREDENTIALS` en Render.

Error: `No se encontro API key para IANutri`
1. Revisar `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.

## 6. Smoke test tras deploy

1. Abrir `https://<service-name>.onrender.com/`.
2. Probar login/registro.
3. Guardar alergenos.
4. Ejecutar flujo IANutri.
5. Ver logs en Render si falla algun endpoint.
