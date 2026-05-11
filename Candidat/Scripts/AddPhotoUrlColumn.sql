-- Add PhotoUrl column to Utilisateur table
-- This script adds support for candidate profile photo uploads

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Utilisateur') AND name = 'PhotoUrl')
BEGIN
    ALTER TABLE Utilisateur
    ADD PhotoUrl NVARCHAR(500) NULL;
    
    PRINT 'PhotoUrl column added successfully to Utilisateur table.'
END
ELSE
BEGIN
    PRINT 'PhotoUrl column already exists in Utilisateur table.'
END
GO
