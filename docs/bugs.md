# Memoria de errores

No hay bugs de producto confirmados: el código de aplicación aún no existe.

## Uso
Buscar por área/síntoma antes de modificar comportamiento.
Registrar un bug por causa y enlazar recurrencias.

Formato:
- BUG-NNN, fecha, severidad P0/P1/P2/P3, área, estado.
- Síntoma y reproducción mínima.
- Esperado vs observado.
- Causa comprobada.
- Corrección.
- Prevención verificable: test/constraint/check exacto.
- Evidencia antes/después.
- Relación con recurrencias.

P0: corrupción/exposición/fiscal crítica.
P1: dinero, aislamiento, stock o flujo esencial incorrecto.
P2: función secundaria.
P3: defecto menor.

Documentar no equivale a prevenir: solo un control ejecutado sustenta "verificado".
