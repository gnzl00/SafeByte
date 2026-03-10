# 01 - Setup local y entorno

Ultima actualizacion: 2026-03-09

Este documento explica como levantar SafeByte desde cero en local y por que cada paso existe.

## 1. Objetivo de este setup

Al finalizar deberias poder:

1. ejecutar la web en `http://localhost:5188`;
2. registrar/login usuarios;
3. guardar alergenos en Firestore;
4. ejecutar IANutri contra proveedor IA configurado.

## 2. Requisitos

## 2.1 Obligatorios

1. `.NET 8 SDK`.
2. Proyecto Firebase con Firestore habilitado.
3. Credenciales Firebase con permisos de lectura/escritura sobre coleccion `users`.
4. Key de proveedor IA (`GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`).

## 2.2 Recomendados

1. `git`.
2. VS Code o Visual Studio.
3. `gcloud` CLI para fallback ADC en local.
4. Docker Desktop para pruebas de contenedor.

## 3. Clonar repositorio

```bash
git clone <url-del-repo>
cd SafeByte
```

## 4. Configurar variables de entorno

SafeByte no debe depender de secretos en archivos versionados.

## 4.1 Minimo requerido

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` (o `GITHUB_TOKEN`)
- `CORS_ALLOWED_ORIGINS`

## 4.2 Ejemplo PowerShell

```powershell
$env:FIRESTORE__PROJECTID = "fooddna-b91c1"
$env:FIREBASE_CREDENTIALS = (Get-Content .\service-account.json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)
$env:GITHUB_MODELS_API_KEY = "tu_key"
$env:CORS_ALLOWED_ORIGINS = "http://localhost:5188"
```

## 4.3 Alternativa ADC (solo local)

Si no quieres inyectar `FIREBASE_CREDENTIALS` en local:

```powershell
gcloud auth application-default login
gcloud config set project fooddna-b91c1
```

## 5. Restaurar, compilar y ejecutar

```bash
dotnet restore
dotnet build
dotnet run
```

Por `Properties/launchSettings.json`, el perfil `SafeByte` publica en:

- `http://localhost:5188`

## 6. Verificacion funcional minima

1. Abrir `/` y confirmar pantalla de login.
2. Registrar un usuario nuevo.
3. Iniciar sesion.
4. Guardar alergenos en Home > Configuracion.
5. Abrir Comidas y verificar filtrado por alergenos.
6. Abrir IANutri y generar sugerencias.

## 7. Verificacion de persistencia en Firestore

Documento esperado:

- Coleccion: `users`
- Documento: email normalizado (minusculas)

Campos minimos:

- `username`
- `email`
- `passwordHash`
- `allergens`
- `allergensUpdatedAt`

Subcoleccion IANutri cuando se generan sugerencias:

- `users/{email}/ianutriHistory/{historyId}`

## 8. Notas de arquitectura que afectan setup

1. `Program.cs` usa `PORT` si existe (pensado para cloud), pero en local manda `launchSettings`.
2. CORS en desarrollo permite origen abierto si no defines `CORS_ALLOWED_ORIGINS`; en produccion no.
3. IANutri usa `HttpClient` y proveedor externo; sin key no funciona ese modulo.
4. Seed Firestore solo corre en Development si `Firestore:SeedOnStartup=true`.

## 9. Troubleshooting

## 9.1 `Your default credentials were not found`

Causa:
- no hay `FIREBASE_CREDENTIALS` y no hay ADC.

Solucion:
1. definir `FIREBASE_CREDENTIALS`, o
2. ejecutar login ADC con `gcloud`.

## 9.2 `FIREBASE_CREDENTIALS is not valid JSON`

Causa:
- JSON roto o comillas incorrectas.

Solucion:
1. regenerar variable con `ConvertTo-Json -Compress`.
2. verificar `\\n` en `private_key`.

## 9.3 `No se encontro API key para IANutri`

Causa:
- falta key IA.

Solucion:
- definir `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`.

## 9.4 `address already in use`

Causa:
- otro proceso escuchando mismo puerto.

Solucion:
1. cerrar proceso previo,
2. o cambiar URL de lanzamiento.

## 9.5 Scanner movil se congela

Estado actual mitigado:

1. lectura unica (`decodeOnceFromVideoDevice`),
2. timeout de 15s,
3. limpieza de stream en `stopScanner()`,
4. cleanup en `visibilitychange` y `beforeunload`.

Si aun hay problemas, revisar permisos de camara y navegador movil.

## 10. Seguridad basica durante desarrollo

1. No subir `.env`, `service-account.json` ni claves.
2. Revisar `.gitignore` antes de commit.
3. Rotar secretos si hubo exposicion.