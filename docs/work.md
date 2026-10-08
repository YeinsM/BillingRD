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
| TAX-01 | Exento + ITBIS 0/16/18 + redondeo/snapshot por línea | verificado | PR #5 + CI #37466043222 |
| SALE-01 | Sale/SaleLine + idempotencia + autorización | verificado | PostgreSQL/HTTP integration tests |
| INV-01 | Invoice interna 1:1 con Sale | verificado | PostgreSQL/HTTP integration tests |
| PAY-01 | Pagos múltiples que cuadran exactamente con Sale.Total | verificado | PostgreSQL/HTTP integration tests |
| MVP-01 | Negocio → producto → cliente → venta → factura → pago | verificado | PR #5 + CI #37466043222 |
| STOCK-01 | StockBalance por sucursal + movimientos auditables | verificado | PR #6 + CI #37467802708 |
| STOCK-02 | Venta descuenta stock atómicamente y rechaza sobreventa | verificado | última unidad concurrente + CI #37467802708 |
| CASH-01 | CashRegister + CashSession + apertura/cierre | verificado | PR #7 + CI #37476745272 |
| CASH-02 | Venta Cash genera CashMovement idempotente | verificado | mixed payment + retry tests |
| CASH-03 | Conciliación Expected/Counted/Difference | verificado | cierre con diferencia + CI #37476745272 |
| RET-01 | Devolución parcial/total sin exceder cantidad vendida | verificado | PR #8 + CI #37481370309 |
| RET-02 | Reposición de inventario + reembolso por método | verificado | ReturnFlowTests + PostgreSQL |
| RET-03 | Cash refund en sesión abierta + idempotencia | verificado | ReturnFlowTests + CI #37481370309 |
| ADJ-01 | Devolución crea AdjustmentDocument interno 1:1 | verificado | PR #9 + CI #37496619541 |
| ECF-01 | Catálogo oficial de tipos e-CF + draft tipo 34 | verificado | DGII + ElectronicInvoicingFoundationTests |
| ECF-02 | Draft fiscal 1:1, sin e-NCF/XML/firma/envío | verificado | CI #37496619541 |
| FISC-01 | BusinessFiscalProfile + identidad fiscal Customer | verificado | PR #10 + CI #37617623620 |
| ECF-03 | Draft e-CF 31 con comprador identificado | verificado | FiscalInvoiceDraftTests + PostgreSQL |
| ECF-04 | Draft e-CF 32 + regla DOP 250,000 | verificado | CI #37617623620 |
| ECF-05 | Snapshot fiscal de líneas/totales/pagos para 31/32 | en curso | PR XML pendiente de CI |
| ECF-06 | XML preview 31/32 pre-firma y preflight local | en curso | FiscalXmlPreviewTests pendiente de CI |
| DGII-01 | Integración fiscal desde fuente oficial vigente | definido | drafts 31/32/34 + XML preview; sin emisión |
| OFF-01 | Preparación arquitectónica para offline | definido | ADR-005 |

## Handoff activo
Primer vertical slice comercial completo y verificado: negocio → producto → cliente → venta → factura interna → pago.

Inventario por sucursal verificado.

Caja / cash register verificada.

Devoluciones y reembolsos verificados.

Ajustes internos + base de ElectronicInvoicing verificados.

Perfil fiscal + preparación e-CF 31/32 verificados.

Slice actual: snapshot fiscal + XML preview 31/32.

Siguiente slice recomendado una vez verificado:
1. importar/versionar XSD oficiales 31/32 con hash y pruebas de esquema;
2. implementar proveedor de secuencias e-NCF de certificación/producción;
3. implementar XMLDSig/certificado y recién entonces habilitar documento submittable.

Decisiones ya resueltas:
- Business como frontera principal y Branch como hijo;
- autorización mediante BusinessMembership/BusinessRole;
- EF Core 10 + Npgsql;
- cookie HttpOnly same-site para autenticación del MVP;
- BusinessId activo revalidado contra membership en cada request;
- Exempt y ZeroRated separados, ITBIS 0/16/18, precio MVP antes de impuesto y snapshot fiscal por línea;
- venta confirmada totalmente pagada e idempotente en esta primera fase;
- inventario controlado por sucursal, sin stock negativo en el MVP;
- devolución interna separada de nota de crédito fiscal, con reversión atómica de stock/caja.
