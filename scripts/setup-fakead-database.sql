-- FakeAd Database Setup and Seed Script
-- Creates the FakeAdDb database, schema, and sample data.
-- Idempotent: safe to run multiple times.
-- Compatible with SQL Server 2019+
--
-- This database acts as a substitute for Active Directory during development.
-- The FakeAdService reads membership data from these tables instead of querying AD.

USE master;
GO

-- ============================================================
-- Create database if it does not exist
-- ============================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FakeAdDb')
BEGIN
    CREATE DATABASE FakeAdDb;
    PRINT 'Database FakeAdDb created.';
END
ELSE
    PRINT 'Database FakeAdDb already exists.';
GO

USE FakeAdDb;
GO

-- ============================================================
-- AdUsers
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdUsers')
BEGIN
    CREATE TABLE AdUsers (
        Id   INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        CONSTRAINT UQ_AdUsers_Name UNIQUE (Name)
    );
    PRINT 'Table AdUsers created.';
END
GO

-- ============================================================
-- AdGroups
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdGroups')
BEGIN
    CREATE TABLE AdGroups (
        Id   INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        CONSTRAINT UQ_AdGroups_Name UNIQUE (Name)
    );
    PRINT 'Table AdGroups created.';
END
GO

-- ============================================================
-- AdUserGroupMemberships  (many-to-many: AdUser <-> AdGroup)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdUserGroupMemberships')
BEGIN
    CREATE TABLE AdUserGroupMemberships (
        UserId  INT NOT NULL,
        GroupId INT NOT NULL,
        CONSTRAINT PK_AdUserGroupMemberships PRIMARY KEY (UserId, GroupId),
        CONSTRAINT FK_AdUserGroupMemberships_AdUsers
            FOREIGN KEY (UserId)  REFERENCES AdUsers  (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AdUserGroupMemberships_AdGroups
            FOREIGN KEY (GroupId) REFERENCES AdGroups (Id) ON DELETE CASCADE
    );
    PRINT 'Table AdUserGroupMemberships created.';
END
GO

-- ============================================================
-- AdGroupGroupMemberships  (many-to-many: parent AdGroup <-> child AdGroup)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdGroupGroupMemberships')
BEGIN
    CREATE TABLE AdGroupGroupMemberships (
        ParentGroupId INT NOT NULL,
        ChildGroupId  INT NOT NULL,
        CONSTRAINT PK_AdGroupGroupMemberships PRIMARY KEY (ParentGroupId, ChildGroupId),
        CONSTRAINT FK_AdGroupGroupMemberships_Parent
            FOREIGN KEY (ParentGroupId) REFERENCES AdGroups (Id),
        CONSTRAINT FK_AdGroupGroupMemberships_Child
            FOREIGN KEY (ChildGroupId)  REFERENCES AdGroups (Id)
    );
    PRINT 'Table AdGroupGroupMemberships created.';
END
GO

-- ============================================================
-- Seed: Users
-- ============================================================
MERGE AdUsers AS target
USING (VALUES
    ('alice'),
    ('bob'),
    ('charlie'),
    ('dave'),
    ('eve')
) AS source (Name)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (source.Name);
GO

-- ============================================================
-- Seed: Groups
-- ============================================================
MERGE AdGroups AS target
USING (VALUES
    ('Developers'),
    ('Admins'),
    ('DevOps'),
    ('ReadOnly'),
    ('IT-Management')
) AS source (Name)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (source.Name);
GO

-- ============================================================
-- Seed: User → Group memberships
-- ============================================================
MERGE AdUserGroupMemberships AS target
USING (
    SELECT u.Id AS UserId, g.Id AS GroupId
    FROM (VALUES
        ('alice',   'Developers'),
        ('alice',   'DevOps'),
        ('bob',     'Developers'),
        ('bob',     'ReadOnly'),
        ('charlie', 'Admins'),
        ('charlie', 'IT-Management'),
        ('dave',    'DevOps'),
        ('dave',    'ReadOnly'),
        ('eve',     'Admins'),
        ('eve',     'ReadOnly')
    ) AS pairs (UserName, GroupName)
    INNER JOIN AdUsers  u ON u.Name = pairs.UserName
    INNER JOIN AdGroups g ON g.Name = pairs.GroupName
) AS source
ON target.UserId = source.UserId AND target.GroupId = source.GroupId
WHEN NOT MATCHED THEN
    INSERT (UserId, GroupId) VALUES (source.UserId, source.GroupId);
GO

-- ============================================================
-- Seed: Group → Group hierarchy
-- ============================================================
MERGE AdGroupGroupMemberships AS target
USING (
    SELECT p.Id AS ParentGroupId, c.Id AS ChildGroupId
    FROM (VALUES
        ('IT-Management', 'Admins'),
        ('IT-Management', 'DevOps'),
        ('DevOps',        'Developers')
    ) AS pairs (ParentName, ChildName)
    INNER JOIN AdGroups p ON p.Name = pairs.ParentName
    INNER JOIN AdGroups c ON c.Name = pairs.ChildName
) AS source
ON target.ParentGroupId = source.ParentGroupId AND target.ChildGroupId = source.ChildGroupId
WHEN NOT MATCHED THEN
    INSERT (ParentGroupId, ChildGroupId)
    VALUES (source.ParentGroupId, source.ChildGroupId);
GO

PRINT 'FakeAd database setup and seed complete.';
GO
