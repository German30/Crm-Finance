/* =============================================================================
   crm_finance — Usuario de aplicacion
   -----------------------------------------------------------------------------
   Solo hace falta cuando la API se conecta con autenticacion de SQL Server, que es
   el caso del contenedor: desde Linux no existe Trusted_Connection.

   Crea un login con los permisos minimos que la API necesita: leer y escribir
   datos. Nada de DDL, porque el esquema lo gestionan los scripts 00_schema.sql y
   los incrementales, no la aplicacion.

   Se ejecuta con sqlcmd pasandole la contrasena como variable:

       sqlcmd -S servidor -U sa -P <clave-sa> -C \
              -v CRM_APP_PASSWORD="<clave-app>" -i 00_app_user.sql

   Ejecutar DESPUES de 00_schema.sql. Idempotente.
   ============================================================================= */

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'crm_app')
BEGIN
    DECLARE @sql nvarchar(max) =
        N'CREATE LOGIN crm_app WITH PASSWORD = ' + QUOTENAME(N'$(CRM_APP_PASSWORD)', '''') + N';';
    EXEC sp_executesql @sql;
END
GO

USE crm_finance;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'crm_app')
BEGIN
    CREATE USER crm_app FOR LOGIN crm_app;

    ALTER ROLE db_datareader ADD MEMBER crm_app;
    ALTER ROLE db_datawriter ADD MEMBER crm_app;
END
GO

SELECT dp.name AS usuario, r.name AS rol
FROM sys.database_role_members m
JOIN sys.database_principals dp ON dp.principal_id = m.member_principal_id
JOIN sys.database_principals r  ON r.principal_id  = m.role_principal_id
WHERE dp.name = 'crm_app';
GO
