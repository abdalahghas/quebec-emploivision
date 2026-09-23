-- Québec EmploiVision — 01 : création de la base
-- Exécuter dans SQL Server Management Studio ou sqlcmd.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'EmploiVision')
BEGIN
    CREATE DATABASE EmploiVision;
END
GO
