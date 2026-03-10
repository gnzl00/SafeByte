# 02 - Estructura, arquitectura y flujos del sistema

Fecha de referencia: 2026-03-09.

Este documento explica como esta construido SafeByte hoy, por que se tomo cada decision principal y como fluye la informacion de extremo a extremo.

## 1. Arquitectura global

SafeByte sigue una arquitectura monolitica web con separacion por capas dentro del mismo proceso ASP.NET Core:

1. Presentacion server-rendered:
- Razor Views en `Views/Home/*.cshtml`.
- Parcial compartida de navegacion en `Views/Shared/_AppNavbar.cshtml`.

2. Logica cliente:
- JavaScript vanilla en `wwwroot/src/ventanas/*.js`.
- CSS en `wwwroot/src/css/*.css`.

3. API y control HTTP:
- Controladores REST en `Controllers/*.cs`.

4. Servicios de dominio:
- `Services/AllergenCatalog.cs`.
- `Services/IANutriService.cs`.
- `Services/PasswordHasher.cs`.

5. Persistencia:
- Firestore, coleccion `users`.
- Subcoleccion `users/{email}/ianutriHistory`.

## 2. Por que esta arquitectura y no otra

### 2.1 Un solo backend (MVC + API)

Decision:
- Backend unico ASP.NET Core 8 que sirve vistas y endpoints JSON.

Motivo tecnico:
- Menos complejidad operativa para equipo pequeno.
- Menor friccion para desarrollo academico con tiempo limitado.
- Menos coste de deploy (un solo servicio).

Coste asumido:
- Frontend no desacoplado como SPA moderna.
- Menos facilidad para escalar frontend y backend por separado.

### 2.2 JavaScript vanilla en vez de framework frontend

Decision:
- UI dinamica con JS directo sobre DOM.

Motivo tecnico:
- Sin build pipeline complejo.
- Curva de aprendizaje baja para iterar rapido.

Coste asumido:
- Mayor codigo imperativo.
- Menor reutilizacion tipada de componentes.

### 2.3 Firestore como base principal

Decision:
- Persistir usuario y preferencias en documentos por email.

Motivo tecnico:
- Base gestionada, sin administrar servidor SQL.
- Encaje natural para historial IA en subcolecciones JSON.

Coste asumido:
- Sin joins relacionales.
- Dependencia de convencion de documento por email.

### 2.4 Proveedor IA compatible chat-completions

Decision:
- IANutri usa endpoint tipo OpenAI con GitHub Models por defecto.

Motivo tecnico:
- Flexibilidad de modelos y fallback.
- Cambio de proveedor por configuracion, no por refactor grande.

Coste asumido:
- Dependencia de API externa para funciones IA.
- Variabilidad de salida del modelo, mitigada con saneo y fallback.

## 3. Bootstrap y runtime (`Program.cs`)

`Program.cs` concentra decisiones de plataforma:

1. Puerto cloud-friendly:
- Si existe `PORT`, hace bind a `http://0.0.0.0:{PORT}`.

2. Firestore robusto:
- Intenta inicializar con `FIREBASE_CREDENTIALS` (JSON completo).
- Si no existe, intenta ADC (`FirestoreDb.Create(projectId)`).
- Si falla, aborta con error explicito de configuracion.

3. CORS controlado por entorno:
- En produccion usa `CORS_ALLOWED_ORIGINS`.
- En desarrollo permite origen abierto si no hay lista.

4. DI principal:
- `AddControllersWithViews()`.
- `Configure<IANutriOptions>`.
- `AddHttpClient<IIANutriService, IANutriService>()`.

5. Seed de desarrollo:
- Solo en `Development` y con `Firestore:SeedOnStartup=true`.
- Crea usuarios desde `Data/Seed/Users.json` con hash de password.

## 4. Mapa de carpetas con responsabilidad

1. `Controllers/`
- Orquestacion HTTP y validaciones de entrada.

2. `Models/`
- DTOs de request/response para Auth, Allergens e IANutri.

3. `Services/`
- Reglas de negocio reutilizables y clientes externos.

4. `Data/`
- Seed local y utilidades de carga de datos de prueba.

5. `Views/`
- Vistas Razor y composicion HTML base.

6. `wwwroot/src/ventanas/`
- Estado de UI, eventos y llamadas a API.

7. `wwwroot/src/css/`
- Estilos por pantalla/modulo.

8. `docs/`
- Documentacion tecnica, operativa y de decisiones.

## 5. Flujos funcionales principales

### 5.1 Registro y login

Secuencia:

1. `index.js` envia `POST /api/Auth/Register` o `POST /api/Auth/Login`.
2. `AuthController` normaliza email (`trim + lowercase`).
3. Registro:
- rechaza email existente,
- guarda `passwordHash`, `allergens=[]`, `allergensUpdatedAt`.
4. Login:
- compara hash,
- devuelve `username`, `email`, `allergens`.
5. Front guarda sesion local en `sb_user` y cache de alergenos.

Decision:
- Sesion local rapida por `localStorage`.

Coste:
- No hay token firmado ni expiracion centralizada.

### 5.2 Gestion de alergenos

Secuencia:

