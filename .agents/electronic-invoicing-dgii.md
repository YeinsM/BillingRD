# Facturación electrónica DGII / e-CF

Activar para NCF, e-NCF, e-CF, XML, firma, envío, respuestas, contingencia y certificación.

Regla principal: no inventar comportamiento fiscal. Consultar documentación oficial DGII vigente
para cada cambio material y registrar fecha/fuente.

- Separar factura interna del artefacto fiscal.
- Estados fiscales explícitos conforme especificación vigente.
- XML generado desde datos canónicos y validado contra reglas/esquemas oficiales.
- Certificados y claves privadas nunca en Git, frontend, logs ni fixtures.
- Envío/reintento seguro sin emisión duplicada.
- Persistir evidencia necesaria: payload/hash/versiones/respuesta/fechas según política.
- Contingencia y secuencias solo desde documentación oficial verificada.
- No asumir que todos los clientes son emisores electrónicos ni usan el mismo comprobante.
- Todo cambio CRITICAL requiere billing + security + QA/review independiente.

Salida: regla aplicada, fuente, contrato, estados, evidencia y pendientes de certificación.
