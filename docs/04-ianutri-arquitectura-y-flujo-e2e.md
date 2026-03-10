# 04 - IANutri: arquitectura, implementacion y decisiones E2E

Fecha de referencia: 2026-03-09.

Este documento describe en detalle el modulo IANutri desde backend, frontend, persistencia, seguridad de salida y decisiones tecnicas.

## 1. Objetivo de producto del modulo

IANutri existe para convertir una peticion informal del usuario en una ayuda de cocina accionable y segura respecto a alergenos.

Capacidades actuales:

1. Reformular peticion en lenguaje operativo.
2. Generar entre 1 y 3 sugerencias de receta con alertas/sustituciones.
3. Abrir asistente de cocina paso a paso para una receta concreta.
4. Guardar y recuperar historial por usuario.

## 2. Mapa de componentes

### 2.1 Backend

1. `Controllers/IANutriController.cs`
- Valida request basica.
- Normaliza modo y email.
- Resuelve alergenos remotos con fallback.
- Orquesta llamadas al servicio IA.
- Persiste historial en Firestore.

2. `Services/IIANutriService.cs`
- Contrato de servicio desacoplado para test/mocks.

3. `Services/IANutriService.cs`
- Construccion de prompts.
- Llamada HTTP al proveedor.
- Fallback de modelos.
- Parseo JSON y saneo defensivo.
- Fallback de contenido si el modelo falla.

4. `Services/IANutriOptions.cs`
- Config tipada: `BaseUrl`, `ApiKey`, modelos, timeout.

5. `Models/IANutriRequests.cs` y `Models/IANutriResponses.cs`
- DTOs para API y persistencia de historial.

### 2.2 Frontend

1. `Views/Home/IANutri.cshtml`
- Estructura visual de planner, resultado, asistente e historial.

2. `wwwroot/src/ventanas/ianutri.js`
- Estado cliente.
- Eventos de UI.
- Llamadas API.
- Render de resultados y recuperacion de historial.

3. `wwwroot/src/css/ianutri.css`
- Estilos del modulo (chips, estados, tarjetas, alertas).

## 3. Diseno de API y responsabilidades

Base route: `/api/IANutri`.

### 3.1 `POST /Reformulate`

Responsabilidad:
- Convertir texto libre en prompt operativo limpio y util para siguiente fase.

Por que existe endpoint separado:

1. Da feedback temprano al usuario.
2. Permite corregir antes de gastar tokens grandes de sugerencias.
3. Mejora trazabilidad de intencion.

### 3.2 `POST /GenerateSuggestions`

Responsabilidad:
- Generar resumen + recetas + advertencias/sustituciones.
- Persistir historial y devolver `historyId`.

Decision:
- Persistir justo al generar sugerencias (no al abrir asistente).

Motivo:
- El valor principal ya existe en esta fase.
- El asistente se trata como enriquecimiento posterior.

### 3.3 `POST /CookingAssistant`

Responsabilidad:
- Generar guia operativa para una receta seleccionada.
- Si hay `historyId`, actualizar historial con bloque `assistant`.

Decision:
- Si falla update de historial, no bloquear respuesta al usuario.

Motivo:
- Priorizar continuidad de experiencia de cocina.

### 3.4 `GET /History` y `DELETE /History`

Responsabilidad:
- Recuperar hasta 40 conversaciones recientes.
- Borrar historial completo del usuario.

Decision:
- Limite fijo de 40 para evitar payloads excesivos.

## 4. Flujo E2E completo

### 4.1 Inicio

1. Front carga `state` y bind de nodos DOM.
2. Resuelve alergenos (`/api/Allergens/User` con fallback cache).
3. Carga historial remoto si hay sesion.

### 4.2 Reformulacion

1. Usuario escribe input y selecciona modo.
2. Front valida que hay texto y modo.
3. Envia payload a `/Reformulate`.
4. Backend:
- normaliza modo,
- resuelve alergenos remotos si puede,
- llama a `IANutriService.ReformulateAsync`.
5. Front muestra bloque "Asi he entendido tu peticion".

### 4.3 Sugerencias

1. Front llama `/GenerateSuggestions`.
2. Backend llama `GenerateSuggestionsAsync`.
3. Servicio:
- parsea JSON,
- sanea campos,
- aplica conflictos de alergenos,
- completa fallback si faltan piezas.
4. Controller guarda historial en Firestore y devuelve `historyId`.
5. Front renderiza resumen, alertas globales y tarjetas de receta.

### 4.4 Asistente

1. Usuario pulsa tarjeta de receta.
2. Front llama `/CookingAssistant` con `historyId` + receta.
3. Servicio genera intro, items, pasos y notas de seguridad.
4. Controller intenta actualizar historial con bloque `assistant`.
5. Front muestra card de asistente con highlight y scroll.

### 4.5 Historial

1. `GET /History` devuelve lista normalizada de conversaciones.
2. Front permite restaurar una conversacion al estado actual.
3. `DELETE /History` elimina documentos de subcoleccion.

## 5. Logica interna de `IANutriService`

### 5.1 Estrategia de prompts

Cada fase define `systemPrompt` estricto con:

1. Respuesta obligatoria en espanol.
2. Formato JSON exacto.
3. Restricciones de seguridad por alergenos.
4. Prohibiciones de ruido (markdown, metadatos tecnicos).

Razon:
- Reducir variabilidad y parsing roto.

### 5.2 Saneo y parseo defensivo

Tecnicas usadas:

1. `ExtractJsonObject`:
- recorta fences markdown,
- extrae bloque entre primera y ultima llave.

