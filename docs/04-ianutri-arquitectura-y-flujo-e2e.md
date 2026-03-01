# 04 - IANutri (documentacion unificada)

Ultima actualizacion: 2026-02-25

Este documento unifica arquitectura, flujo funcional, contratos API, frontend/UX, operacion y bitacora tecnica del modulo IANutri.

## 1. Objetivo y alcance

IANutri permite:
1. Reformular la peticion del usuario con reglas de seguridad alimentaria.
2. Generar 1-3 sugerencias de platos considerando alergenos.
3. Guiar al usuario en cocina paso a paso para una receta seleccionada.
4. Persistir historial por usuario y permitir restaurarlo o borrarlo.

## 2. Mapa de componentes

### 2.1 Backend

1. `Controllers/IANutriController.cs`
- Endpoints de reformulacion, sugerencias, asistente e historial.
- Normalizacion de email/modo.
- Lectura de alergenos remotos con fallback.
- Guardado de historial en Firestore.

2. `Services/IIANutriService.cs`
- Contrato del servicio IA.

3. `Services/IANutriService.cs`
- Prompting de sistema.
- Llamadas HTTP al proveedor de modelos.
- Fallback de modelos ante `unknown_model`.
- Parseo de JSON del modelo.
- Saneo defensivo de respuestas.

4. `Services/IANutriOptions.cs`
- Configuracion tipada (`BaseUrl`, `ApiKey`, modelos, `TimeoutSeconds`).

5. `Models/IANutriRequests.cs`, `Models/IANutriResponses.cs`
- DTOs de entrada/salida e historial.

6. `Program.cs`
- Registro DI:
  - `Configure<IANutriOptions>`
  - `AddHttpClient<IIANutriService, IANutriService>`

### 2.2 Frontend

1. `Views/Home/IANutri.cshtml`
- Layout del modulo y bloques de UI.

2. `wwwroot/src/ventanas/ianutri.js`
- Estado cliente.
- Eventos y llamadas API.
- Render de reformulacion, resultados, historial y asistente.

3. `wwwroot/src/css/ianutri.css`
- Estilo visual.
- Semantica de color:
  - positivo: verde
  - warning: naranja
  - riesgo: rojo

## 3. Flujo extremo a extremo

1. Carga inicial:
- Se cargan alergenos del usuario (`/api/Allergens/User`) o cache local.
- Se carga historial (`/api/IANutri/History`).

2. Reformulacion:
- El usuario escribe peticion y selecciona modo.
- Front llama `POST /api/IANutri/Reformulate`.
- Backend genera `reformulatedPrompt` y lo sanea.
- Front lo muestra en "Asi he entendido tu peticion".

3. Generacion de sugerencias:
- Front llama `POST /api/IANutri/GenerateSuggestions`.
- Backend genera resumen + recetas + warnings/sustituciones.
- Backend persiste la conversacion y devuelve `historyId`.
- Front renderiza cards con tiempo estimado y dificultad.

4. Asistente de cocina:
- Al pulsar receta, front llama `POST /api/IANutri/CookingAssistant`.
- Muestra guia con materiales, pasos y notas de seguridad.
- Se actualiza historial con bloque `assistant`.

5. Historial:
- UI compacta desplegable al final.
- Se puede restaurar una conversacion completa.
- Se puede borrar historial sin `alert/confirm` externo; solo aviso inline.

## 4. API y contratos

Base route: `/api/IANutri`

### 4.1 `POST /Reformulate`

Request:
```json
{
  "email": "usuario@dominio.com",
  "userInput": "Tengo pollo, arroz y verduras",
  "option": "cena-ligera",
  "allergens": ["Gluten", "Lacteos"]
}
```

Response (resumen):
```json
{
  "optionLabel": "Cena ligera",
  "reformulatedPrompt": "Genera una cena ligera con pollo, arroz y verduras evitando gluten y lacteos.",
  "allergens": ["Gluten", "Lacteos"],
  "notes": ["Enfoque: ligero"]
}
```

### 4.2 `POST /GenerateSuggestions`

Request:
```json
{
  "email": "usuario@dominio.com",
  "userInput": "Quiero una pizza para cenar",
  "option": "Cena ligera",
  "reformulatedPrompt": "Genera alternativas ligeras tipo pizza sin gluten ni lacteos.",
  "allergens": ["Gluten", "Lacteos"]
}
```

