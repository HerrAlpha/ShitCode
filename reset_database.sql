-- ShitCode Error Dashboard - Manual Database Reset
-- Use this if you prefer SQL commands over the bash script

-- Drop and recreate database
DROP DATABASE IF EXISTS error_log_dashboard;
CREATE DATABASE error_log_dashboard CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE error_log_dashboard;

-- The tables will be created by EF Core migrations
-- After running this, execute:
-- cd /Users/herralpha/Development/project/ShitCode/ErrorLogDashboard.Web
-- rm -rf Migrations
-- dotnet ef migrations add InitialCreate
-- dotnet ef database update
-- dotnet run

SELECT 'Database reset. Now run EF migrations.' as Status;
