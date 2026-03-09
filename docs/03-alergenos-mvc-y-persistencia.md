# 03 - MVC de alergenos y persistencia

## 0. Contexto de migracion (Apache/XAMPP -> Firebase)

Segun el flujo historico del proyecto:
- fase antigua: pruebas locales con Apache/XAMPP en puerto local
- fase actual: backend ASP.NET Core con persistencia en Firebase Firestore

Este documento refleja la fase actual y deja claro que la fuente de verdad ya no es una sesion local del navegador para alergenos.

## 1. Que problema habia

Antes, las preferencias de alergenos se guardaban solo en navegador:
- `localStorage`
- sin persistencia real por usuario en base de datos

Consecuencia:
- si el usuario cambiaba de dispositivo o navegador, perdia preferencias
- no habia fuente unica de verdad en backend

## 2. Que se ha implementado

Se ha creado un flujo MVC completo para alergenos, conectado a Firestore:

1. Modelos:
- `Models/UpdateUserAllergensRequest.cs`
- `Models/UserAllergenPreferencesResponse.cs`
- `Services/AllergenCatalog.cs` (catalogo y normalizacion)

2. Controlador:
- `Controllers/AllergensController.cs`

3. Vista y cliente:
- `Views/Home/Home.cshtml` (UI checkboxes)
- `wwwroot/src/ventanas/Home.js` (cargar/guardar)
- `wwwroot/src/ventanas/comida.js` (filtrar recetas usando preferencias persistidas)

4. Integracion de autenticacion:
- `Controllers/AuthController.cs` ahora crea usuarios con `allergens: []`
- login devuelve alergenos actuales
- `wwwroot/src/ventanas/index.js` guarda usuario actual (`sb_user`)

## 3. Catalogo de alergenos soportado

Catalogo actual validado por backend:
1. Gluten
2. Lácteos
3. Huevo
4. Frutos secos
5. Mariscos
6. Soja

Nota:
- el backend normaliza mayusculas/minusculas y tildes.
- si llega un alergeno fuera de catalogo, devuelve error 400.

## 4. Esquema Firestore usado

Coleccion:
- `users`

Documento:
- ID = email normalizado (minusculas, sin espacios)

Campos de alergenos:
- `allergens` (array de string)
- `allergensUpdatedAt` (timestamp)

Ejemplo:
```json
{
  "username": "pepe",
  "email": "pepe@correo.com",
  "passwordHash": "....",
  "createdAt": "<timestamp>",
  "allergens": ["Gluten", "Mariscos"],
  "allergensUpdatedAt": "<timestamp>"
}
```

## 5. API de alergenos (contratos)

### 5.1 GET catalogo

`GET /api/Allergens/Catalog`

Respuesta:
```json
{
  "allergens": ["Gluten", "Lácteos", "Huevo", "Frutos secos", "Mariscos", "Soja"]
}
```

### 5.2 GET alergenos de usuario

`GET /api/Allergens/User?email=usuario@correo.com`

Respuesta:
```json
{
  "email": "usuario@correo.com",
  "allergens": ["Gluten", "Soja"],
  "updatedAtUtc": "2026-02-24T10:12:30.0000000Z"
}
```

Errores:
- `400` email invalido
- `404` usuario no encontrado

### 5.3 PUT alergenos de usuario

`PUT /api/Allergens/User`

Body:
```json
{
  "email": "usuario@correo.com",
  "allergens": ["gluten", "Lácteos"]
}
```

Comportamiento:
- normaliza valores (`gluten` -> `Gluten`, `Lácteos` -> `Lácteos`)
- valida catalogo
- guarda en Firestore

Respuesta:
```json
{
  "email": "usuario@correo.com",
  "allergens": ["Gluten", "Lácteos"],
  "updatedAtUtc": "2026-02-24T10:13:05.0000000Z"
}
```

