-- Migration: Add structured CV fields to DonneesCvs table
-- This script adds the new fields required for the structured CV form

-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'Competences')
BEGIN
    ALTER TABLE DonneesCvs ADD Competences NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'Experience')
BEGIN
    ALTER TABLE DonneesCvs ADD Experience NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'NiveauEducation')
BEGIN
    ALTER TABLE DonneesCvs ADD NiveauEducation NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'AutresInfos')
BEGIN
    ALTER TABLE DonneesCvs ADD AutresInfos NVARCHAR(MAX) NULL;
END

-- Update existing columns to be non-nullable with default values if needed
-- Note: Existing data will have empty strings for NomCandidat and Email
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'NomCandidat' AND IS_NULLABLE = 'YES')
BEGIN
    -- Update existing NULL values to empty string
    UPDATE DonneesCvs SET NomCandidat = '' WHERE NomCandidat IS NULL;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'DonneesCvs' AND COLUMN_NAME = 'Email' AND IS_NULLABLE = 'YES')
BEGIN
    -- Update existing NULL values to empty string
    UPDATE DonneesCvs SET Email = '' WHERE Email IS NULL;
END

PRINT 'Migration completed successfully: Structured CV fields added to DonneesCvs table.';
