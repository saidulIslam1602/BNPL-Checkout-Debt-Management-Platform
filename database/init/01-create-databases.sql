-- Create databases for Riverty BNPL Platform
-- This script creates all necessary databases for the microservices

USE master;
GO

-- Create Payment Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RivertyBNPL_Payment')
BEGIN
    CREATE DATABASE RivertyBNPL_Payment;
    PRINT 'Created RivertyBNPL_Payment database';
END
ELSE
BEGIN
    PRINT 'RivertyBNPL_Payment database already exists';
END
GO

-- Create Risk Assessment Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RivertyBNPL_Risk')
BEGIN
    CREATE DATABASE RivertyBNPL_Risk;
    PRINT 'Created RivertyBNPL_Risk database';
END
ELSE
BEGIN
    PRINT 'RivertyBNPL_Risk database already exists';
END
GO

-- Create Settlement Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RivertyBNPL_Settlement')
BEGIN
    CREATE DATABASE RivertyBNPL_Settlement;
    PRINT 'Created RivertyBNPL_Settlement database';
END
ELSE
BEGIN
    PRINT 'RivertyBNPL_Settlement database already exists';
END
GO

-- Create Notification Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RivertyBNPL_Notification')
BEGIN
    CREATE DATABASE RivertyBNPL_Notification;
    PRINT 'Created RivertyBNPL_Notification database';
END
ELSE
BEGIN
    PRINT 'RivertyBNPL_Notification database already exists';
END
GO

-- Create Health Checks Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RivertyBNPL_HealthChecks')
BEGIN
    CREATE DATABASE RivertyBNPL_HealthChecks;
    PRINT 'Created RivertyBNPL_HealthChecks database';
END
ELSE
BEGIN
    PRINT 'RivertyBNPL_HealthChecks database already exists';
END
GO

-- Configure database settings for optimal performance
ALTER DATABASE RivertyBNPL_Payment SET RECOVERY FULL;
ALTER DATABASE RivertyBNPL_Risk SET RECOVERY FULL;
ALTER DATABASE RivertyBNPL_Settlement SET RECOVERY FULL;
ALTER DATABASE RivertyBNPL_Notification SET RECOVERY FULL;
ALTER DATABASE RivertyBNPL_HealthChecks SET RECOVERY SIMPLE;

-- Set compatibility level to SQL Server 2022
ALTER DATABASE RivertyBNPL_Payment SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE RivertyBNPL_Risk SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE RivertyBNPL_Settlement SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE RivertyBNPL_Notification SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE RivertyBNPL_HealthChecks SET COMPATIBILITY_LEVEL = 160;

PRINT 'Database initialization completed successfully';
GO