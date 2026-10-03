# Operaciones, CI y releases

Activar para Git, Docker, CI/CD, entornos, desktop/mobile packaging y despliegues.

- Inspeccionar scripts reales antes de ejecutar; no inventar comandos/resultados.
- Fijar versiones SDK/runtime cuando se adopten.
- Entornos separados y datos sintéticos; nunca clientes reales en fixtures.
- Pipelines selectivos por paths cuando la estructura se estabilice.
- Build/test independientes de deploy. Commit/push no autorizan producción.
- No force push ni borrar trabajo ajeno.
- Docker local reproduce dependencias necesarias sin ocultar prerrequisitos.
- Secretos en mecanismos seguros; no .env real versionado.
- Releases desktop/mobile registran versión, commit y canal.
- Rollback/restore antes de migraciones o deploys de alto riesgo.

Salida: comandos reales/resultados, artefactos, destino y limitaciones.
