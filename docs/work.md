# Estado y evidencia

Estados: definido, en curso, pendiente de decisión, implementado, verificado, publicado.

| ID | Alcance / aceptación | Estado | Evidencia |
|---|---|---|---|
| BOOT-01 | Repo inicializado y rama de bootstrap | implementado | GitHub |
| AG-01 | Router, perfiles y clasificación de riesgo | implementado | .agents/ |
| DOC-01 | Producto, ADRs, trabajo y bugs | implementado | docs/ |
| ARCH-01 | Monorepo + monolito modular | definido | ADR-001/002 |
| STRUCT-01 | Límites backend/frontend/apps/infra/tests | implementado | READMEs de cada área |
| TECH-01 | Esqueleto .NET/Angular/PostgreSQL | verificado | CI #37126890170 |
| BUS-01 | Business/Branch/User/Membership/Product/Customer | verificado | PR #3 + CI #37127912570 |
| DB-01 | EF Core 10 + Npgsql + migración inicial | verificado | PostgreSQL 18.6 integration test |
| SEC-01 | Filtro fail-closed para recursos business-scoped | verificado | test de aislamiento entre dos negocios |
| AUTH-01 | Registro/login/logout con cookie HttpOnly y password hashing | verificado | PR #4 + CI #37129390754 |
| AUTH-02 | Selección de negocio validada contra BusinessMembership | verificado | HTTP integration test de acceso cruzado |
| API-01 | Business/Product/Customer API autenticada | verificado | HTTP integration test contra PostgreSQL |
| TAX-01 | ITBIS 0/16/18 + redondeo/snapshot por línea | implementado | ADR-010 + rama sales-invoice-payment |
| SALE-01 | Sale/SaleLine + idempotencia + autorización | implementado | pendiente CI |
| INV-01 | Invoice interna 1:1 con Sale | implementado | pendiente CI |
| PAY-01 | Pagos múltiples que cuadran exactamente con Sale.Total | implementado | pendiente CI |
| MVP-01 | Negocio → producto → cliente → venta → factura → pago | en curso | flujo transaccional implementado; pendiente CI |
| DGII-01 | Integración fiscal desde fuente oficial vigente | definido | sin implementación |
| OFF-01 | Preparación arquitectónica para offline | definido | ADR-005 |

## Handoff activo
Validar el flujo Sale → SaleLine → Invoice → Payment contra PostgreSQL real.
Si CI queda verde, cerrar PR y continuar con inventario/movimientos y caja antes de integrar e-CF.

Decisiones ya resueltas:
- Business como frontera principal y Branch como hijo;
- autorización mediante BusinessMembership/BusinessRole;
- EF Core 10 + Npgsql;
- cookie HttpOnly same-site para autenticación del MVP;
- BusinessId activo revalidado contra membership en cada request;
- ITBIS 0/16/18, precio MVP antes de impuesto y snapshot fiscal por línea;
- venta confirmada totalmente pagada e idempotente en esta primera fase.
