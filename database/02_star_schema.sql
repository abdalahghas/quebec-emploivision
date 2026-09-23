-- Québec EmploiVision — 02 : schéma en étoile, staging, historique des imports.
-- USE EmploiVision; doit précéder l'exécution de ce script.

USE EmploiVision;
GO

-- ============================================================
-- DIMENSIONS
-- ============================================================

IF OBJECT_ID('DimRegion', 'U') IS NULL
CREATE TABLE DimRegion (
    RegionId    INT IDENTITY(1,1) PRIMARY KEY,
    RegionCode  NVARCHAR(10)  NOT NULL UNIQUE,
    RegionName  NVARCHAR(100) NOT NULL UNIQUE
);
GO

IF OBJECT_ID('DimProfession', 'U') IS NULL
CREATE TABLE DimProfession (
    ProfessionId    INT IDENTITY(1,1) PRIMARY KEY,
    ProfessionCode  NVARCHAR(10)  NOT NULL UNIQUE,   -- CNP 2021
    ProfessionName  NVARCHAR(150) NOT NULL
);
GO

IF OBJECT_ID('DimIndustry', 'U') IS NULL
CREATE TABLE DimIndustry (
    IndustryId    INT IDENTITY(1,1) PRIMARY KEY,
    IndustryCode  NVARCHAR(10)  NOT NULL UNIQUE,    -- SCIAN
    IndustryName  NVARCHAR(150) NOT NULL
);
GO

-- DimDate générée une fois : une ligne par année ET par mois (DateId = AAAAMM).
IF OBJECT_ID('DimDate', 'U') IS NULL
CREATE TABLE DimDate (
    DateId  INT PRIMARY KEY,        -- ex : 202506 pour juin 2025
    [Year]  INT NOT NULL,
    Quarter INT NOT NULL,
    [Month] INT NOT NULL
);
GO

IF OBJECT_ID('DimDate', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM DimDate)
BEGIN
    DECLARE @y INT = 2015;
    WHILE @y <= 2030
    BEGIN
        DECLARE @m INT = 1;
        WHILE @m <= 12
        BEGIN
            INSERT INTO DimDate (DateId, [Year], Quarter, [Month])
            VALUES (@y * 100 + @m, @y, (@m - 1) / 3 + 1, @m);
            SET @m += 1;
        END
        SET @y += 1;
    END
END
GO

-- ============================================================
-- TABLE DE FAITS + STAGING
-- ============================================================

IF OBJECT_ID('FactLabourMarket', 'U') IS NULL
CREATE TABLE FactLabourMarket (
    FactId           INT IDENTITY(1,1) PRIMARY KEY,
    DateId           INT NOT NULL FOREIGN KEY REFERENCES DimDate(DateId),
    RegionId         INT NOT NULL FOREIGN KEY REFERENCES DimRegion(RegionId),
    ProfessionId     INT NULL     FOREIGN KEY REFERENCES DimProfession(ProfessionId),
    IndustryId       INT NULL     FOREIGN KEY REFERENCES DimIndustry(IndustryId),
    EmploymentCount  INT NULL,
    VacancyCount     INT NULL,
    AverageWage      DECIMAL(10,2) NULL,
    UnemploymentRate DECIMAL(5,2) NULL,
    ImportRunId      INT NOT NULL,
    CONSTRAINT UQ_Observation UNIQUE (DateId, RegionId, ProfessionId, IndustryId)
);
GO

-- Zone d'atterrissage des lignes validées avant chargement définitif.
IF OBJECT_ID('StagingLabourObservation', 'U') IS NULL
CREATE TABLE StagingLabourObservation (
    RegionCode       NVARCHAR(10),
    ProfessionCode   NVARCHAR(10),
    IndustryCode     NVARCHAR(10),
    [Year]           INT,
    [Month]          INT,
    EmploymentCount  INT NULL,
    VacancyCount     INT NULL,
    AverageWage      DECIMAL(10,2) NULL,
    UnemploymentRate DECIMAL(5,2) NULL
);
GO

-- ============================================================
-- HISTORIQUE DES IMPORTS + ERREURS
-- ============================================================

IF OBJECT_ID('ImportRun', 'U') IS NULL
CREATE TABLE ImportRun (
    ImportRunId   INT IDENTITY(1,1) PRIMARY KEY,
    DatasetName   NVARCHAR(100) NOT NULL,
    SourceFileName NVARCHAR(260) NOT NULL,
    StartedAt     DATETIME2 NOT NULL,
    FinishedAt    DATETIME2 NOT NULL,
    RowsProcessed INT NOT NULL,
    RowsAccepted  INT NOT NULL,
    RowsRejected  INT NOT NULL,
    RowsWarning   INT NOT NULL DEFAULT 0,
    Status        NVARCHAR(20) NOT NULL   -- SUCCESS / WARNING / FAILED
);
GO

