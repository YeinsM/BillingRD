# Stack técnico

Verificado el 2026-10-03.

## Backend
- .NET 10 LTS.
- SDK 10.0.401 mediante global.json.
- ASP.NET Core.
- EF Core 10 para persistencia.
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3.
- Microsoft.EntityFrameworkCore.Design 10.0.4 para migraciones.

## Frontend
- Angular 22.
- Node.js mínimo 22.22.3 conforme compatibilidad oficial de Angular 22.
- TypeScript 6.0.x.
- PWA/web primero; Tauri y Capacitor se integran cuando exista flujo funcional.

## Base de datos
- PostgreSQL 18.
- Desarrollo local: postgres:18.6-alpine.
- Migraciones EF Core versionadas en Infrastructure.

## Testing backend
- Pruebas de integración contra PostgreSQL real en CI.
- Microsoft.NET.Test.Sdk 18.10.1.
- xUnit 2.9.3 + Visual Studio runner 4.0.0.

Las versiones se revisan antes de upgrades; no se cambia major automáticamente.
