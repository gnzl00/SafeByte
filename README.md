# SafeByte

SafeByte es una aplicacion web orientada a seguridad alimentaria para usuarios con alergias o intolerancias.

Fecha de referencia de esta documentacion: 2026-03-09.

## 1. Que es SafeByte y que problema resuelve

El proyecto nace para cubrir un flujo muy concreto:

1. El usuario se registra/inicia sesion.
2. Guarda sus alergenos personales.
3. Consulta recetas y recomendaciones filtradas por seguridad.
4. Usa un asistente IA (IANutri) para reformular peticiones, proponer menus seguros y guiar cocina paso a paso.

El foco no es solo "recomendar comida", sino reducir riesgo de ingesta insegura por contaminacion cruzada, ingredientes conflictivos o decisiones improvisadas.

## 2. Stack tecnico real y por que se eligio

### 2.1 ASP.NET Core 8 (MVC + API)

- Se usa `net8.0` con un unico backend que sirve vistas y APIs.
- Motivo: unificar desarrollo full stack en un solo runtime C#, simplificar despliegue y minimizar complejidad de integracion.
- Impacto: menos piezas operativas; a cambio, frontend no esta desacoplado como SPA moderna.

### 2.2 Razor + JavaScript vanilla

- UI server-rendered con Razor (`Views/Home/*.cshtml`) y logica cliente en `wwwroot/src/ventanas/*.js`.
- Motivo: velocidad de implementacion para equipo academico, curva de aprendizaje baja y cero toolchain frontend obligatorio.
- Impacto: desarrollo rapido; a cambio, mas JS imperative y menos componentizacion.

### 2.3 Firebase Firestore

- Persistencia principal en coleccion `users` y subcoleccion `ianutriHistory`.
- Motivo: base gestionada, esquema flexible y buen encaje para documentos JSON de historial IA.
- Impacto: operacion simple; a cambio, sin transacciones complejas relacionales ni joins nativos.

### 2.4 GitHub Models como proveedor IA por defecto

- `IANutriService` consume endpoint compatible chat-completions.
- Motivo: coste/control para entorno de pruebas y compatibilidad con fallback de modelos.
- Impacto: flexibilidad multi proveedor; a cambio, se depende de disponibilidad externa para IANutri.

### 2.5 Docker + Render

- Se incluye `Dockerfile`, `.dockerignore` y `render.yaml`.
- Motivo: despliegue reproducible y sencillo en proveedor gratuito para demos.
- Impacto: onboarding rapido de infraestructura; a cambio, limits de free tier (cold starts, recursos).

## 3. Estado funcional actual (2026-03-09)

Implementado y operativo:

1. Autenticacion basica por email/password (`/api/Auth/Register`, `/api/Auth/Login`).
2. Persistencia de alergenos por usuario en Firestore (`/api/Allergens/User`).
3. Home con:
- scanner de codigo de barras,
- historial local de escaneos,
- configuracion de usuario.
4. Pagina de comidas con filtro por alergenos y favoritos por usuario.
5. Modulo IANutri completo:
- reformulacion de peticion,
- generacion de sugerencias,
- asistente de cocina,
- historial remoto por usuario.
6. Menu de usuario en navbar (esquina superior derecha) que usa `username` y `email` de sesion local.
7. Endurecimiento del scanner movil:
- lectura unica por intento,
- timeout,
- liberacion de camara al terminar/cambiar pestaña/salir,
- prevencion de loops de decodificacion.

## 4. Arquitectura de alto nivel

### 4.1 Capas

1. Presentacion:
- `Views/Home/*.cshtml`
- `Views/Shared/_AppNavbar.cshtml`
- `wwwroot/src/css/*.css`

2. Logica cliente:
- `wwwroot/src/ventanas/index.js` (auth)
- `wwwroot/src/ventanas/Home.js` (home + alergenos + historial local)
- `wwwroot/src/ventanas/comida.js` (recetas + favoritos + filtro)
- `wwwroot/src/ventanas/ianutri.js` (flujo IA completo)
- `wwwroot/src/ventanas/navbar.js` (menu de usuario)

3. API/control:
- `Controllers/AuthController.cs`
- `Controllers/AllergensController.cs`
- `Controllers/IANutriController.cs`
- `Controllers/HomeController.cs`

4. Dominio/servicios:
- `Services/AllergenCatalog.cs`
- `Services/PasswordHasher.cs`
- `Services/IANutriService.cs`
- `Services/IANutriOptions.cs`

