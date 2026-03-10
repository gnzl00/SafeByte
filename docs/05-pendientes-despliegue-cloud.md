# 05 - Pendientes tecnicos y de despliegue cloud

Fecha de referencia: 2026-03-09.

Este documento separa claramente:

1. Lo que ya esta implementado en codigo.
2. Lo que aun depende de configuracion manual.
3. Lo que es deuda tecnica planificada.

## 1. Estado actual: que ya quedo resuelto

### 1.1 Preparacion cloud del backend

Ya implementado:

1. Puerto dinamico (`PORT`) y bind `0.0.0.0`.
2. Inicializacion Firestore por `FIREBASE_CREDENTIALS`.
3. Fallback a ADC cuando no hay JSON inline.
4. CORS parametrizable por `CORS_ALLOWED_ORIGINS`.
5. Dockerfile y archivos base de despliegue (`render.yaml`, `.dockerignore`, `.env.example`).

### 1.2 Modulo de producto

Ya implementado:

1. Auth basica con persistencia en Firestore.
2. Persistencia de alergenos por usuario.
3. IANutri completo con historial remoto.
4. Navbar con menu de usuario usando `username` y `email`.
5. Scanner endurecido con liberacion explicita de camara.

## 2. Pendientes manuales obligatorios (fuera de codigo)

## 2.1 Configurar secretos en proveedor cloud

Variables minimas obligatorias:

1. `FIRESTORE__PROJECTID`
2. `FIREBASE_CREDENTIALS`
3. `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
4. `CORS_ALLOWED_ORIGINS`

Motivo:
- Sin estas variables la app arranca incompleta o falla al primer uso real.

## 2.2 Rotacion de credenciales

Accion obligatoria si existio exposicion previa:

1. Revocar key anterior de IA.
2. Generar key nueva y actualizar entorno cloud.
3. Revocar service account key anterior de Firebase.
4. Generar credencial nueva y actualizar `FIREBASE_CREDENTIALS`.

## 2.3 Ajustar clientes externos

1. Si hay app movil separada, cambiar base URL de API a dominio cloud.
2. Recompilar y revalidar login/alergenos/ianutri.

## 3. Deuda tecnica priorizada (producto + plataforma)

## 3.1 Prioridad alta (seguridad)

1. Migrar hash de password a algoritmo adaptativo con salt por usuario.
2. Implementar auth robusta (JWT o Firebase Auth).
3. Dejar de confiar en `email` recibido del cliente para autorizar acciones.
4. Aplicar rate limiting en `Auth` e `IANutri`.

Impacto esperado:
- Reduce riesgo de suplantacion y mejora postura de seguridad minima.

## 3.2 Prioridad media (arquitectura y mantenibilidad)

1. Extraer JS inline de `Home.cshtml` a modulo dedicado.
2. Reducir duplicacion de utilidades (`getCurrentUser`, cache de alergenos) entre scripts.
3. Consolidar codificacion UTF-8 consistente para evitar mojibake.
4. Definir contratos de errores estandarizados en API.

Impacto esperado:
- Menos bugs por divergencia entre pantallas y mantenimiento mas predecible.

## 3.3 Prioridad media-baja (experiencia y observabilidad)

1. Persistir historial scanner y favoritos en backend.
2. Anadir metricas y trazas (latencia endpoint, ratio fallback IA, errores por tipo).
3. Definir dashboard operativo minimo para produccion.

Impacto esperado:
- Mejor experiencia multi-dispositivo y diagnostico mas rapido.

## 4. Plan de ejecucion recomendado por iteraciones

### Iteracion 1 (hardening base)

1. Auth robusta y autorizacion server-side.
2. Migracion de password hash.
3. Rate limiting.

### Iteracion 2 (calidad de codigo)

1. Refactor frontend Home/scanner.
2. Utilidades compartidas para sesion/alergenos.
3. Limpieza de textos/codificacion.

### Iteracion 3 (operacion)

1. Observabilidad.
2. Persistencia de favoritos/historial scanner en backend.
3. Politicas de backup/retencion de Firestore.

## 5. Checklist rapido antes de cada deploy

1. `dotnet build` sin errores.
2. Variables cloud revisadas y actualizadas.
3. Smoke tests de:
- `Auth/Register` y `Auth/Login`.
- `Allergens GET/PUT`.
- `IANutri` (3 endpoints + historial).
4. Verificacion de scanner en movil real.
5. Confirmar que no hay secretos en repo.

## 6. Gestion de ramas para evitar conflictos de sincronizacion

Practica recomendada para evitar bloqueos de `git pull`:

1. Rama corta por feature (`feature/<tema>`).
2. Commits pequenos y frecuentes.
3. Antes de abrir PR:
- `git fetch origin`
- `git rebase origin/main` (o merge, segun politica del equipo)
4. No mezclar cambios no relacionados en la misma rama.
5. Limpiar archivos no trackeados que puedan colisionar antes de pull.

## 7. Riesgos si no se abordan estos pendientes

1. Riesgo de seguridad en credenciales y sesiones.
2. Mayor probabilidad de conflictos de datos entre cliente y backend.
3. Incidencias de rendimiento/estabilidad sin capacidad de diagnostico rapido.
4. Coste creciente de mantenimiento por duplicacion de logica frontend.

## 8. Referencias

1. `DEPLOY.md`
2. `docs/06-render-checklist.md`
3. `README.md`
