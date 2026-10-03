# Contrato inicial del producto

## Confirmado — 2026-10-03
- Sistema de facturación/POS para pequeñas tiendas y negocios en República Dominicana.
- Monorepo: YeinsM/BillingRD.
- Backend objetivo: ASP.NET Core/.NET 10.
- Frontend objetivo: Angular compartido para web/PWA, escritorio y móvil.
- Desktop: objetivo Tauri; móvil: objetivo Capacitor, sujetos a validación técnica del bootstrap.
- PostgreSQL en servidor.
- Arquitectura inicial: monolito modular.
- Soporte multiempresa y luego multisucursal.
- Núcleo: productos, clientes, ventas, facturas/comprobantes, pagos, caja e inventario.
- e-CF/DGII será un módulo aislado.
- Desktop debe poder evolucionar a offline con sincronización segura.
- Fase actual: agentes, documentación y esqueleto técnico.

## Principios
- POS rápido.
- Datos/cálculos auditables.
- UI responsiva/reutilizable.
- Crecer sin microservicios prematuros.
- Fiscalidad desde fuentes oficiales vigentes.

## Decisiones pendientes
| ID | Decisión | Afecta |
|---|---|---|
| D01 | Nombre comercial definitivo | branding |
| D02 | Modelo Tenant/Business/Branch exacto | aislamiento |
| D03 | Identidad/auth y roles iniciales | seguridad |
| D04 | ORM/acceso a datos .NET | backend/database |
| D05 | Política de stock negativo | inventario |
| D06 | Impuestos/tipos de producto MVP | billing |
| D07 | Alcance fiscal MVP y comprobantes priorizados | DGII |
| D08 | DB local desktop y primer flujo offline | sync |
| D09 | Hardware POS objetivo | POS |
| D10 | Hosting/storage/email/observabilidad | operaciones |

No interpretar decisiones pendientes como aprobadas.
