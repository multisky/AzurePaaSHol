SELECT name, cmptlevel
FROM sys.sysdatabases
WHERE dbid = db_id();
GO

ALTER DATABASE <<database name>>
SET COMPATIBILITY_LEVEL = 130;
GO

SELECT name, cmptlevel
FROM sys.sysdatabases
WHERE dbid = db_id();
GO                                                                                                                                                                                                                            