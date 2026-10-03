# Revisión técnica

Activar para MEDIUM cuando aporte valor y obligatoriamente en HIGH/CRITICAL según router.

El reviewer no reimplementa por preferencia personal. Evalúa:
- corrección respecto al requisito y contratos;
- límites de módulos y dependencia;
- duplicación de lógica, sobre todo dinero/impuestos/stock/permisos;
- atomicidad, idempotencia y concurrencia;
- errores y observabilidad;
- seguridad/aislamiento;
- rendimiento si existe ruta crítica o evidencia;
- claridad/mantenibilidad proporcional;
- tests que cubren riesgo, no solo líneas.

Priorizar hallazgos que cambian comportamiento o riesgo. No bloquear por estilo automatizable.
Salida: hallazgos por severidad con archivo/escenario; si no hay hallazgos materiales, indicarlo.
