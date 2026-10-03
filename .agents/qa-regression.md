# QA y regresión

Activar para aceptación, bugs, cambios HIGH/CRITICAL y cierre técnico.

- Reproducir antes de corregir; separar síntoma de causa.
- Cada bug confirmado registra causa comprobada y prevención verificable.
- Preferir test que falle antes y pase después; si no automatizable, procedimiento reproducible.
- LOW: diff/alcance. UI: flujo/estados. API: contrato/permisos. DB: integración real.
- HIGH/CRITICAL: negativos, concurrencia, idempotencia y revisión independiente cuando aplique.
- Un build no prueba reglas fiscales/contables; un mock no prueba constraint PostgreSQL.
- No repetir suite completa sin nuevo riesgo o cambio.
- Ante recurrencia, explicar por qué el control previo falló y mejorarlo.
- No declarar revisión independiente sobre trabajo propio.

Hallazgo: severidad, ubicación, escenario, impacto y comprobación propuesta.
