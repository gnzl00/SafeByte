# 02 - Estructura y flujos del proyecto

## 1. Arquitectura general

SafeByte esta dividido en 3 capas principales:

1. Vista (Razor + JS):
- `Views/Home/*.cshtml`
- `wwwroot/src/ventanas/*.js`

2. Controladores (API + MVC):
- `Controllers/HomeController.cs` (vistas)
- `Controllers/AuthController.cs` (registro/login)
- `Controllers/AllergensController.cs` (preferencias de alergenos)

3. Datos (Firestore):
- Coleccion `users`
- Documento por usuario: `users/{email_normalizado}`

## 2. Estructura de carpetas (resumen)

- `Controllers`: endpoints MVC/API.
- `Models`: modelos de dominio y DTO.
- `Services`: utilidades (`PasswordHasher`, `AllergenCatalog`).
- `Data`: utilidades legacy de seed en JSON.
- `Views`: pantallas Razor.
- `wwwroot`: JS/CSS/imagenes estaticas.
- `docs`: documentacion tecnica.

## 3. Archivos clave

- `Program.cs`: configura Firestore, CORS, routing y seed.
- `Controllers/AuthController.cs`: login/registro.
- `Controllers/AllergensController.cs`: API para leer/guardar alergenos por usuario.
- `Services/AllergenCatalog.cs`: catalogo y normalizacion de alergenos permitidos.
- `wwwroot/src/ventanas/index.js`: login/registro en frontend.
- `wwwroot/src/ventanas/Home.js`: carga y guardado de preferencias.
- `wwwroot/src/ventanas/comida.js`: filtrado de comidas con alergenos del usuario.

## 4. Modelo de datos en Firestore

Coleccion: `users`

Documento: ID = email en minusculas.

Campos principales:
- `username` (string)
- `email` (string)
- `passwordHash` (string)
- `createdAt` (timestamp)
- `allergens` (array de string)
- `allergensUpdatedAt` (timestamp)
- `seeded` (bool opcional en usuarios de semilla)

## 5. Flujo de registro

1. Usuario rellena formulario en `Index`.
2. Frontend llama `POST /api/Auth/Register`.
3. Backend:
- normaliza email
- verifica que no exista
- hashea password
- crea usuario con `allergens: []`
4. Frontend guarda sesion local (`sb_user`) y redirige a `/Home/Home`.

## 6. Flujo de login

1. Usuario hace login en `Index`.
2. Frontend llama `POST /api/Auth/Login`.
3. Backend valida password hash.
4. Backend devuelve datos del usuario + alergenos guardados.
5. Frontend guarda `sb_user` y cache de alergenos local.

## 7. Flujo de preferencias de alergenos

1. En `Home`, `Home.js` detecta usuario logueado.
2. Si hay usuario:
- llama `GET /api/Allergens/User?email=...`
- pinta checkboxes con datos remotos
3. Si no hay usuario (skip login):
- usa almacenamiento local como fallback
4. Al pulsar `Guardar preferencias`:
- si logueado: `PUT /api/Allergens/User`
- si invitado: guardado local

## 8. Flujo de filtrado de comidas

1. `comida.js` intenta cargar alergenos remotos del usuario.
2. Si falla o no hay usuario, usa cache local.
3. Filtra `comidas` excluyendo recetas con alergenos bloqueados.
4. Aplica tambien texto de busqueda.

## 9. Endpoints API actuales

Auth:
- `POST /api/Auth/Register`
- `POST /api/Auth/Login`

Alergenos:
- `GET /api/Allergens/Catalog`
- `GET /api/Allergens/User?email=...`
- `PUT /api/Allergens/User`

## 10. Claves de almacenamiento local (frontend)

- `sb_user`: usuario actual (`email`, `username`)
- `sb_alergenos`: cache local de alergenos
- `alergenosSeleccionados`: clave legacy mantenida por compatibilidad

## 11. Limitaciones actuales (importante)

1. No hay JWT/cookies de sesion de servidor.
2. La API de alergenos identifica usuario por email recibido.
3. Para un entorno productivo real:
- añadir autenticacion robusta (JWT/Firebase Auth)
- validar identidad en backend antes de permitir `PUT /api/Allergens/User`

## 12. Nota de codificacion (UTF-8)

Para textos con tildes (`Configuración`, `Lácteos`, `alérgenos`):
1. Mantener archivos de vista/script/documentación en UTF-8.
2. Si aparece mojibake (`Configuración`, `Lácteos`), el frontend puede enviar valores rotos.
3. El backend ahora tolera parte de estos casos, pero la corrección principal debe hacerse en los archivos fuente.