1. Home carga preferencias con `GET /api/Allergens/User?email=...`.
2. `AllergensController` lee documento Firestore y normaliza lista.
3. Al guardar, Home envia `PUT /api/Allergens/User`.
4. Backend valida contra catalogo cerrado y actualiza `allergens`.
5. Front sincroniza cache local para evitar latencia entre pantallas.

Decision:
- Catalogo cerrado (`Gluten`, `Lacteos`, `Huevo`, `Frutos secos`, `Mariscos`, `Soja`).

Motivo:
- Consistencia entre UI, API y filtrado de recetas.

Coste:
- Menos flexibilidad para alergenos fuera de catalogo.

### 5.3 Flujo de Comidas y favoritos

Secuencia:

1. `comida.js` resuelve alergenos remotos con fallback local.
2. Filtra dataset de recetas ocultando platos con conflicto directo.
3. Favoritos se guardan por usuario en clave `sb_favoritos_{email}`.

Decision:
- Favoritos en cliente para simplicidad y velocidad.

Coste:
- No sincroniza favoritos entre dispositivos.

### 5.4 Scanner de producto

Secuencia:

1. En `Home.cshtml`, `startScanner()` usa `decodeOnceFromVideoDevice`.
2. Se aplica timeout de lectura.
3. Tras leer o fallar, `stopScanner()` libera tracks de camara.
4. `processBarcode()` consulta Open Food Facts y guarda historial local.

Decision:
- Lectura unica por intento + limpieza explicita de recursos.

Motivo:
- Evitar loops continuos y bloqueos en moviles/navegador.

Coste:
- UX menos "streaming continuo", pero mas estable.

### 5.5 IANutri end-to-end

Secuencia:

1. Front solicita reformulacion (`POST /api/IANutri/Reformulate`).
2. Backend resuelve alergenos remotos con fallback a payload.
3. `IANutriService` llama al modelo con prompt estricto JSON.
4. Front habilita generacion cuando existe reformulacion valida.
5. Front genera sugerencias (`POST /GenerateSuggestions`).
6. Backend guarda historial en Firestore y devuelve `historyId`.
7. Usuario abre receta -> `POST /CookingAssistant`.
8. Backend actualiza historial con bloque `assistant`.
9. Historial remoto se consulta con `GET /History` y limpia con `DELETE /History`.

Decision:
- Pipeline en 3 fases (reformular, sugerir, asistir) en vez de una sola llamada.

Motivo:
- Mejor control UX y mejor trazabilidad del historial.

Coste:
- Mas llamadas HTTP y mas estado cliente.

## 6. Modelo de datos principal

### 6.1 Usuario

Ruta:
- `users/{normalizedEmail}`

Campos relevantes:
- `username`
- `email`
- `passwordHash`
- `createdAt`
- `allergens`
- `allergensUpdatedAt`

### 6.2 Historial IANutri

Ruta:
- `users/{normalizedEmail}/ianutriHistory/{historyId}`

Campos relevantes:
- `userInput`
- `option`
- `reformulatedPrompt`
- `summary`
- `allergens`
- `globalWarnings`
- `generalSubstitutions`
- `suggestions`
- `assistant`
- `createdAt`, `updatedAt`

Decision:
- Historial como subcoleccion del usuario.

Motivo:
- Lecturas acotadas por usuario y limite simple por fecha.

## 7. Reglas de consistencia transversal

1. Email siempre normalizado en backend.
2. Alergenos siempre normalizados por `AllergenCatalog`.
3. Front conserva cache local para tolerar fallos temporales de API.
4. IANutri sanea texto de modelo para quitar ruido y tokens bloqueados.
5. Historial limitado a 40 elementos al consultar (`HistoryLimit`).

## 8. Seguridad actual y deuda abierta

### 8.1 Lo implementado

1. Hash de password antes de persistir.
2. Validaciones de payload y controles de errores en controladores.
3. Secretos fuera del repo mediante variables de entorno.

### 8.2 Lo pendiente (deuda explicita)

1. Migrar de hash simple a algoritmo adaptativo con salt por usuario.
2. Implementar autenticacion robusta (JWT/Firebase Auth) y autorizacion real.
3. Evitar aceptar `email` libre como identidad en cada request.
4. Anadir rate limiting para endpoints de IA y auth.

## 9. Operacion y observabilidad

Estado actual:

1. Logs basicos de ASP.NET y excepciones.
2. Sin metricas de negocio ni trazas distribuidas.
3. Health check funcional via ruta `/`.

Decision:
- Mantener operacion simple para etapa academica/demo.

Coste:
- Diagnostico mas manual ante incidencias complejas.

## 10. Tradeoffs globales del proyecto

1. Se priorizo velocidad de entrega y estabilidad funcional sobre arquitectura enterprise.
2. Se eligio simplicidad operativa (monolito + Firestore + JS vanilla).
3. Se asumio deuda de seguridad y escalado para una fase posterior.
4. Se reforzo resiliencia de IANutri y scanner porque eran puntos de fallo mas visibles para usuario final.

## 11. Referencias

1. `README.md`
2. `docs/03-alergenos-mvc-y-persistencia.md`
3. `docs/04-ianutri-arquitectura-y-flujo-e2e.md`
4. `DEPLOY.md`
