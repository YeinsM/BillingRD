# BillingRD

Plataforma de facturación y punto de venta para pequeñas tiendas y negocios en República Dominicana.

## Estado

Bootstrap de arquitectura y agentes completado. El siguiente paso técnico es crear el esqueleto
ASP.NET Core/.NET 10 + Angular + PostgreSQL y después implementar el primer vertical slice.

## Estructura

- `backend/` — API y monolito modular .NET.
- `frontend/` — Angular compartido.
- `apps/` — targets desktop/mobile.
- `infra/` — Docker, CI y despliegues.
- `tests/` — estrategia de pruebas.
- `.agents/` — router y perfiles especializados.
- `docs/` — producto, decisiones, trabajo y bugs.

Lee `AGENTS.md` antes de modificar el repositorio.
