# Cloud Deployment Guide (ASP.NET Core 8)

SafeByte backend runtime is **ASP.NET Core / .NET 8**. Node.js is not required to run the backend in production.

## 1. Required environment variables

- `FIRESTORE__PROJECTID`: Firestore project id (maps to `Firestore:ProjectId`)
- `FIREBASE_CREDENTIALS`: Full Firebase service-account JSON string
- `GITHUB_MODELS_API_KEY` (or `GITHUB_TOKEN`): Primary API key for IANutri
- `CORS_ALLOWED_ORIGINS` (recommended): comma-separated list of allowed frontend origins

Optional compatibility variables:

- `IANUTRI_API_KEY`
- `OPENAI_API_KEY` (if you switch provider endpoint to OpenAI)

Cloud runtime variable:

- `PORT`: injected by host (Render/Railway/Azure). The app listens on `0.0.0.0:{PORT}` when available.

## 2. Local build/run

```bash
dotnet restore
dotnet build
dotnet run
```

## 3. Docker deployment

```bash
docker build -t safebyte-backend .
docker run -p 8080:8080 \
  -e FIRESTORE__PROJECTID="your-project-id" \
  -e FIREBASE_CREDENTIALS='{"type":"service_account",...}' \
  -e GITHUB_MODELS_API_KEY="your-key" \
  safebyte-backend
```

## 4. Render (Blueprint)

- Use `render.yaml` in repo root.
- Set secret values in Render dashboard for:
  - `FIRESTORE__PROJECTID`
  - `FIREBASE_CREDENTIALS`
  - `GITHUB_MODELS_API_KEY` (or `GITHUB_TOKEN`)
  - `CORS_ALLOWED_ORIGINS` (if frontend is hosted on another domain)

## 5. Railway

- Deploy from GitHub repo or Dockerfile.
- Add the same environment variables from section 1.
- Railway injects `PORT` automatically.

## 6. Azure App Service

- Runtime stack: `.NET 8` or container using provided `Dockerfile`.
- Configure the same environment variables in App Settings.
- Do not upload any `service-account.json` file.

## 7. Security baseline checklist

- Keep `.env`, `secrets/`, and service-account files out of git.
- Store secrets only in cloud secret manager/env settings.
- Rotate leaked keys immediately.
- Use production-only credentials in production.
- Restrict CORS in production with `CORS_ALLOWED_ORIGINS`.
