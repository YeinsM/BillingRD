# BillingRD — instrucciones de trabajo

Repositorio: https://github.com/YeinsM/BillingRD

## Entrada mínima
Lee [.agents/index.md](.agents/index.md), clasifica el riesgo y carga solo los perfiles necesarios.
Consulta [docs/project.md](docs/project.md) al cambiar alcance o reglas y [docs/decisions.md](docs/decisions.md)
antes de elegir comportamiento durable. Busca en [docs/bugs.md](docs/bugs.md) antes de modificar comportamiento existente.

## Arquitectura
Monorepo; backend ASP.NET Core/.NET 10; frontend Angular; PostgreSQL; adaptadores web/PWA,
escritorio y móvil. Backend inicial: monolito modular. No introducir microservicios, CQRS,
event sourcing o infraestructura distribuida sin necesidad demostrada.

## Reglas
- Distingue confirmado, propuesto, pendiente, implementado, probado y publicado.
- No conviertas preguntas en requisitos aprobados.
- No inventes reglas fiscales; DGII/e-CF se implementa desde documentación oficial vigente.
- Dinero: decimal exacto y redondeo explícito; nunca float/double.
- Ventas, pagos, caja e inventario críticos se corrigen con reversas/ajustes auditables, no borrando historial.
- Operaciones reintentables con efectos deben ser idempotentes.
- No cargues todos los perfiles; usa el router.
- Solo coordinator integra cambios multiárea y registros compartidos.
- No publiques secretos, certificados, tokens ni datos reales.
- Revisa diff y verifica según riesgo antes de cerrar.

## Git
Ramas `codex/*` salvo indicación. Commits pequeños y coherentes. No force push.
No mezclar refactors no solicitados con funcionalidad.

## Cierre
Actualiza evidencia afectada en docs/work.md, decisiones duraderas en decisions.md y bugs
confirmados con prevención verificable en bugs.md. No declares pruebas que no ocurrieron.
