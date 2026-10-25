/* ============================================================================
   03_datos_demo_reportes.sql  -  Ferretería "El Tornillo Feliz" (empresa ficticia)

   Carga un historial de ventas SIMULADO para probar el Módulo 3 (Reportes).
   Los resultados esperados del documento de pruebas salen de ESTOS datos.

   ATENCIÓN - este script:
     1. BORRA todas las ventas y sus detalles (incluidas las de tus pruebas del Módulo 2).
     2. Restaura precios de compra/venta de los 8 productos de ejemplo.
     3. Deja los costos fijos mensuales en Q3,000.00.
     4. NO modifica el stock: son ventas históricas simuladas, no descuentan existencias.
   Si algo no cuadra, hace ROLLBACK y no cambia nada.
   Ejecutar en SSMS con F5 (una sola vez; se puede repetir y siempre deja lo mismo).
   ========================================================================== */
USE FerreteriaTornilloFeliz;
GO
SET NOCOUNT ON;

BEGIN TRANSACTION;

-- 1) Limpiar ventas (primero el detalle por la llave foránea)
DELETE FROM DetalleVenta;
DELETE FROM Ventas;
DBCC CHECKIDENT ('DetalleVenta', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Ventas', RESEED, 0) WITH NO_INFOMSGS;

-- 2) Restaurar precios de los productos de ejemplo (por si cambiaste alguno en las pruebas)
DECLARE @Precios TABLE (Codigo VARCHAR(20), Compra DECIMAL(10,2), Venta DECIMAL(10,2));
INSERT INTO @Precios VALUES
('MAR-001', 45.00,  65.00), ('DES-001', 12.00,  20.00), ('CLA-001',  6.00,  10.00),
('TOR-001', 15.00,  25.00), ('PIN-001', 95.00, 130.00), ('CAB-001',  5.00,   8.50),
('FOC-001', 14.00,  22.00), ('BRO-001', 10.00,  18.00);

UPDATE p SET p.PrecioCompra = x.Compra, p.PrecioVenta = x.Venta
FROM Productos p INNER JOIN @Precios x ON x.Codigo = p.Codigo;

-- 3) Ventas simuladas (la fecha es única en cada venta y sirve para enlazar sus detalles)
DECLARE @V TABLE (Fecha DATETIME, Anulada BIT);
INSERT INTO @V VALUES
('2026-09-28T15:30:00', 0),   -- venta 1: septiembre (queda fuera del rango de octubre)
('2026-10-02T10:15:00', 0),   -- venta 2
('2026-10-05T09:30:00', 0),   -- venta 3
('2026-10-09T16:45:00', 0),   -- venta 4
('2026-10-12T11:00:00', 0),   -- venta 5
('2026-10-15T14:20:00', 1),   -- venta 6: ANULADA (no debe contar en los reportes)
('2026-10-20T10:05:00', 0);   -- venta 7

INSERT INTO Ventas (Fecha, Total, Anulada)
SELECT Fecha, 0, Anulada FROM @V ORDER BY Fecha;

DECLARE @D TABLE (Fecha DATETIME, Codigo VARCHAR(20), Cantidad INT);
INSERT INTO @D VALUES
('2026-09-28T15:30:00', 'MAR-001',  4),
('2026-10-02T10:15:00', 'MAR-001',  2), ('2026-10-02T10:15:00', 'CLA-001', 10),
('2026-10-05T09:30:00', 'PIN-001',  1), ('2026-10-05T09:30:00', 'BRO-001',  2),
('2026-10-09T16:45:00', 'CAB-001', 20), ('2026-10-09T16:45:00', 'FOC-001',  2), ('2026-10-09T16:45:00', 'DES-001', 3),
('2026-10-12T11:00:00', 'MAR-001',  1), ('2026-10-12T11:00:00', 'TOR-001',  2), ('2026-10-12T11:00:00', 'CLA-001', 20),
('2026-10-15T14:20:00', 'PIN-001',  2), ('2026-10-15T14:20:00', 'BRO-001',  3),
('2026-10-20T10:05:00', 'DES-001',  5), ('2026-10-20T10:05:00', 'CAB-001', 30), ('2026-10-20T10:05:00', 'FOC-001', 1);

-- Igual que VentaLN: el detalle guarda el precio y el costo del momento de la venta
INSERT INTO DetalleVenta (IdVenta, IdProducto, Cantidad, PrecioUnitario, CostoUnitario)
SELECT v.Id, p.Id, d.Cantidad, p.PrecioVenta, p.PrecioCompra
FROM @D d
INNER JOIN Ventas v    ON v.Fecha  = d.Fecha
INNER JOIN Productos p ON p.Codigo = d.Codigo;

UPDATE v SET v.Total = t.Total
FROM Ventas v
INNER JOIN (SELECT IdVenta, SUM(PrecioUnitario * Cantidad) AS Total
            FROM DetalleVenta GROUP BY IdVenta) t ON t.IdVenta = v.Id;

-- 4) Costos fijos simulados
UPDATE Configuracion SET CostosFijosMensuales = 3000.00 WHERE Id = 1;
IF @@ROWCOUNT = 0
    INSERT INTO Configuracion (Id, CostosFijosMensuales) VALUES (1, 3000.00);

-- 5) Comprobación: deben insertarse 16 detalles y 7 ventas; si no, falta algún producto de ejemplo
IF (SELECT COUNT(*) FROM DetalleVenta) <> (SELECT COUNT(*) FROM @D)
   OR (SELECT COUNT(*) FROM Ventas) <> (SELECT COUNT(*) FROM @V)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('No se cargaron todos los datos (¿falta algún producto de ejemplo?). Se canceló todo.', 16, 1);
    RETURN;
END

COMMIT TRANSACTION;

-- Resultado: debe mostrar 7 ventas, la 6 anulada
SELECT Id, Fecha, Total, Anulada FROM Ventas ORDER BY Id;
SELECT COUNT(*) AS Detalles FROM DetalleVenta;   -- 16
GO
