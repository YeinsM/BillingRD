# Router de agentes

Los perfiles son instrucciones especializadas; no se ejecutan todos.

## Riesgo
| Nivel | Ejemplos | Flujo mínimo |
|---|---|---|
| LOW | texto, estilo, docs | dueño + check |
| MEDIUM | CRUD, filtros, endpoint sin dinero | dueño + QA selectivo |
| HIGH | inventario, caja, auth, migración, offline | coordinator + dominio + QA |
| CRITICAL | totales fiscales, pagos, e-CF/DGII, aislamiento, pérdida de datos | coordinator + dominio + implementación + revisión independiente + QA |

Eleva riesgo por dinero, fiscalidad, aislamiento multiempresa, concurrencia, migración irreversible,
offline o integración externa con efectos.

## Routing
| Cambio | Principal | Añadir |
|---|---|---|
| Alcance/contratos/cross-domain | coordinator | requirements + dueño |
| Requisito ambiguo | requirements | coordinator |
| API/casos de uso .NET | backend-dotnet | security/dominio |
| UI Angular/PWA | frontend-angular | backend si cambia contrato |
| DB/migraciones | database-postgres | dominio + QA |
| Facturas/impuestos/pagos | billing-domain | database + QA |
| Stock/devoluciones | inventory | billing + database |
| e-CF/DGII | electronic-invoicing-dgii | billing + security + QA |
| Offline/sync | offline-sync | database + dominio + QA |
| Auth/roles/tenant | security | backend + database |
| Bug/regresión | qa-regression | dueño |
| Revisión técnica | reviewer | dueño |
| CI/Docker/releases | operations | áreas afectadas |

## Delegación
1. Define resultado, aceptación, riesgo y archivos probables.
2. Tarea pequeña: un agente. Delegar solo si reduce duplicación o aporta especialidad.
3. Presupuesto inicial: coordinator + hasta dos subagentes.
4. Handoff: objetivo, requisito, alcance, contrato, restricciones, aceptación y checks.
5. Solo coordinator integra cambios multiárea/registros compartidos.
6. HIGH/CRITICAL exige evidencia proporcional; revisión independiente no puede atribuirse al autor.
7. Dos intentos iguales sin evidencia nueva obligan a cambiar diagnóstico o declarar bloqueo.

Salida: resultado, archivos, verificaciones, riesgos y pendientes. No afirmar ejecución inexistente.
