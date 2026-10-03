# Contrato inicial del producto

## Confirmado — 2026-10-03
- Sistema de facturación/POS para pequeñas tiendas y negocios en República Dominicana.
- Monorepo: YeinsM/BillingRD.
- Backend: ASP.NET Core/.NET 10.
- Frontend: Angular compartido para web/PWA, escritorio y móvil.
- Desktop: objetivo Tauri; móvil: objetivo Capacitor, sujetos a validación técnica.
- PostgreSQL; persistencia adoptada: EF Core 10 + Npgsql.
- Arquitectura inicial: monolito modular.
- Multiempresa: Business es frontera inicial; Branch pertenece a Business.
- Autorización de negocio: membresías y roles; mecanismo de autenticación aún pendiente.
- Núcleo: productos, clientes, ventas, facturas/comprobantes, pagos, caja e inventario.
- e-CF/DGII será un módulo aislado.
- Desktop debe poder evolucionar a offline con sincronización segura.

## Principios
- POS rápido.
- Datos/cálculos auditables.
- UI responsiva/reutilizable.
- Aislamiento entre negocios por defecto.
- Crecer sin microservicios prematuros.
- Fiscalidad desde fuentes oficiales vigentes.

## Decisiones pendientes
| ID | Decisión | Afecta |
|---|---|---|
| D01 | Nombre comercial definitivo | branding |
| D03b | Proveedor/mecanismo de autenticación | seguridad |
| D05 | Política de stock negativo | inventario |
| D06 | Impuestos/tipos de producto MVP | billing |
| D07 | Alcance fiscal MVP y comprobantes priorizados | DGII |
| D08 | DB local desktop y primer flujo offline | sync |
| D09 | Hardware POS objetivo | POS |
| D10 | Hosting/storage/email/observabilidad | operaciones |

No interpretar decisiones pendientes como aprobadas.
