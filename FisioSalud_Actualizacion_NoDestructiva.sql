-- Ejecutar sobre FisioSalud. No borra ni reemplaza datos existentes.
USE [FisioSalud];
GO
IF OBJECT_ID('dbo.AjustesSistema','U') IS NULL
    CREATE TABLE dbo.AjustesSistema (Clave nvarchar(100) NOT NULL PRIMARY KEY, Valor nvarchar(max) NOT NULL);
GO
