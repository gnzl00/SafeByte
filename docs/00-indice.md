# 00 - Indice de documentacion

Ultima actualizacion: 2026-03-09

Este indice organiza la documentacion tecnica, funcional y operativa del proyecto SafeByte.

## 1. Lectura recomendada por perfil

## 1.1 Si vas a desarrollar codigo

1. `README.md`
2. `docs/01-setup.md`
3. `docs/02-estructura-y-flujos.md`
4. `docs/03-alergenos-mvc-y-persistencia.md`
5. `docs/04-ianutri-arquitectura-y-flujo-e2e.md`

## 1.2 Si vas a desplegar y operar

1. `README.md`
2. `DEPLOY.md`
3. `docs/06-render-checklist.md`
4. `docs/05-pendientes-despliegue-cloud.md`

## 1.3 Si necesitas entender decisiones tecnicas

1. `README.md` (vision y decisiones globales)
2. `docs/02-estructura-y-flujos.md` (arquitectura por capas)
3. `docs/03-alergenos-mvc-y-persistencia.md` (dominio de seguridad alimentaria)
4. `docs/04-ianutri-arquitectura-y-flujo-e2e.md` (modulo IA y tradeoffs)

## 2. Mapa completo de documentos

1. `README.md`
- Resumen ejecutivo, stack, decisiones tecnicas, estado actual y rutas de entrada.

2. `docs/01-setup.md`
- Onboarding local completo, prerequisitos, variables y troubleshooting.

3. `docs/02-estructura-y-flujos.md`
- Arquitectura de sistema, capas, flujos E2E y deuda tecnica.

4. `docs/03-alergenos-mvc-y-persistencia.md`
- Modelo de alergenos, contratos API, normalizacion y persistencia en Firestore.

5. `docs/04-ianutri-arquitectura-y-flujo-e2e.md`
- Pipeline IANutri completo: prompts, parseo, fallback, historial y UX.

6. `DEPLOY.md`
- Guia de despliegue cloud multi proveedor con seguridad minima.

7. `docs/05-pendientes-despliegue-cloud.md`
- Pendientes manuales fuera de codigo y roadmap tecnico.

8. `docs/06-render-checklist.md`
- Checklist operativo corto para primer deploy en Render.

## 3. Convenciones de documentacion del proyecto

1. Se documenta el estado real del codigo, no un estado ideal.
2. Se explican decisiones con motivo y coste, no solo "que hace".
3. Se diferencian claramente:
- implementado,
- limitaciones,
- proximos pasos.
4. Todas las fechas se expresan en formato ISO (`YYYY-MM-DD`).

## 4. Cambios recientes reflejados en la documentacion

1. Menu de usuario en navbar usando `username` y `email` de `sb_user`.
2. Endurecimiento del scanner movil para evitar bloqueos por streams abiertos.
3. Consolidacion de decision tecnica sobre GitHub Models como proveedor IA por defecto.