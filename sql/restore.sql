IF DB_ID('Electronics_Shop') IS NULL
BEGIN
    RESTORE DATABASE Electronics_Shop
    FROM DISK = '/var/opt/mssql/backup/techstore.bak'
    WITH MOVE 'Electronics_Shop' TO '/var/opt/mssql/data/Electronics_Shop.mdf',
         MOVE 'Electronics_Shop_log' TO '/var/opt/mssql/data/Electronics_Shop_log.ldf',
         REPLACE;
END
GO