Error si hay valores invalidos:
```json
{
  "message": "Se enviaron alérgenos no válidos.",
  "invalidAllergens": ["Pimienta"],
  "allowedAllergens": ["Gluten", "Lácteos", "Huevo", "Frutos secos", "Mariscos", "Soja"]
}
```

## 6. Flujo completo de extremo a extremo

### Caso A: usuario logueado

1. Hace login en `Index`.
2. `index.js` guarda `sb_user` en localStorage.
3. En `Home`, `Home.js` llama `GET /api/Allergens/User`.
4. Marca checkboxes con los alergenos recibidos.
5. Al guardar, llama `PUT /api/Allergens/User`.
6. `comida.js` vuelve a cargar preferencias y filtra recetas.

### Caso B: acceso sin autenticacion

1. El flujo de pantalla inicial ya no ofrece `Skip Login`.
2. Si no hay usuario valido en `sb_user`, no se considera un flujo soportado para persistencia remota.
3. Para guardar en Firestore, el usuario debe iniciar sesion.

## 7. Archivos tocados y para que sirve cada uno

Backend:
- `Controllers/AllergensController.cs`: endpoints de lectura/escritura de alergenos.
- `Services/AllergenCatalog.cs`: normaliza y valida alergenos permitidos.
- `Models/UpdateUserAllergensRequest.cs`: DTO de entrada para `PUT`.
- `Models/UserAllergenPreferencesResponse.cs`: DTO de salida.
- `Controllers/AuthController.cs`: inicializa y devuelve alergenos en auth.
- `Program.cs`: seed incluye campos de alergenos.

Frontend:
- `wwwroot/src/ventanas/index.js`: guarda sesion del usuario para identificar preferencias (sin modo invitado desde pantalla inicial).
- `wwwroot/src/ventanas/Home.js`: carga y guarda preferencias en backend.
- `wwwroot/src/ventanas/comida.js`: filtra recetas con preferencias persistidas.
- `Views/Home/Home.cshtml`: limpia JS inline duplicado y delega al script principal.

## 8. Como probar manualmente (checklist)

1. Arranca app con `dotnet run`.
2. Registra usuario nuevo.
3. Inicia sesion con ese usuario.
4. Ve a `Configuracion de usuario`.
5. Marca 2-3 alergenos y guarda.
6. Verifica en Firestore que el documento del usuario actualizo `allergens`.
7. Cierra sesion, vuelve a iniciar y revisa que checkboxes siguen marcados.
8. Ve a `Comidas` y confirma que recetas con esos alergenos desaparecen.

## 9. Troubleshooting rapido

No se guardan alergenos:
1. Revisa consola red (peticion `PUT /api/Allergens/User`).
2. Revisa respuesta backend (400/404/500).
3. Verifica que existe `sb_user` en localStorage.

Error concreto con `Lácteos`:
1. Causa detectada: texto mojibake por codificación en la etiqueta de `Lácteos`.
2. Efecto: el backend lo interpretaba como valor no válido y devolvía `400`.
3. Solución aplicada:
- vista corregida a texto UTF-8 correcto (`Lácteos`)
- normalización backend endurecida para ignorar puntuación/artefactos de codificación heredada y mapear correctamente a `Lácteos`
4. Prevención:
- guardar `.cshtml`, `.js`, `.md` siempre en UTF-8
- evitar editores/configuraciones que reinterpreten UTF-8 como Latin-1/Windows-1252

Marca alergenos pero no filtra comidas:
1. Abre `Comidas` despues de guardar.
2. Verifica que `comida.js` recibe array no vacio desde API.
3. Comprueba que los nombres de alergenos coinciden exactamente.

Usuario no encontrado en API de alergenos:
1. Revisa email normalizado (minusculas).
2. Comprueba que el documento `users/{email}` existe en Firestore.

## 10. Limites de la implementacion actual

1. Seguridad basica (sin JWT/Firebase Auth).
2. Email llega desde cliente y se usa como identificador.

Para produccion real:
1. usar autenticacion fuerte (token firmado)
2. validar identidad del token en backend
3. no aceptar email libre en body/query


