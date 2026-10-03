# Decisiones duraderas

## ADR-001 — Monorepo — 2026-10-03
Estado: adoptado.
Frontend, backend, apps nativas, infraestructura, agentes y documentación viven en YeinsM/BillingRD.
Motivo: cambios verticales coordinados, contratos trazables y un PR por feature.
Repos separados se reconsideran si aparecen equipos/productos/ciclos realmente independientes.

## ADR-002 — Monolito modular — 2026-10-03
Estado: adoptado.
Un backend desplegable inicialmente, dividido por módulos de negocio.
Motivo: menor costo operativo/transaccional y evitar distribuir complejidad prematuramente.

## ADR-003 — Agentes selectivos con riesgo — 2026-10-03
Estado: adoptado.
Referencia de calidad: YeinsM/XaurixFX, adaptada a BillingRD.
Router + perfiles + LOW/MEDIUM/HIGH/CRITICAL. No todos los perfiles se cargan/ejecutan por tarea.

## ADR-004 — Factura interna separada de e-CF — 2026-10-03
Estado: adoptado a nivel arquitectónico.
La factura/venta no se modela como respuesta de DGII. ElectronicInvoicing adapta datos canónicos
a artefactos/estados fiscales. Reglas concretas requieren fuente oficial vigente.

## ADR-005 — Offline preparado, no prematuro — 2026-10-03
Estado: adoptado.
Diseñar IDs/idempotencia compatibles con sync; implementar offline completo desde un flujo real acordado.
