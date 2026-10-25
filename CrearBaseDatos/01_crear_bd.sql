/* 01_crear_bd.sql - Ferreteria "El Tornillo Feliz" (empresa ficticia, datos simulados).
   Crea la base de datos, las 4 tablas y 8 productos de ejemplo.
   Ejecutar UNA vez en SSMS (localhost\SQLEXPRESS). Si las tablas ya existen, fallara: use 02_reiniciar_datos_prueba.sql. */
IF DB_ID('FerreteriaTornilloFeliz') IS NULL
    CREATE DATABASE FerreteriaTornilloFeliz;
GO
USE FerreteriaTornilloFeliz;
GO

CREATE TABLE Productos (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Codigo        VARCHAR(20)   NOT NULL UNIQUE,
    Nombre        VARCHAR(100)  NOT NULL,
    Categoria     VARCHAR(50)   NOT NULL,
    PrecioCompra  DECIMAL(10,2) NOT NULL CHECK (PrecioCompra >= 0),
    PrecioVenta   DECIMAL(10,2) NOT NULL CHECK (PrecioVenta >= 0),
    Stock         INT           NOT NULL CHECK (Stock >= 0),
    StockMinimo   INT           NOT NULL CHECK (StockMinimo >= 0),
    Activo        BIT           NOT NULL DEFAULT 1
);

CREATE TABLE Ventas (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Fecha    DATETIME      NOT NULL DEFAULT GETDATE(),
    Total    DECIMAL(12,2) NOT NULL CHECK (Total >= 0),
    Anulada  BIT           NOT NULL DEFAULT 0
);

CREATE TABLE DetalleVenta (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    IdVenta         INT           NOT NULL REFERENCES Ventas(Id),
    IdProducto      INT           NOT NULL REFERENCES Productos(Id),
    Cantidad        INT           NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(10,2) NOT NULL,
    CostoUnitario   DECIMAL(10,2) NOT NULL
);

CREATE TABLE Configuracion (
    Id                    INT PRIMARY KEY CHECK (Id = 1),
    CostosFijosMensuales  DECIMAL(12,2) NOT NULL CHECK (CostosFijosMensuales >= 0)
);
INSERT INTO Configuracion (Id, CostosFijosMensuales) VALUES (1, 3000.00);  -- dato simulado

-- Datos simulados
INSERT INTO Productos (Codigo, Nombre, Categoria, PrecioCompra, PrecioVenta, Stock, StockMinimo) VALUES
('MAR-001', 'Martillo de acero 16 oz',      'Herramientas', 45.00,  65.00, 20, 5),
('DES-001', 'Destornillador plano',         'Herramientas', 12.00,  20.00, 40, 10),
('CLA-001', 'Clavos 2 pulgadas (libra)',    'Tornillería',   6.00,  10.00, 100, 20),
('TOR-001', 'Tornillos para madera (caja)', 'Tornillería',  15.00,  25.00, 8,  10),
('PIN-001', 'Pintura blanca galón',         'Pintura',      95.00, 130.00, 12, 4),
('CAB-001', 'Cable eléctrico #12 (metro)',  'Eléctrico',     5.00,   8.50, 150, 30),
('FOC-001', 'Foco LED 9W',                  'Eléctrico',    14.00,  22.00, 3,  8),
('BRO-001', 'Brocha 3 pulgadas',            'Pintura',      10.00,  18.00, 25, 5);
