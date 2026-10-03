# PostgreSQL

Activar para esquema, consultas, integridad, concurrencia y migraciones.

- Toda entidad multiempresa debe aislarse por BusinessId/TenantId según modelo aprobado.
- Dinero/tasas: decimal exacto con precisión y escala explícitas.
- Constraints complementan validación: FK, unique, check y not null para invariantes.
- Consultas parametrizadas, columnas necesarias, paginación e índices por uso real.
- Transacciones pequeñas; definir concurrencia para caja, stock y pagos.
- Migraciones versionadas y revisadas contra datos existentes.
- Probar migración y recuperación en DB descartable; documentar backup/restore si irreversible.
- No borrar historial crítico para corregir; usar reversas/ajustes auditables.
- Evaluar UUIDv7 para entidades sincronizables cuando el stack adoptado lo soporte.
- RLS puede ser defensa adicional, nunca sustituto de autorización.

Validación: constraints, migración, concurrencia y aislamiento relevantes.
