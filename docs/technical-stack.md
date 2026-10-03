# Stack técnico

Verificado el 2026-10-03.

## Backend
- .NET 10 LTS.
- SDK fijado inicialmente en 10.0.401 mediante global.json.
- ASP.NET Core.
- ORM/acceso a datos sigue pendiente de ADR; no se asume EF Core por herencia.

## Frontend
- Angular 22.
- Node.js mínimo 22.22.3 conforme compatibilidad oficial de Angular 22.
- TypeScript 6.0.x.
- PWA/web primero; Tauri y Capacitor se integran cuando exista un flujo funcional que justificar empaquetar.

## Base de datos
- PostgreSQL 18.
- Desarrollo local fijado inicialmente en imagen 18.6-alpine.

Las versiones se revisan antes de upgrades; no se cambia major automáticamente.
