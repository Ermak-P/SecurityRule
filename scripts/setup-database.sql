-- SecurityRule Database Setup Script
-- Idempotent: safe to run multiple times.
-- Compatible with SQL Server 2019+

USE master;
GO

-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SecurityRuleDb')
BEGIN
    CREATE DATABASE SecurityRuleDb;
    PRINT 'Database SecurityRuleDb created.';
END
ELSE
    PRINT 'Database SecurityRuleDb already exists.';
GO

USE SecurityRuleDb;
GO

-- ============================================================
-- Servers
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Servers')
BEGIN
    CREATE TABLE Servers (
        Id              INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(200)   NOT NULL,
        IpAddress       NVARCHAR(45)    NOT NULL,
        OperatingSystem NVARCHAR(100)   NOT NULL
    );
    PRINT 'Table Servers created.';
END
GO

-- ============================================================
-- AppServices
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppServices')
BEGIN
    CREATE TABLE AppServices (
        Id            INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name          NVARCHAR(200) NOT NULL,
        AdAccountName NVARCHAR(200) NOT NULL
    );
    PRINT 'Table AppServices created.';
END
GO

-- ============================================================
-- ServerServices  (many-to-many join: Server <-> AppService)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServerServices')
BEGIN
    CREATE TABLE ServerServices (
        ServicesId INT NOT NULL,
        ServersId  INT NOT NULL,
        CONSTRAINT PK_ServerServices PRIMARY KEY (ServicesId, ServersId),
        CONSTRAINT FK_ServerServices_AppServices FOREIGN KEY (ServicesId)
            REFERENCES AppServices (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServerServices_Servers FOREIGN KEY (ServersId)
            REFERENCES Servers (Id) ON DELETE CASCADE
    );
    PRINT 'Table ServerServices created.';
END
GO

-- ============================================================
-- Certificates
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Certificates')
BEGIN
    CREATE TABLE Certificates (
        Id          INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        IssuedAt    DATETIME2       NOT NULL,
        ExpiresAt   DATETIME2       NOT NULL,
        Description NVARCHAR(1000)  NULL
    );
    PRINT 'Table Certificates created.';
END
GO

-- ============================================================
-- ServiceCertificates  (many-to-many join: AppService <-> Certificate)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceCertificates')
BEGIN
    CREATE TABLE ServiceCertificates (
        ServicesId     INT NOT NULL,
        CertificatesId INT NOT NULL,
        CONSTRAINT PK_ServiceCertificates PRIMARY KEY (ServicesId, CertificatesId),
        CONSTRAINT FK_ServiceCertificates_AppServices FOREIGN KEY (ServicesId)
            REFERENCES AppServices (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceCertificates_Certificates FOREIGN KEY (CertificatesId)
            REFERENCES Certificates (Id) ON DELETE CASCADE
    );
    PRINT 'Table ServiceCertificates created.';
END
GO

-- ============================================================
-- FirewallRules
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FirewallRules')
BEGIN
    CREATE TABLE FirewallRules (
        Id            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        SourceIp      NVARCHAR(45)   NOT NULL,
        DestinationIp NVARCHAR(45)   NOT NULL,
        ExpiresAt     DATETIME2      NOT NULL,
        Description   NVARCHAR(1000) NULL
    );
    PRINT 'Table FirewallRules created.';
END
GO

-- ============================================================
-- EF Core migrations history table (required by EF runtime)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL PRIMARY KEY,
        ProductVersion NVARCHAR(32)  NOT NULL
    );
    PRINT 'Table __EFMigrationsHistory created.';
END
GO

PRINT 'Setup complete.';
GO
