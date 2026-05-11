-- Check and add missing columns to Cv table
USE Cvparsing;
GO

-- Add NomCandidat if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'NomCandidat')
BEGIN
    ALTER TABLE Cv ADD NomCandidat NVARCHAR(200) NULL;
    PRINT 'Added NomCandidat';
END
GO

-- Add Email if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'Email')
BEGIN
    ALTER TABLE Cv ADD Email NVARCHAR(200) NULL;
    PRINT 'Added Email';
END
GO

-- Add Telephone if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'Telephone')
BEGIN
    ALTER TABLE Cv ADD Telephone NVARCHAR(50) NULL;
    PRINT 'Added Telephone';
END
GO

-- Add Competences if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'Competences')
BEGIN
    ALTER TABLE Cv ADD Competences NVARCHAR(MAX) NULL;
    PRINT 'Added Competences';
END
GO

-- Add Experience if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'Experience')
BEGIN
    ALTER TABLE Cv ADD Experience NVARCHAR(MAX) NULL;
    PRINT 'Added Experience';
END
GO

-- Add NiveauEducation if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'NiveauEducation')
BEGIN
    ALTER TABLE Cv ADD NiveauEducation NVARCHAR(MAX) NULL;
    PRINT 'Added NiveauEducation';
END
GO

-- Add AutresInfos if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cv') AND name = 'AutresInfos')
BEGIN
    ALTER TABLE Cv ADD AutresInfos NVARCHAR(MAX) NULL;
    PRINT 'Added AutresInfos';
END
GO

PRINT 'All columns added successfully!';
GO
