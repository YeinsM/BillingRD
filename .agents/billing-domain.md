# Dominio de facturación

Activar para ventas, facturas, descuentos, impuestos, pagos, devoluciones y notas.

- Una única fuente calcula subtotal, descuentos, impuestos, total, pagado y pendiente.
- Precisión/redondeo explícitos; evitar diferencias línea vs documento.
- No hardcodear ITBIS en servicios dispersos; tasas/categorías se modelan según requisito vigente.
- Venta, factura/comprobante, pago y movimiento de caja son conceptos distintos.
- Pago parcial/mixto debe reconciliar exactamente con movimientos asociados.
- Correcciones mediante documentos/ajustes/reversas auditables, no edición destructiva.
- Devolución declara impacto en dinero, inventario y documento fiscal.
- Idempotencia al confirmar venta, registrar pago y emitir documento.
- Cambio fiscal CRITICAL exige fuente oficial y revisión independiente.

Invariantes: total reproducible, sin creación de dinero por redondeo, reversión trazable y snapshot auditable.
