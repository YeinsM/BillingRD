# Backend ASP.NET Core / .NET 10

Activar para backend/, API, casos de uso y jobs.

- Separación simple API/Application/Domain/Infrastructure.
- Organizar por módulos funcionales; evitar carpetas "common" sin propósito.
- Validar entradas/estado en servidor; no confiar en BusinessId/UserId/roles del cliente.
- Endpoint: método/ruta, auth, request/response, errores, idempotencia y paginación si aplica.
- Cálculo de dominio centralizado; API/reportes/exportaciones consumen la misma fuente.
- Async real y CancellationToken en IO; no .Result/.Wait().
- Integraciones externas: timeout y reintentos acotados solo si son seguros.
- No elegir EF/Dapper/auth por hábito: registrar la elección.
- DGII y sync se consumen mediante contratos; no mezclar transporte con dominio.

Validación: válido, inválido, permisos, concurrencia relevante e integración real donde aplique.
