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

## ADR-006 — Modelo multiempresa inicial — 2026-10-03
Estado: adoptado.
Business es la frontera principal de propiedad de datos. Branch pertenece a Business.
Product y Customer pertenecen inicialmente a Business; el inventario por sucursal se modelará al implementar stock.
Los recursos business-scoped se filtran usando un contexto de negocio confiable resuelto por autenticación/autorización.
Sin contexto confiable, las consultas deben fallar cerradas.

## ADR-007 — Autorización por membresía — 2026-10-03
Estado: adoptado.
Un UserAccount puede pertenecer a uno o más negocios mediante BusinessMembership y un BusinessRole.
Roles iniciales: Owner, Administrator, Cashier, InventoryManager.
El BusinessId activo nunca se confía por venir del cliente: se activa únicamente después de verificar una membresía
y se vuelve a comprobar en cada request antes de poblar CurrentBusinessContext.

## ADR-008 — EF Core + Npgsql — 2026-10-03
Estado: adoptado.
Persistencia relacional con EF Core 10 y Npgsql sobre PostgreSQL.
Motivo: migrations, transacciones, constraints, query filters, integración .NET 10 y bajo costo de mantenimiento.
No se añade repositorio genérico por encima de EF Core; se crearán abstracciones solo cuando un caso de uso lo justifique.

## ADR-009 — Autenticación cookie para el MVP — 2026-10-03
Estado: adoptado.
La API usa una cookie ASP.NET Core cifrada/firmada, HttpOnly y SameSite=Strict, con sesión deslizante de 8 horas.
Las contraseñas se almacenan exclusivamente como hashes generados/verificados por ASP.NET Core PasswordHasher.
Registro y login tienen rate limit. No se almacenan tokens de acceso en localStorage.

Motivo: para una SPA/PWA servida same-site con la API, la cookie HttpOnly reduce exposición del secreto de sesión
al JavaScript del navegador y mantiene el MVP simple. Si web y API pasan a sitios distintos, o antes de exposición
pública con escenarios cross-site, se debe revisar política de cookies/CORS y añadir la estrategia antiforgery
correspondiente antes de considerar el flujo listo para producción.
