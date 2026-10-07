# Contrato inicial del producto

## Confirmado — 2026-10-06
- Sistema de facturación/POS para pequeñas tiendas y negocios en República Dominicana.
- Monorepo: YeinsM/BillingRD.
- Backend: ASP.NET Core/.NET 10.
- Frontend: Angular compartido para web/PWA, escritorio y móvil.
- Desktop: objetivo Tauri; móvil: objetivo Capacitor, sujetos a validación técnica.
- PostgreSQL; persistencia adoptada: EF Core 10 + Npgsql.
- Arquitectura inicial: monolito modular.
- Multiempresa: Business es frontera inicial; Branch pertenece a Business.
- Autorización de negocio: membresías y roles; autenticación MVP con cookie HttpOnly same-site.
- Núcleo: productos, clientes, ventas, facturas/comprobantes, pagos, caja e inventario.
- ITBIS MVP: Exempt (exento), ZeroRated (0%), Reduced (16%) y Standard (18%); SalePrice inicialmente antes de impuesto.
- Las ventas guardan snapshots monetarios/fiscales por línea y requieren idempotencia.
- Inventario: stock por sucursal con balances + movimientos auditables; stock negativo deshabilitado en el MVP.
- Caja: múltiples CashRegister por sucursal, una sesión abierta por caja, movimientos auditables y conciliación esperado/contado.
- Devoluciones: parciales/totales contra snapshots de venta, con reposición de inventario y reembolso trazable por método.
- Ajustes: cada devolución genera un AdjustmentDocument interno; ElectronicInvoicing puede preparar un borrador fiscal separado.
- Alcance fiscal MVP: drafts 31 (Crédito Fiscal), 32 (Consumo) y 34 (Nota de Crédito); emisión DGII aún no implementada.
- e-CF/DGII es un módulo aislado de la factura/ajuste interno; hoy solo existe preparación de borrador tipo 34, no emisión.
- Desktop debe poder evolucionar a offline con sincronización segura.

## Principios
- POS rápido.
- Datos/cálculos auditables.
- UI responsiva/reutilizable.
- Aislamiento entre negocios por defecto.
- Crecer sin microservicios prematuros.
- Fiscalidad desde fuentes oficiales vigentes.
- Ningún retry puede duplicar una venta, devolución, pago o reversión operativa.

## Decisiones pendientes
| ID | Decisión | Afecta |
|---|---|---|
| D01 | Nombre comercial definitivo | branding |
| D08 | DB local desktop y primer flujo offline | sync |
| D09 | Hardware POS objetivo | POS |
| D10 | Hosting/storage/email/observabilidad | operaciones |
| D11 | Sobrepago/cambio y ventas a crédito | sales/payments |
| D12 | Precios con ITBIS incluido como opción | billing/POS |

No interpretar decisiones pendientes como aprobadas.
