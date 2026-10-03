# Offline y sincronización

Activar para trabajo sin internet, almacenamiento local y reconciliación.

- Offline es capacidad explícita, no réplica improvisada de PostgreSQL.
- Definir qué comandos nacen offline y cuáles requieren servidor/DGII.
- Comando sincronizable: ID estable, origen, versión/fecha y clave idempotente.
- Reintentos no duplican venta, pago, caja, stock ni documento.
- No usar last-write-wins para dinero, stock o estados fiscales sin regla aprobada.
- Cola local: pending/sent/acknowledged/failed con causa accionable.
- Servidor es autoridad para reglas multiusuario/fiscales salvo excepción documentada.
- UI muestra offline, pendiente, conflicto y sincronizado.
- Probar cierre inesperado, reconexión, duplicados, orden invertido y dos dispositivos.

No implementar sync completa hasta definir primer flujo offline real.