Response (resumen):
```json
{
  "historyId": "abc123",
  "summary": "Opciones ligeras y seguras para tus restricciones.",
  "globalWarnings": ["Revisa trazas y contaminacion cruzada para: Gluten, Lacteos."],
  "generalSubstitutions": ["Usa versiones certificadas sin alergenos y revisa etiquetas antes de cocinar."],
  "suggestions": []
}
```

### 4.3 `POST /CookingAssistant`

Request:
```json
{
  "email": "usuario@dominio.com",
  "allergens": ["Gluten"],
  "historyId": "abc123",
  "recipe": {
    "title": "Pizza vegetal sin gluten",
    "description": "Base sin gluten con verduras",
    "estimatedTime": "35 min",
    "difficulty": "Media",
    "ingredients": ["Base sin gluten"],
    "steps": ["Precalienta horno"],
    "allergensDetected": [],
    "safeSubstitutions": [],
    "allergyWarning": ""
  }
}
```

Response (resumen):
```json
{
  "recipeTitle": "Pizza vegetal sin gluten",
  "intro": "Vamos a cocinar Pizza vegetal sin gluten paso a paso.",
  "requiredItems": ["Tabla de cortar"],
  "stepByStep": ["Lava y corta verduras"],
  "safetyNotes": ["Evita contacto con: Gluten."]
}
```

### 4.4 Historial

1. `GET /History?email=...`
- Devuelve hasta 40 conversaciones.

2. `DELETE /History?email=...`
- Borra historial del usuario.

## 5. Persistencia en Firestore

1. Coleccion de usuarios: `users/{email}`
2. Subcoleccion historial IA: `users/{email}/ianutriHistory/{historyId}`

Campos de historial relevantes:
1. `userInput`
2. `option`
3. `reformulatedPrompt`
4. `summary`
5. `allergens`
6. `globalWarnings`
7. `generalSubstitutions`
8. `suggestions`
9. `assistant` (si se genero)
10. `createdAt`, `updatedAt`

## 6. Reglas de consistencia y seguridad

1. Prompt de sistema estricto en reformulacion.
2. Prohibicion de menciones de modelo/proveedor en output visible.
3. Saneo backend de `reformulatedPrompt`.
4. Saneo frontend de historico antiguo.
5. Aplicacion de conflictos de alergenos y sustituciones seguras.

## 7. Configuracion

Bloque `IANutri` en `appsettings`:
```json
"IANutri": {
  "BaseUrl": "https://models.inference.ai.azure.com",
  "ApiKey": "",
  "ReformulationModel": "gpt-4.1-nano",
  "SuggestionModel": "gpt-4.1",
  "CookingAssistantModel": "gpt-4.1",
  "TimeoutSeconds": 60
}
```

Fallback de API key:
1. `IANutri:ApiKey`
2. `IANUTRI_API_KEY`
3. `GITHUB_MODELS_API_KEY`
4. `GITHUB_TOKEN`
5. `OPENAI_API_KEY`

## 8. Troubleshooting rapido

1. `unknown_model`:
- Revisar modelos configurados y permisos de la key.
- El servicio ya prueba variantes con/sin `openai/`.

2. `No se encontro API key para IANutri`:
- Configurar `IANutri:ApiKey` o variables de entorno.

3. `Host desconocido` (Firestore):
- Revisar red, DNS, credenciales y `Firestore:ProjectId`.

4. `address already in use`:
- Cerrar instancia previa o cambiar `applicationUrl`.

5. Build bloqueado por `SafeByte.exe`:
```powershell
Get-Process SafeByte -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build
```

## 9. Bitacora resumida de implementacion

1. Se implemento modulo IANutri completo (backend + frontend + estilos).
2. Se anadio persistencia de historial en Firestore.
3. Se mejoro UX:
- historial compacto desplegable
- aviso inline al borrar historial
- mejor visibilidad de asistente al pulsar receta
- reformulacion visible para el usuario
4. Se reforzo consistencia de respuestas con prompt estricto y saneo doble.

## 10. Checklist de smoke test

1. Abrir `IANutri`.
2. Verificar carga de alergenos.
3. Escribir peticion + seleccionar modo.
4. Confirmar bloque de reformulacion visible.
5. Generar sugerencias y revisar colores/alertas.
6. Pulsar receta y validar asistente de cocina.
7. Guardar, recuperar y borrar historial.
