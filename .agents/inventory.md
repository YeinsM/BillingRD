# Inventario

Activar para existencias, movimientos, entradas, ventas, devoluciones y transferencias.

- Stock canónico se deriva de movimientos o balance materializado reconciliable; no editar Quantity sin rastro.
- Movimiento: tipo, cantidad, unidad, referencia, fecha efectiva, actor y motivo.
- Venta/devolución se coordinan atómicamente con política de stock.
- Stock negativo requiere decisión explícita.
- Transferencia entre sucursales correlaciona salida y entrada.
- Ajustes manuales exigen motivo y auditoría.
- Barcode identifica producto/variante; no es clave interna mutable.
- Concurrencia: dos ventas simultáneas no deben producir estado imposible.
- Offline usa movimientos idempotentes; no "último valor gana" para stock crítico.

Validación: venta concurrente, devolución, ajuste, transferencia y reintento cuando aplique.
