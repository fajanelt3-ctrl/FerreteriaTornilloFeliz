/* ============================================================================
   02_reiniciar_datos_prueba.sql  -  Ferretería "El Tornillo Feliz" (datos simulados)

   Deja la base lista para repetir las pruebas P y V del registro de pruebas:
     1. BORRA todas las ventas y sus detalles.
     2. Restaura precio, stock, stock mínimo y estado activo de los 8 productos de ejemplo.
     3. Deja los costos fijos mensuales en Q3,000.00.
   NO borra los productos que usted haya agregado (por ejemplo HAM-001).
   Si ya tiene su propio script de reinicio de las pruebas del módulo de ventas, puede usar el suyo.
   ========================================================================== */
USE FerreteriaTornilloFeliz;
GO
SET NOCOUNT ON;

BEGIN TRANSACTION;

DELETE FROM DetalleVenta;
DELETE FROM Ventas;
DBCC CHECKIDENT ('DetalleVenta', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Ventas', RESEED, 0) WITH NO_INFOMSGS;

DECLARE @Base TABLE (Codigo VARCHAR(20), Compra DECIMAL(10,2), Venta DECIMAL(10,2), Stock INT, Minimo INT);
INSERT INTO @Base VALUES
('MAR-001',  45.00,  65.00,  20,  5),
('DES-001',  12.00,  20.00,  40, 10),
('CLA-001',   6.00,  10.00, 100, 20),
('TOR-001',  15.00,  25.00,   8, 10),
('PIN-001',  95.00, 130.00,  12,  4),
('CAB-001',   5.00,   8.50, 150, 30),
('FOC-001',  14.00,  22.00,   3,  8),
('BRO-001',  10.00,  18.00,  25,  5);

UPDATE p
SET p.PrecioCompra = b.Compra, p.PrecioVenta = b.Venta,
    p.Stock = b.Stock, p.StockMinimo = b.Minimo, p.Activo = 1
FROM Productos p INNER JOIN @Base b ON b.Codigo = p.Codigo;

UPDATE Configuracion SET CostosFijosMensuales = 3000.00 WHERE Id = 1;
IF @@ROWCOUNT = 0
    INSERT INTO Configuracion (Id, CostosFijosMensuales) VALUES (1, 3000.00);

-- Comprobación: los 8 productos de ejemplo deben existir
IF (SELECT COUNT(*) FROM Productos p INNER JOIN @Base b ON b.Codigo = p.Codigo) <> 8
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Falta algun producto de ejemplo. No se cambio nada.', 16, 1);
    RETURN;
END

COMMIT TRANSACTION;

SELECT Codigo, Stock, StockMinimo, PrecioCompra, PrecioVenta, Activo FROM Productos ORDER BY Id;
GO
