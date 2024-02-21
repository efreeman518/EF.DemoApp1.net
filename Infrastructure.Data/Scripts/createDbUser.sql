--enable managed identity login (managed identity must exist on a resource in Azure)
CREATE USER [managed-identity-name] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [managed-identity-name];
ALTER ROLE db_datawriter ADD MEMBER [managed-identity-name];
--ALTER ROLE db_ddladmin ADD MEMBER [managed-identity-name];
grant VIEW ANY COLUMN MASTER KEY DEFINITION to [managed-identity-name]
grant VIEW ANY COLUMN ENCRYPTION KEY DEFINITION to [managed-identity-name]
--see 'create a role with execute permissions' step below if needed

USE [targetDB];
ALTER USER [targetDBUser] WITH login = [server-login];

--create server level login
--run in the master db - create the server login
USE [master]
CREATE LOGIN [appname-user/admin-env] WITH password=''
--run in the target db - create the db user from the server login
USE [db]
CREATE USER [appname-user/admin-env] FROM LOGIN [appname-user/admin-env]

--OR 
--create contained user (in specific DB only, more portable)
CREATE USER [appname-user/admin-env] WITH PASSWORD = ''

--run in target db - create a role with appropriate permissions
CREATE ROLE [role-appname-user/admin-env]
GO
--assign permissions to the service account role
ALTER ROLE [db_datareader] ADD MEMBER  [role-appname-user/admin-env];
ALTER ROLE [db_datawriter] ADD MEMBER  [role-appname-user/admin-env];
--ALTER ROLE [db_ddladmin] ADD MEMBER  [role-appname-user/admin-env]; --modify schema using DDL needed for EF migrations

--create a role with execute permissions (for existing and future stored procs)
CREATE ROLE [db_execute] AUTHORIZATION [dbo]
GO
GRANT EXECUTE TO [db_execute]
GO

ALTER ROLE [db_execute] ADD MEMBER  [role-appname-user/admin-env];

--add the user to the role
ALTER ROLE [role-appname-user/admin-env] ADD MEMBER [appname-user/admin-env];

--verify appropriate access
select m.name as Member, r.name as Role
from sys.database_role_members
inner join sys.database_principals m on sys.database_role_members.member_principal_id = m.principal_id
inner join sys.database_principals r on sys.database_role_members.role_principal_id = r.principal_id

/*
For replicated backup databases - must create server logins synced with DB (not needed for contained users)

--1 - in the primary master database get the login names and SIDs
SELECT [name], [sid]
FROM [sys].[sql_logins]
WHERE [type_desc] = 'SQL_Login'

ra-dbprod1-admin 0x01060000000001640000000000000000917C6D6724EFD345A97CE1FCE3B13590
appuser_support_prod 0x010600000000006400000000000000006B77D9FFD774914EBA488641929490FD
readonly_support_prod 0x010600000000006400000000000000005CCB09DD84B6AB40B092CF8ADFD5A2D5
appuser_docworkflow_prod 0x01060000000000640000000000000000344BE6B6B30430438BF3B39B05BC262B
readonly_docworkflow_prod 0x0106000000000064000000000000000024DB179E41A33F44A85D56B7258BD1DD

--2 - in the secondary replicated backup master db, create logins with same password and SID
CREATE LOGIN readonly_docworkflow_prod
WITH password='...',
SID = 0x0106000000000064000000000000000024DB179E41A33F44A85D56B7258BD1DD

*/