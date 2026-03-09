# 01 - Setup

Ultima actualizacion: 2026-03-09

## 1. Requisitos

Obligatorio:
1. `.NET 8 SDK`
2. Proyecto Firebase con Firestore habilitado
3. Key para GitHub Models (`GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`)

Opcional:
1. `git`
2. VS Code o Visual Studio
3. `gcloud` CLI (si quieres ADC local)

## 2. Clonar y entrar al repo

```bash
git clone <url-del-repo>
cd SafeByte
```

## 3. Variables de entorno

Minimas para ejecutar backend:

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS` (JSON completo de Service Account)
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
- `CORS_ALLOWED_ORIGINS` (para produccion)

Ejemplo de `FIREBASE_CREDENTIALS` (una sola linea):

```text
{"type":"service_account","project_id":"tu-project-id","private_key_id":"...","private_key":"-----BEGIN PRIVATE KEY-----\\n...\\n-----END PRIVATE KEY-----\\n","client_email":"...","client_id":"...","token_uri":"https://oauth2.googleapis.com/token"}
```

## 4. Configuracion recomendada local (PowerShell)

```powershell
$env:FIRESTORE__PROJECTID = "fooddna-b91c1"
$env:FIREBASE_CREDENTIALS = (Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
$env:GITHUB_MODELS_API_KEY = "tu_key"
$env:CORS_ALLOWED_ORIGINS = "http://localhost:5188"
```

Alternativa con ADC (si no usas `FIREBASE_CREDENTIALS`):

```powershell
gcloud auth application-default login
gcloud config set project fooddna-b91c1
```

## 5. Restaurar y ejecutar

```bash
dotnet restore
dotnet build
dotnet run
```

URL local por perfil de lanzamiento:

- `http://localhost:5188`

## 6. Verificacion minima

1. Abrir app en navegador.
2. Registrar usuario e iniciar sesion.
3. Guardar alergenos y confirmar en Firestore.
4. Abrir `IANutri` y generar sugerencia.

## 7. Troubleshooting

Error: `Your default credentials were not found`
1. Define `FIREBASE_CREDENTIALS` antes de `dotnet run`, o
2. configura ADC con `gcloud auth application-default login`.

Error: `FIREBASE_CREDENTIALS is not valid JSON`
1. Revisar formato JSON en una sola linea.
2. Revisar escapes `\\n` en `private_key`.

Error: `No se encontro API key para IANutri`
1. Configurar `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.

Error: `address already in use`
1. Cerrar proceso previo o cambiar puerto local.

## 8. Nota de seguridad

- No subir `.env`, `secrets/` ni `service-account*.json` al repositorio.
- Si una clave se expone, rotarla de inmediato.
