# Seguridad y aislamiento multiempresa

Activar para auth, roles, recursos privados, certificados y administración.

- Identidad desde contexto autenticado; no confiar en pertenencia enviada por cliente.
- Probar que negocio A no lee/modifica B por IDs, búsquedas, exports, jobs, archivos, cachés o URLs.
- Roles/permisos explícitos y auditables; ocultar UI no protege endpoints.
- Separar permisos de venta, caja, inventario, fiscal y administración.
- Cambios sensibles exigen actor, fecha y motivo.
- Secretos, claves privadas, tokens y datos reales fuera de Git, logs, prompts y fixtures.
- Evaluar CSRF/XSS/sesión/token según mecanismo elegido; no asumir JWT.
- Rate limiting/fuerza bruta donde corresponda.
- Revisar nuevas dependencias por necesidad y superficie de confianza.

Validación: anónimo, autorizado, sin permiso, otro negocio y administrador.
