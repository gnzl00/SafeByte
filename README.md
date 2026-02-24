# SafeByte

Aplicacion web ASP.NET Core MVC + API para recomendar comidas seguras segun alergenos del usuario.

## Estado actual (resumen rapido)
- Backend: ASP.NET Core 8.
- Base de datos: Firebase Firestore (no MySQL/XAMPP).
- Frontend: Razor + JavaScript plano en `wwwroot/src/ventanas`.
- Login/registro: API en `AuthController`.
- Alergenos por usuario: API en `AllergensController`, persistidos en Firestore.
- Textos y formularios de usuario corregidos en UTF-8 para evitar errores de tildes (`Lácteos`, `Configuración`, etc.).

## Cambio de arquitectura (antes vs ahora)
- Antes: almacenamiento local temporal en navegador para alergenos.
- Ahora: alergenos guardados en Firestore por usuario (`users/{email}`), con lectura y escritura via API.
- Resultado: las preferencias no se pierden al cerrar sesion o cambiar de dispositivo (si el usuario inicia sesion con su cuenta).

## Ejecucion rapida
1. Instala `.NET 8 SDK`.
2. Configura Firestore y credenciales (ver `docs/01-setup.md`).
3. Ejecuta:
```bash
dotnet restore
dotnet run
```
4. Abre:
- `http://localhost:5113`
- `https://localhost:7113`

## Endpoints principales
- `POST /api/Auth/Register`
- `POST /api/Auth/Login`
- `GET /api/Allergens/Catalog`
- `GET /api/Allergens/User?email=usuario@dominio.com`
- `PUT /api/Allergens/User`

## Documentacion detallada
- Setup completo: [docs/01-setup.md](docs/01-setup.md)
- Estructura y flujos: [docs/02-estructura-y-flujos.md](docs/02-estructura-y-flujos.md)
- MVC de alergenos y persistencia: [docs/03-alergenos-mvc-y-persistencia.md](docs/03-alergenos-mvc-y-persistencia.md)