IF OBJECT_ID('ImportRowError', 'U') IS NULL
CREATE TABLE ImportRowError (
    ImportRowErrorId INT IDENTITY(1,1) PRIMARY KEY,
    ImportRunId      INT NOT NULL FOREIGN KEY REFERENCES ImportRun(ImportRunId),
    RowNumber        INT NOT NULL,
    ColumnName       NVARCHAR(50) NOT NULL,
    Expected         NVARCHAR(200) NOT NULL,
    Received         NVARCHAR(200) NOT NULL,
    Severity         NVARCHAR(20) NOT NULL,
    Message          NVARCHAR(500) NOT NULL
);
GO

-- ============================================================
-- VUES ET INDEX
-- ============================================================

IF OBJECT_ID('vw_FactDetails', 'V') IS NULL
EXEC('CREATE VIEW vw_FactDetails AS
    SELECT f.FactId, f.DateId, f.EmploymentCount, f.VacancyCount,
           f.AverageWage, f.UnemploymentRate, f.ImportRunId,
           d.[Year], d.Quarter, d.[Month],
           r.RegionCode, r.RegionName,
           p.ProfessionCode, p.ProfessionName,
           i.IndustryCode, i.IndustryName
    FROM FactLabourMarket f
    JOIN DimDate d       ON d.DateId = f.DateId
    JOIN DimRegion r     ON r.RegionId = f.RegionId
    LEFT JOIN DimProfession p ON p.ProfessionId = f.ProfessionId
    LEFT JOIN DimIndustry i   ON i.IndustryId = f.IndustryId;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Region_Year')
    CREATE INDEX IX_Fact_Region_Year ON FactLabourMarket (RegionId, DateId)
        INCLUDE (EmploymentCount, VacancyCount, AverageWage);
GO

-- Vue analytique prête pour Power BI (DirectQuery ou import).
IF OBJECT_ID('vw_PowerBI_Base', 'V') IS NULL
EXEC('CREATE VIEW vw_PowerBI_Base AS
    SELECT r.RegionName, p.ProfessionName, i.IndustryName,
           d.[Year], d.[Month], d.Quarter,
           f.EmploymentCount, f.VacancyCount, f.AverageWage, f.UnemploymentRate
    FROM FactLabourMarket f
    JOIN DimDate d ON d.DateId = f.DateId
    JOIN DimRegion r ON r.RegionId = f.RegionId
    LEFT JOIN DimProfession p ON p.ProfessionId = f.ProfessionId
    LEFT JOIN DimIndustry i ON i.IndustryId = f.IndustryId;');
GO

-- ============================================================
-- PROCÉDURES
-- ============================================================

-- Charge le staging vers la table de faits en résolvant les clés des dimensions.
IF OBJECT_ID('usp_LoadStagingToFact', 'P') IS NULL
EXEC('CREATE PROCEDURE usp_LoadStagingToFact AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE usp_LoadStagingToFact
    @ImportRunId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RunId INT = ISNULL(@ImportRunId,
        (SELECT MAX(ImportRunId) FROM ImportRun));

    -- MERGE idempotent : une observation existante est mise à jour,
    -- une nouvelle observation est insérée. La contrainte UQ_Observation
    -- (DateId, RegionId, ProfessionId, IndustryId) sert de critère.
    MERGE FactLabourMarket AS target
    USING (
        SELECT d.DateId, r.RegionId, p.ProfessionId, i.IndustryId,
               s.EmploymentCount, s.VacancyCount, s.AverageWage, s.UnemploymentRate
        FROM StagingLabourObservation s
        JOIN DimRegion r     ON r.RegionCode = s.RegionCode
        JOIN DimDate d      ON d.[Year] = s.[Year] AND d.[Month] = ISNULL(s.[Month], 1)
        LEFT JOIN DimProfession p ON p.ProfessionCode = s.ProfessionCode
        LEFT JOIN DimIndustry i   ON i.IndustryCode = s.IndustryCode
    ) AS source
    ON target.DateId = source.DateId
       AND target.RegionId = source.RegionId
       AND ISNULL(target.ProfessionId, -1) = ISNULL(source.ProfessionId, -1)
       AND ISNULL(target.IndustryId, -1) = ISNULL(source.IndustryId, -1)
    WHEN MATCHED THEN UPDATE SET
        target.EmploymentCount = source.EmploymentCount,
        target.VacancyCount = source.VacancyCount,
        target.AverageWage = source.AverageWage,
        target.UnemploymentRate = source.UnemploymentRate,
        target.ImportRunId = @RunId
    WHEN NOT MATCHED THEN INSERT
        (DateId, RegionId, ProfessionId, IndustryId,
         EmploymentCount, VacancyCount, AverageWage, UnemploymentRate, ImportRunId)
    VALUES
        (source.DateId, source.RegionId, source.ProfessionId, source.IndustryId,
         source.EmploymentCount, source.VacancyCount, source.AverageWage,
         source.UnemploymentRate, @RunId);
END
GO
