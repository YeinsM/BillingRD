# Estado y evidencia

Estados: definido, en curso, pendiente de decisión, implementado, verificado, publicado.

| ID | Alcance / aceptación | Estado | Evidencia |
|---|---|---|---|
| BOOT-01 | Repo inicializado y rama de bootstrap | implementado | GitHub |
| AG-01 | Router, perfiles y clasificación de riesgo | implementado | .agents/ |
| DOC-01 | Producto, ADRs, trabajo y bugs | implementado | docs/ |
| ARCH-01 | Monorepo + monolito modular | definido | ADR-001/002 |
| STRUCT-01 | Límites backend/frontend/apps/infra/tests | implementado | READMEs de cada área |
| TECH-01 | Esqueleto .NET/Angular/PostgreSQL | verificado | CI #37126890170: backend y frontend build exitosos |
| MVP-01 | Negocio → producto → cliente → venta → factura → pago | definido | sin implementación |
| DGII-01 | Integración fiscal desde fuente oficial vigente | definido | sin implementación |
| OFF-01 | Preparación arquitectónica para offline | definido | ADR-005 |

## Handoff activo
Bootstrap técnico verificado. Siguiente vertical slice: negocio → producto → cliente → venta → factura → pago.
Antes de persistencia, resolver D02 (Tenant/Business/Branch), D03 (auth/roles) y D04 (acceso a datos/ORM)
solo en la medida necesaria para ese slice.
