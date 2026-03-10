# 03 - Dominio de alergenos: MVC, persistencia y decisiones

Fecha de referencia: 2026-03-09.

Este documento describe de forma detallada como SafeByte modela, valida y usa los alergenos del usuario a lo largo de toda la aplicacion.

## 1. Problema funcional que resuelve este modulo

El proyecto necesita una fuente de verdad unica para restricciones alimentarias del usuario.

Riesgo previo (antes de consolidar backend):

1. Preferencias guardadas solo en navegador.
2. Perdida al cambiar de dispositivo o limpiar datos.
3. Inconsistencia entre pantallas (Home, Comidas, IANutri).

Objetivo actual:

1. Persistir alergenos por usuario en Firestore.
2. Validar en backend con reglas unicas.
3. Reutilizar esas preferencias en todos los flujos de producto.

## 2. Implementacion actual (resumen ejecutivo)

Piezas principales:

1. Catalogo y normalizacion:
- `Services/AllergenCatalog.cs`

2. Contratos:
- `Models/UpdateUserAllergensRequest.cs`
- `Models/UserAllergenPreferencesResponse.cs`

3. API:
- `Controllers/AllergensController.cs`

4. Integracion auth:
- `Controllers/AuthController.cs`

5. UI y consumo:
- `wwwroot/src/ventanas/Home.js`
- `wwwroot/src/ventanas/comida.js`
- `wwwroot/src/ventanas/ianutri.js`

## 3. Decision central: catalogo cerrado

Catalogo canonico en backend:

1. `Gluten`
2. `Lacteos`
3. `Huevo`
4. `Frutos secos`
5. `Mariscos`
6. `Soja`

### 3.1 Por que catalogo cerrado

1. Evita errores de escritura y sinonimos inconsistentes.
2. Permite validacion estricta en API.
3. Permite filtros de recetas predecibles.
4. Simplifica UX al mostrar opciones fijas.

### 3.2 Coste de la decision

1. Menor flexibilidad para casos especiales.
2. Cualquier nuevo alergeno requiere cambio coordinado en backend+frontend+docs.

## 4. Normalizacion de entradas (`AllergenCatalog`)

`AllergenCatalog.NormalizeMany(...)` aplica:

1. `trim + lowercase`.
2. Remocion de diacriticos (formato Unicode FormD).
3. Conserva letras, digitos y espacios.
4. Colapsa espacios repetidos.
5. Mapea llave normalizada a valor canonico.
6. Deduplica sin importar mayusculas/minusculas.
7. Devuelve lista de invalidos por separado.

Resultado:
- Entradas como `gluten`, `GLUTEN`, `Gluten` convergen a `Gluten`.
- Entradas fuera de catalogo quedan en `invalidAllergens`.

## 5. Modelo de datos en Firestore

### 5.1 Documento de usuario

Ruta:
- `users/{normalizedEmail}`

Campos de este modulo:

1. `allergens`: array de strings canonicos.
2. `allergensUpdatedAt`: timestamp UTC.

### 5.2 Por que embebido en el usuario

Decision:
- Guardar alergenos en el mismo documento del usuario, no en coleccion separada.

Motivo:

1. Lectura de perfil en una sola llamada.
2. Menos round-trips para login y carga inicial.
3. Menor complejidad de consultas.

Coste:

1. Menos flexible para versionado historico de preferencias.
2. Escalar auditoria requiere estructura adicional.

## 6. Contratos API detallados

Base route: `/api/Allergens`.

### 6.1 `GET /Catalog`

Uso:
- Obtener lista oficial de alergenos soportados.

Respuesta tipo:

```json
{
  "allergens": ["Gluten", "Lacteos", "Huevo", "Frutos secos", "Mariscos", "Soja"]
}
```

### 6.2 `GET /User?email=...`

Uso:
- Cargar preferencias de un usuario.

Validaciones:

1. Email no vacio.
2. Usuario existente en Firestore.

Respuestas:

