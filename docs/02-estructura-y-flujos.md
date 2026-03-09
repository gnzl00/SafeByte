# 02 - Estructura y flujos del proyecto

## 1. Arquitectura general

SafeByte esta dividido en 3 capas:

1. Vista (Razor + JS)
- `Views/Home/*.cshtml`
- `wwwroot/src/ventanas/*.js`

2. Controladores (MVC + API)
- `Controllers/HomeController.cs`
- `Controllers/AuthController.cs`
- `Controllers/AllergensController.cs`
- `Controllers/IANutriController.cs`

3. Datos y servicios
- Firestore (`users/{email}`)
- `Services/*`
- `Models/*`

## 2. Estructura de carpetas (resumen)

1. `Controllers`: endpoints MVC/API.
2. `Models`: DTOs y modelos.
3. `Services`: logica de dominio y utilidades.
4. `Data`: seed local de usuarios.
5. `Views`: pantallas Razor.
6. `wwwroot`: JS/CSS/imagenes.
7. `docs`: documentacion tecnica.

## 3. Archivos clave

1. `Program.cs`: DI, Firestore, routing, CORS.
2. `Controllers/AuthController.cs`: registro/login.
3. `Controllers/AllergensController.cs`: preferencias de alergenos.
4. `Controllers/IANutriController.cs`: reformulacion, sugerencias, asistente e historial.
5. `Services/AllergenCatalog.cs`: catalogo y normalizacion.
6. `Services/IANutriService.cs`: prompts, llamadas IA y saneo.
7. `wwwroot/src/ventanas/index.js`: auth frontend.
8. `wwwroot/src/ventanas/Home.js`: gestion de preferencias.
9. `wwwroot/src/ventanas/comida.js`: filtrado de comidas.
10. `wwwroot/src/ventanas/ianutri.js`: flujo completo IANutri.

## 4. Modelo de datos Firestore

Coleccion principal:
1. `users`

Documento por usuario:
1. ID = email normalizado.
2. Campos principales:
- `username`
- `email`
- `passwordHash`
- `createdAt`
- `allergens`
- `allergensUpdatedAt`

Subcoleccion de historial IA:
1. `users/{email}/ianutriHistory/{historyId}`

## 5. Flujos funcionales

1. Registro/Login:
- `POST /api/Auth/Register`
- `POST /api/Auth/Login`

2. Alergenos:
- `GET /api/Allergens/User`
- `PUT /api/Allergens/User`

3. IANutri:
- `POST /api/IANutri/Reformulate`
- `POST /api/IANutri/GenerateSuggestions`
- `POST /api/IANutri/CookingAssistant`
- `GET /api/IANutri/History`
- `DELETE /api/IANutri/History`

## 6. Local storage (frontend)

1. `sb_user`: usuario actual.
2. `sb_alergenos`: cache de alergenos.
3. `alergenosSeleccionados`: clave legacy.

## 7. Limites actuales

1. No hay JWT ni sesion de servidor robusta.
2. Parte de endpoints usan email del cliente como identificador.

Para produccion:
1. autenticacion fuerte (token)
2. autorizacion por identidad validada en backend

## 8. Referencias de detalle

1. `docs/03-alergenos-mvc-y-persistencia.md`
2. `docs/04-ianutri-arquitectura-y-flujo-e2e.md`
