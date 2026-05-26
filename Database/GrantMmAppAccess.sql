IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'mm')
    CREATE USER [mm] FROM EXTERNAL PROVIDER;
GO
IF IS_ROLEMEMBER('db_datareader', 'mm') = 0
    ALTER ROLE db_datareader ADD MEMBER [mm];
GO
IF IS_ROLEMEMBER('db_datawriter', 'mm') = 0
    ALTER ROLE db_datawriter ADD MEMBER [mm];
GO
IF IS_ROLEMEMBER('db_ddladmin', 'mm') = 0
    ALTER ROLE db_ddladmin ADD MEMBER [mm];
GO
SELECT name, type_desc FROM sys.database_principals WHERE name = N'mm';
GO
