-- Create databases for YourCompany BNPL Platform
-- This script creates all necessary databases for the microservices

USE master;
GO

-- Create Payment Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'YourCompanyBNPL_Payment')
BEGIN
    CREATE DATABASE YourCompanyBNPL_Payment;
    PRINT 'Created YourCompanyBNPL_Payment database';
END
ELSE
BEGIN
    PRINT 'YourCompanyBNPL_Payment database already exists';
END
GO

-- Create Risk Assessment Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'YourCompanyBNPL_Risk')
BEGIN
    CREATE DATABASE YourCompanyBNPL_Risk;
    PRINT 'Created YourCompanyBNPL_Risk database';
END
ELSE
BEGIN
    PRINT 'YourCompanyBNPL_Risk database already exists';
END
GO

-- Create Settlement Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'YourCompanyBNPL_Settlement')
BEGIN
    CREATE DATABASE YourCompanyBNPL_Settlement;
    PRINT 'Created YourCompanyBNPL_Settlement database';
END
ELSE
BEGIN
    PRINT 'YourCompanyBNPL_Settlement database already exists';
END
GO

-- Create Notification Service Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'YourCompanyBNPL_Notification')
BEGIN
    CREATE DATABASE YourCompanyBNPL_Notification;
    PRINT 'Created YourCompanyBNPL_Notification database';
END
ELSE
BEGIN
    PRINT 'YourCompanyBNPL_Notification database already exists';
END
GO

-- Create Health Checks Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'YourCompanyBNPL_HealthChecks')
BEGIN
    CREATE DATABASE YourCompanyBNPL_HealthChecks;
    PRINT 'Created YourCompanyBNPL_HealthChecks database';
END
ELSE
BEGIN
    PRINT 'YourCompanyBNPL_HealthChecks database already exists';
END
GO

-- Configure database settings for optimal performance
ALTER DATABASE YourCompanyBNPL_Payment SET RECOVERY FULL;
ALTER DATABASE YourCompanyBNPL_Risk SET RECOVERY FULL;
ALTER DATABASE YourCompanyBNPL_Settlement SET RECOVERY FULL;
ALTER DATABASE YourCompanyBNPL_Notification SET RECOVERY FULL;
ALTER DATABASE YourCompanyBNPL_HealthChecks SET RECOVERY SIMPLE;

-- Set compatibility level to SQL Server 2022
ALTER DATABASE YourCompanyBNPL_Payment SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE YourCompanyBNPL_Risk SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE YourCompanyBNPL_Settlement SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE YourCompanyBNPL_Notification SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE YourCompanyBNPL_HealthChecks SET COMPATIBILITY_LEVEL = 160;

PRINT 'Database initialization completed successfully';
GO