5. Datos:
- Firestore (`users/{email}`)
- `users/{email}/ianutriHistory/{historyId}`

### 4.2 Bootstrap de aplicacion

`Program.cs` concentra decisiones clave:

1. Soporte de puerto dinamico por `PORT` (cloud friendly).
2. Inicializacion Firestore por `FIREBASE_CREDENTIALS` o ADC fallback.
3. CORS por `CORS_ALLOWED_ORIGINS` en produccion.
4. Registro DI de `IIANutriService` con `HttpClient`.
5. Seed opcional solo en desarrollo (`Firestore:SeedOnStartup`).

## 5. Decisiones tecnicas importantes y justificacion

1. Documento de usuario identificado por email normalizado.
- Por que: evita indice adicional y simplifica lecturas directas.
- Coste: dependencia fuerte de email como identidad tecnica.

2. Sesion cliente en `localStorage` (`sb_user`).
- Por que: implementacion rapida sin infraestructura de auth server-side.
- Coste: no hay sesion robusta basada en token firmado.

3. Catalogo cerrado de alergenos.
- Por que: consistencia de UX, validacion estricta y filtros previsibles.
- Coste: menor flexibilidad para alergenos no contemplados.

4. Prompting estricto + saneo defensivo en IANutri.
- Por que: los modelos pueden romper formato o mezclar metadatos.
- Coste: mayor complejidad de codigo de parsing y fallback.

5. Fallback de modelos y de contenido IA.
- Por que: resiliencia ante `unknown_model`, salida vacia o JSON roto.
- Coste: comportamiento menos determinista entre proveedores.

6. Historial IA en subcoleccion por usuario.
- Por que: escalado natural por usuario y consultas sencillas por fecha.
- Coste: no hay analitica global sin pipelines adicionales.

7. Scanner en Home con ZXing y limpieza explicita de recursos.
- Por que: evitar freeze por streams abiertos o decodificacion continua.
- Coste: logica de UI mas manual.

## 6. Limitaciones y deuda tecnica consciente

1. Seguridad de credenciales:
- hashing actual con SHA-256 simple (`PasswordHasher`) sin salt/cost adaptativo.
- recomendado: migrar a Argon2id o PBKDF2 robusto con versionado de hash.

2. Autenticacion/autorizacion:
- no hay JWT ni Firebase Auth ni cookies seguras de servidor.
- varios endpoints aceptan `email` del cliente.

3. Frontend:
- parte de UI depende de JS inline y archivos grandes (`Comidas.cshtml`).
- recomendado: modularizacion por componentes y separacion de datos.

4. Observabilidad:
- sin trazas distribuidas ni metricas de negocio.
- recomendado: structured logging, correlation ids, health/readiness mas ricos.

## 7. Arranque local rapido

```bash
dotnet restore
dotnet build
dotnet run
```

URL local por `launchSettings`:

- `http://localhost:5188`

## 8. Variables de entorno

### 8.1 Requeridas para funcionamiento completo

- `FIRESTORE__PROJECTID`
- `FIREBASE_CREDENTIALS`
- `GITHUB_MODELS_API_KEY` o `GITHUB_TOKEN`
- `CORS_ALLOWED_ORIGINS` (en produccion)

### 8.2 Compatibilidad y alternativas

- `IANUTRI_API_KEY`
- `OPENAI_API_KEY` (si cambias `IANutri:BaseUrl` a OpenAI)

## 9. Deploy rapido

1. Crear servicio Docker en Render.
2. Confirmar `Dockerfile` en raiz.
3. Configurar variables requeridas.
4. Health check path: `/`.
5. Verificar flujos `Auth`, `Allergens` y `IANutri` tras deploy.

## 10. Documentacion completa

- Indice general: `docs/00-indice.md`
- Setup local y entorno: `docs/01-setup.md`
- Arquitectura y flujos de sistema: `docs/02-estructura-y-flujos.md`
- Dominio de alergenos y persistencia: `docs/03-alergenos-mvc-y-persistencia.md`
- Arquitectura IANutri E2E: `docs/04-ianutri-arquitectura-y-flujo-e2e.md`
- Pendientes y roadmap tecnico: `docs/05-pendientes-despliegue-cloud.md`
- Checklist operativo Render: `docs/06-render-checklist.md`
- Guia de despliegue cloud: `DEPLOY.md`