1. `200 OK` con DTO:

```json
{
  "email": "usuario@dominio.com",
  "allergens": ["Gluten", "Soja"],
  "updatedAtUtc": "2026-03-09T12:34:56Z"
}
```

2. `400 BadRequest` si email invalido.
3. `404 NotFound` si usuario no existe.

### 6.3 `PUT /User`

Uso:
- Persistir preferencias del usuario.

Request:

```json
{
  "email": "usuario@dominio.com",
  "allergens": ["gluten", "Lacteos"]
}
```

Comportamiento:

1. Normaliza email.
2. Normaliza alergenos.
3. Si hay invalidos, responde `400` con detalle.
4. Si usuario no existe, responde `404`.
5. Si todo es valido, actualiza documento y timestamp.

Error de validacion tipo:

```json
{
  "message": "Se enviaron alergenos no validos.",
  "invalidAllergens": ["Pimienta"],
  "allowedAllergens": ["Gluten", "Lacteos", "Huevo", "Frutos secos", "Mariscos", "Soja"]
}
```

## 7. Integracion con autenticacion

`AuthController` incorpora este dominio en registro/login:

1. Registro crea usuario con `allergens = []`.
2. Login devuelve `allergens` normalizados.

Decision:
- Devolver alergenos en login para bootstrap inmediato del frontend.

Motivo:
- Menor latencia percibida al entrar a Home/Comidas/IANutri.

## 8. Integracion frontend por pantalla

### 8.1 Login (`index.js`)

1. Persiste `sb_user` con email/username.
2. Guarda cache de alergenos en:
- `sb_alergenos`
- `alergenosSeleccionados` (compatibilidad legacy)

### 8.2 Home (`Home.js`)

1. Carga remota desde API cuando hay usuario.
2. Si falla API, usa cache local.
3. Guarda remoto con `PUT /api/Allergens/User`.
4. Refleja estado en checkboxes y mensaje de estado.

### 8.3 Comidas (`comida.js`)

1. Lee alergenos de API/cache.
2. Excluye recetas con conflicto directo.

### 8.4 IANutri (`ianutri.js`)

1. Resuelve alergenos al iniciar modulo.
2. Los envia en reformulacion/sugerencias/asistente.
3. Backend vuelve a resolver remoto para evitar depender solo del cliente.

## 9. Decisiones de resiliencia

1. Fallback local cuando no hay red o falla Firestore.
2. Cache duplicada (`sb_alergenos` + legacy) para no romper pantallas antiguas.
3. Normalizacion en ambos lados:
- frontend evita duplicados simples,
- backend impone canon definitivo.

## 10. Riesgos y limitaciones actuales

1. Identidad por email enviado desde cliente.
2. Sin token de sesion firmado.
3. Favoritos e historial scanner siguen en localStorage (no remotos).
4. Catalogo no configurable sin despliegue.

## 11. Mejoras recomendadas (roadmap)

1. Migrar a autenticacion real (JWT o Firebase Auth) y derivar email del token.
2. Versionar preferencias (quien, cuando, cambio) para auditoria.
3. Exponer endpoint de catalogo administrable con feature flag.
4. Sincronizar favoritos y scanner en backend por usuario.

## 12. Checklist de pruebas manuales

1. Registrar usuario nuevo y verificar `allergens=[]` en Firestore.
2. Guardar 2 alergenos desde Home y confirmar respuesta `200`.
3. Recargar Home y comprobar persistencia visual.
4. Ir a Comidas y validar que platos en conflicto desaparecen.
5. Ir a IANutri y validar chips de alergenos cargados.
6. Enviar alergeno invalido por API y confirmar `400` con detalle.

## 13. Referencias

1. `Controllers/AllergensController.cs`
2. `Services/AllergenCatalog.cs`
3. `wwwroot/src/ventanas/Home.js`
4. `wwwroot/src/ventanas/comida.js`
5. `wwwroot/src/ventanas/ianutri.js`
