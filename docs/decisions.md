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

## ADR-010 — Modelo de ITBIS y redondeo del MVP — 2026-10-06
Estado: adoptado.
BillingRD soporta inicialmente cuatro tratamientos fiscales por producto: Exempt (exento), ZeroRated (ITBIS 0%), Reduced (16%) y Standard (18%).
La tasa se configura en Product, pero SaleLine guarda snapshot de categoría, tasa, precio, subtotal, impuesto y total.

SalePrice se interpreta inicialmente como precio antes de ITBIS. El impuesto se calcula por línea:
Subtotal = round(Quantity × UnitPrice, 2)
TaxAmount = round(Subtotal × TaxRate / 100, 2)
Total = Subtotal + TaxAmount
El redondeo monetario usa 2 decimales con MidpointRounding.AwayFromZero.

Motivo: DGII mantiene tasa general de 18%, tasa reducida de 16% para determinados productos y distingue operaciones exentas de partidas gravadas a tasa 0% en el formato e-CF.
El snapshot evita que cambios futuros de catálogo o tasas reescriban ventas históricas.
Una futura opción de precios con impuestos incluidos requiere ADR y casos de prueba propios antes de implementarse.

## ADR-011 — Venta pagada e idempotente como primera transacción — 2026-10-06
Estado: adoptado.
El primer flujo de Sale crea atómicamente Sale + SaleLine + Invoice interna + Payment(s).
La suma aplicada de pagos debe igualar exactamente el total de la venta. Sobrepago/cambio en efectivo y cuentas por cobrar
se modelarán explícitamente después; no se infieren en esta fase.

POST /api/sales requiere Idempotency-Key. La clave es única por Business para evitar duplicados ante retries de POS/offline.
Owner, Administrator y Cashier pueden vender; InventoryManager no puede confirmar ventas.

La Invoice creada en esta fase es interna y no es NCF/e-CF. La emisión fiscal continúa aislada en ElectronicInvoicing.


## ADR-012 — Inventario por sucursal sin stock negativo — 2026-10-06
Estado: adoptado.
Los productos pueden indicar TracksInventory. Para productos controlados, el stock es específico por Branch.
StockBalance mantiene la cantidad operativa actual y StockMovement conserva el historial auditable de entradas/salidas.

El MVP no permite stock negativo. Una venta de producto inventariable solo se confirma si existe saldo suficiente en
la sucursal seleccionada. El descuento usa actualización condicional en PostgreSQL dentro de la misma transacción que
Sale + SaleLine + Invoice + Payment, para evitar sobreventa concurrente.

Los ajustes manuales requieren motivo y solo Owner, Administrator o InventoryManager pueden ejecutarlos.
Sale genera movimientos tipo Sale automáticamente. Un producto con TracksInventory=false puede venderse sin balance.

Motivo: una tienda necesita consistencia operativa inmediata y trazabilidad. Permitir stock negativo por defecto
ocultaría errores de recepción/conteo y complicaría la reconciliación. Si se requiere venta con stock negativo en el
futuro deberá ser una política explícita por negocio/sucursal con ADR y pruebas de concurrencia propias.


## ADR-013 — Caja por sesión con movimientos auditables — 2026-10-06
Estado: adoptado.
CashRegister representa una caja física/lógica ligada a una Branch. CashSession representa un turno abierto/cerrado.
Solo puede existir una CashSession abierta por CashRegister. El efectivo esperado se deriva de OpeningBalance +
CashMovement(s); al cerrar se guarda snapshot de ExpectedCashAtClose, CountedCash y Difference.

Los pagos Cash de ventas requieren una sesión abierta de una caja perteneciente a la misma sucursal de la venta.
Cada Payment efectivo genera exactamente un CashMovement tipo SaleCash. Card y BankTransfer no modifican efectivo.
PaymentId es único en CashMovement para impedir duplicación por retries.

Los movimientos manuales admitidos en el MVP son ManualIncome, Expense y Withdrawal. Requieren motivo y permisos
Owner/Administrator. Owner, Administrator y Cashier pueden abrir/cerrar sesiones. El cierre bloquea la sesión durante
el cálculo final para evitar movimientos concurrentes omitidos.

Motivo: separar caja, turno y movimientos permite conciliación operativa, múltiples cajas por sucursal y auditoría
sin mezclar el saldo físico con la entidad Payment.