2. `TryParseModelJson<T>`:
- deserializa con `PropertyNameCaseInsensitive`.
- si falla, devuelve `default` y activa fallback.

3. `NormalizeReformulatedPrompt`:
- elimina listas, tokens de modelo (`gpt`, `openai`, etc), lineas no utiles.

4. Sanitizacion general:
- `SanitizeInput` para limitar longitud y remover saltos conflictivos.

### 5.3 Fallback de contenido

Si el modelo no devuelve estructura valida:

1. Reformulacion:
- `BuildFallbackReformulatedPrompt`.

2. Sugerencias:
- `BuildFallbackSuggestion`.

3. Asistente:
- usa pasos/ingredientes de receta origen y notas de seguridad base.

Razon:
- Nunca dejar UI vacia por fallo de formato.

### 5.4 Conflictos de alergenos

`ApplyAllergenConflicts`:

1. Cruza `allergensDetected` vs alergenos del usuario.
2. Si hay conflicto y falta warning, lo crea.
3. Si faltan sustituciones, agrega recomendacion segura.

Razon:
- Segunda capa de seguridad aunque el modelo omita alertas.

## 6. Resolucion de modelos y API key

### 6.1 Candidatos de modelo

`BuildModelCandidates` genera lista:

1. Modelo configurado.
2. Fallbacks por fase.
3. Variante con y sin prefijo `openai/`.

Razon:
- Mitigar errores `unknown_model` por naming distinto entre proveedores.

### 6.2 Orden de API key

`ResolveApiKey` cambia segun endpoint:

1. Si `IANutri:ApiKey` esta definido, prioridad maxima.
2. Si endpoint es GitHub Models:
- `GITHUB_MODELS_API_KEY`
- `GITHUB_TOKEN`
- `IANUTRI_API_KEY`
- `OPENAI_API_KEY`
3. Si endpoint no es GitHub Models:
- `OPENAI_API_KEY`
- `IANUTRI_API_KEY`
- `GITHUB_MODELS_API_KEY`
- `GITHUB_TOKEN`

Razon:
- Facilitar failover operacional sin cambios de codigo.

### 6.3 Tratamiento de errores de proveedor

`SendCompletionAsync`:

1. Si HTTP no exitoso y es `unknown_model`, prueba siguiente candidato.
2. Si otro error, lanza detalle resumido de payload.
3. Si no hay contenido util, lanza error controlado.

## 7. Persistencia de historial en Firestore

Ruta:
- `users/{email}/ianutriHistory/{historyId}`

### 7.1 Guardado inicial

`SaveHistoryAsync` persiste:

1. `userInput`, `option`, `reformulatedPrompt`, `summary`.
2. `allergens`, `globalWarnings`, `generalSubstitutions`.
3. `suggestions` serializadas.
4. `createdAt`, `updatedAt`.

### 7.2 Enriquecimiento posterior

`UpdateHistoryWithAssistantAsync` actualiza:

1. Bloque `assistant` (intro, requiredItems, stepByStep, safetyNotes).
2. `updatedAt`.

Decision:
- Fallo de update se ignora para no cortar experiencia.

## 8. Estado y UX del frontend (`ianutri.js`)

`state` central:

1. `allergens`
2. `selectedMode`
3. `reformulatedPrompt`
4. `suggestions`
5. `activeHistoryId`
6. `history`

Decisiones UX destacadas:

1. No permitir generar sugerencias hasta tener reformulacion.
2. Mostrar mensajes por estado (`info`, `success`, `warning`, `error`).
3. Asistente incrustado bajo resultados para continuidad visual.
4. Historial dentro de `<details>` para reducir ruido en pantalla.

## 9. Seguridad y calidad de salida

Controles implementados:

1. Validacion de request en controller.
2. Filtro de texto sensible a menciones de proveedor/modelo.
3. Normalizacion y deduplicado de arrays.
4. Capado de longitudes para evitar payloads descontrolados.
5. Fallback seguro cuando IA no cumple contrato.

Lo que aun no cubre:

1. Moderacion avanzada de contenido.
2. Guardrails semanticos de alto nivel (policy engine).
3. Rate limiting por usuario en endpoints IA.

## 10. Observabilidad operativa del modulo

Estado actual:

1. Errores se propagan como `InvalidOperationException` -> `503` en controller.
2. Historial y borrado devuelven mensajes detallados de fallo.
3. Sin metricas nativas de latencia por endpoint/modelo.

Siguiente fase recomendada:

1. Medir p50/p95/p99 por endpoint IA.
2. Registrar `modelUsed` en logs internos (no en UI).
3. Trazar ratio de fallback vs respuesta estructurada valida.

## 11. Tradeoffs explicitos

1. Se priorizo resiliencia de UX frente a pureza de arquitectura.
2. Se prefirio parser tolerante frente a rechazo estricto de salidas IA.
3. Se mantiene historial simple por usuario en vez de analitica global avanzada.
4. Se asume deuda de auth robusta para centrarse en valor funcional.

## 12. Pruebas manuales recomendadas

1. Flujo completo con usuario autenticado:
- reformular,
- generar,
- abrir asistente,
- restaurar historial,
- borrar historial.

2. Caso sin usuario (`sb_user` ausente):
- verificar fallback local de alergenos.

3. Caso de fallo de proveedor IA:
- comprobar mensajes de error y fallback visual.

4. Caso de conflicto de alergenos:
- confirmar warnings y sustituciones automaticas.

## 13. Referencias

1. `Controllers/IANutriController.cs`
2. `Services/IANutriService.cs`
3. `Views/Home/IANutri.cshtml`
4. `wwwroot/src/ventanas/ianutri.js`
