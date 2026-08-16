---
layout: chapter
title: "Microsoft SQL Server"
number: 8
part: 3
---

All sql scripts included in this section expect to be run in your preferred sql tool. If you need to install sql server skip to [SQL - Install](#sql-install).

Azure Data Studio, which earlier versions of this text recommended, was retired by Microsoft and stopped receiving support in February 2026. See [Client tools](#client-tools) for what to use instead.

## Adventure Works

While many of the sql examples shown will not use the adventure works sample database I suggest that it is restored and used to investigate sql server.

For a more detailed sample database download and restore [Microsoft's AdventureWorks database](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure?view=sql-server-ver16&tabs=ssms).

Before restoring the bak change the owner to mssql and move it to a folder that sql server has permissions to access.

```bash
sudo mkdir -p /var/opt/mssql/backup/
sudo chown mssql /var/opt/mssql/backup/
sudo chgrp mssql /var/opt/mssql/backup/
chown mssql AdventureWorksLT2019.bak
chgrp mssql AdventureWorksLT2019.bak
sudo mv AdventureWorksLT2019.bak  /var/opt/mssql/backup/
```

Find the logical names

```sql
USE [master];
GO
RESTORE FILELISTONLY
FROM DISK = '/var/opt/mssql/backup/AdventureWorksLT2019.bak'
```

Restore the database.

```sql
USE [master];
GO
RESTORE DATABASE [AdventureWorks2019]
FROM DISK = '/var/opt/mssql/backup/AdventureWorksLT2019.bak'
WITH
    MOVE 'AdventureWorksLT2012_Data' TO '/var/opt/mssql/data/AdventureWorks2019.mdf',
    MOVE 'AdventureWorksLT2012_Log' TO '/var/opt/mssql/data/AdventureWorks2019_log.ldf',
    FILE = 1,
    NOUNLOAD,
    STATS = 5;
GO
```

## Create a database

```sql
use master;
create database SqlPlayground;
```

## Create a table

Create a table using a UNIQUEIDENTIFIER (sequential guid) column as the primary key.

```sql
use SqlPlayground;

create table [dbo].[TvShows]
(
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ShowName nvarchar(50) not null,
    ShowLength int not null,
    Summary nvarchar(max) not null,
    Rating decimal(18,2) null,
    Episode nvarchar(200) not null,
    ParentalGuide nvarchar(5) null
)
```

As an alternative, create the table with a bigint identity column as the primary key.

```sql
create table [dbo].[TvShows]
(
    Id BIGINT NOT NULL IDENTITY PRIMARY KEY,
    ShowName nvarchar(50) not null,
    ShowLength int not null,
    Summary nvarchar(max) not null,
    Rating decimal(18,2) null,
    Episode nvarchar(200) not null,
    ParentalGuide nvarchar(5) null
)
```

Note: schemas, tables, and column names can be surrounded in square brackets []. This is for when special characters or reserved keywords are part of the name.

## Alter a table

```sql
alter table TvShows
add FirstAiredUtc DateTime;
```

## Create indexes

Review [Clustered and nonclustered indexes described](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/clustered-and-nonclustered-indexes-described?view=sql-server-ver16) and [CREATE INDEX](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql?view=sql-server-ver16).

```sql
create index index_tvshows_showname ON dbo.TvShows (ShowName);
```

## SELECT

```sql
select * from TvShows;
select * from TvShows where ShowName = 'Dexter';
select Id, ShowName, ShowLength, Summary, FirstAiredUtc
from TvShows;
```

## INSERT

Insert a new row into a table.

```sql
insert into TvShows (ShowName, ShowLength, Summary, Rating, Episode, ParentalGuide)
values ('Frasier', '30', 'Frasier goes home.', 4.56, '1e01', 'PG');
```

Insert a new row into a table and select back the new unique identifier of that row.

```sql
declare @InsertedRowIds table(InsertedId UNIQUEIDENTIFIER);

insert into TvShows (ShowName, ShowLength, Summary, Rating, Episode, ParentalGuide)
OUTPUT inserted.Id INTO  @InsertedRowIds(InsertedId)
values ('Frasier', '30', 'Frasier does it again.', 3.68, '2e01', 'PG');

select * FROM @InsertedRowIds;
```

Insert a new row into a table that uses a bigint identity column and select back the new id of that row.

```sql
insert into TvShows (ShowName, ShowLength, Summary, Rating, Episode, ParentalGuide)
values ('Frasier', '30', 'Frasier does it again.', 3.68, '2e01', 'PG');

select SCOPE_IDENTITY();
```

Further reading

- [INSERT](https://learn.microsoft.com/en-us/sql/t-sql/statements/insert-transact-sql?view=sql-server-ver16)
- [OUTPUT clause](https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql?view=sql-server-ver16)

## UPDATE

When executing updates be sure to include a where clause to avoid updating every record in a table.

```sql
update TvShows set ParentalGuide = 'PG13' where ShowName='Friends';
update TvShows set ParentalGuide = 'PG' where ShowName = 'Frasier';
update TvShows set ParentalGuide = '18A' where ShowName = 'Dexter';
update TvShows set ParentalGuide = 'PG' where ShowName in ('Friends', 'Frasier');
```

## DELETE

When executing deletes be sure to include a where clause to avoid deleting every record in a table.

```sql
delete from TvShows where ShowName = 'Dexter';
```

## Foreign Keys

A **foreign key** is a constraint that enforces a relationship between columns in two tables, ensuring that the value in one table matches a value in another. This maintains referential integrity between related data.

For example, suppose you have a `TvShows` table and an `Episodes` table. Each episode references a TV show by its `TvShowId`:

```sql
CREATE TABLE TvShows (
    Id BIGINT NOT NULL IDENTITY PRIMARY KEY,
    ShowName NVARCHAR(50) NOT NULL
);

CREATE TABLE Episodes (
    Id BIGINT NOT NULL IDENTITY PRIMARY KEY,
    TvShowId BIGINT NOT NULL,
    EpisodeName NVARCHAR(100) NOT NULL,
    FOREIGN KEY (TvShowId) REFERENCES TvShows(Id)
);
```

In this example, `Episodes.TvShowId` must match an existing `TvShows.Id`, ensuring episodes are always linked to a valid TV show.

## JOIN

A **JOIN** in SQL combines rows from two or more tables based on a related column between them. The most common type is an **INNER JOIN**, which returns only the rows where there is a match in both tables.

**Example:**

Suppose you have `TvShows` and `Episodes` tables. To list all episodes with their show names:

```sql
SELECT
    TvShows.ShowName,
    Episodes.EpisodeName
FROM
    TvShows
INNER JOIN
    Episodes ON TvShows.Id = Episodes.TvShowId;
```

This query returns each episode along with the name of its TV show.

## CTE

A **Common Table Expression (CTE)** is a temporary result set in SQL that you can reference within a `SELECT`, `INSERT`, `UPDATE`, or `DELETE` statement. CTEs make complex queries easier to read and maintain, and are especially useful for recursive queries or breaking down large queries into logical building blocks.

**Example:**

Suppose you want to select all TV shows with a rating above 4.0 and then count them.

```sql
WITH HighRatedShows AS (
    SELECT *
    FROM TvShows
    WHERE Rating > 4.0
)
SELECT COUNT(*) AS HighRatedShowCount
FROM HighRatedShows;
```

In this example, the CTE `HighRatedShows` selects all shows with a rating above 4.0, and the main query counts how many such shows exist.

## Stored Procedures

A **stored procedure** in SQL Server is a precompiled collection of one or more T-SQL statements that can be executed as a single unit. Stored procedures help encapsulate logic, improve performance, and promote code reuse.

**Example:**

This stored procedure selects all TV shows with a rating above a specified value:

```sql
CREATE PROCEDURE GetHighRatedTvShows
    @MinRating DECIMAL(18,2)
AS
BEGIN
    SELECT *
    FROM TvShows
    WHERE Rating >= @MinRating;
END
```

To execute the procedure:

```sql
EXEC GetHighRatedTvShows @MinRating = 4.5;
```

This will return all rows from `TvShows` where the `Rating` is 4.5 or higher.

## Stored Functions

A **stored function** in SQL Server is a user-defined function (UDF) that returns a single value or a table. Functions can be used in queries, computed columns, or as part of expressions. Unlike stored procedures, functions must return a value and cannot modify database state (no `INSERT`, `UPDATE`, or `DELETE`).

**Example:**  
This scalar-valued function returns the full name of a TV show episode by combining the show name and episode name.

```sql
CREATE FUNCTION dbo.GetFullEpisodeName
(
    @ShowName NVARCHAR(50),
    @EpisodeName NVARCHAR(100)
)
RETURNS NVARCHAR(200)
AS
BEGIN
    RETURN @ShowName + ' - ' + @EpisodeName
END
```

**Usage:**

```sql
SELECT dbo.GetFullEpisodeName('Friends', 'The One Where It All Began') AS FullEpisodeName;
```

## Views

A **view** in SQL Server is a virtual table based on the result of a `SELECT` query. Views simplify complex queries, encapsulate logic, and can help restrict access to specific data.

**Example:**

Create a view that lists only TV shows with a rating above 4.0:

```sql
CREATE VIEW HighRatedTvShows AS
SELECT Id, ShowName, Rating
FROM TvShows
WHERE Rating > 4.0;
```

You can then query the view like a table:

```sql
SELECT * FROM HighRatedTvShows;
```

## SQL - Install {#sql-install}

### SQL server windows install

[Download sql server](https://www.microsoft.com/en-ca/sql-server/sql-server-downloads) from Microsoft. The simple install method is to double click the setup.exe and use the user interface to complete the install.

If it is a non production environment, for development choose the developer edition.

If you wish to automate the install it can be script with options similar to the below example.

```bash
setup.exe /ACTION=INSTALL /IACCEPTSQLSERVERLICENSETERMS /FEATURES="SQL,Tools" /SECURITYMODE=SQL /SAPWD="PLACEHOLDER, PUT A GOOD PASSWORD HERE" /SQLSVCACCOUNT="NT AUTHORITY\Network Service" /SQLSVCSTARTUPTYPE=Automatic /TCPENABLED=1 /SQLSYSADMINACCOUNTS=".\Users" ".\Administrator" /SQLCOLLATION="SQL_Latin1_General_CP1_CI_AS"
```

Review the [Install SQL Server on Windows from the command prompt](https://learn.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server-from-the-command-prompt?view=sql-server-ver16) page for up to date options and documentation.

### SQL server linux install

See [Quickstart: Install SQL Server and create a database on Ubuntu](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-ubuntu?view=sql-server-ver16) for further details.

Run these commands to install sql server 2022 on a ubuntu server.

```bash
# Download the Microsoft repository GPG keys
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/mssql-server-2022.list)"
# Update the list of packages after we added packages.microsoft.com
sudo apt-get update
# Install SQL Server
sudo apt-get install mssql-server
sudo /opt/mssql/bin/mssql-conf setup
```

To enable the sql agent feature run this command:

```bash
sudo /opt/mssql/bin/mssql-conf set sqlagent.enabled true
sudo systemctl restart mssql-server
```

If the command line tools are also required run these commands:

```bash
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
wget -qO- https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
sudo apt-get update
sudo apt-get install mssql-tools unixodbc-dev

echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bash_profile
```

### SQL server extra configuration after the install

Set some initial configuration options from any of the tools in [Client tools](#client-tools). Run the following sql.

```sql
sp_configure 'show advanced options', 1
reconfigure with override

sp_configure 'max server memory (MB)', -- 90% of OS MEM
reconfigure with override
```

The per database settings worth changing are all reachable from T-SQL, which means the same script works from every tool and on Linux, where there is no Configuration Manager to click through.

```sql
-- Full recovery keeps point in time restore available.  If the data is non
-- production or not important, SIMPLE is fine and the log stops growing.
ALTER DATABASE YourDatabase SET RECOVERY FULL;

-- Percentage growth on both files.  The default 1 MB log growth produces
-- thousands of tiny virtual log files on a busy database.
ALTER DATABASE YourDatabase MODIFY FILE (NAME = 'YourDatabase',     FILEGROWTH = 10%);
ALTER DATABASE YourDatabase MODIFY FILE (NAME = 'YourDatabase_log', FILEGROWTH = 10%);

-- Match the engine version so the current query optimiser is used.
ALTER DATABASE YourDatabase SET COMPATIBILITY_LEVEL = 170;  -- SQL Server 2025

-- Query Store, read write, so it is actually collecting.
ALTER DATABASE YourDatabase SET QUERY_STORE = ON;
ALTER DATABASE YourDatabase SET QUERY_STORE (OPERATION_MODE = READ_WRITE);
```

On Windows, force encryption lives in SQL Server Configuration Manager under Protocols. On Linux, set it with `mssql-conf` instead.

```bash
sudo /opt/mssql/bin/mssql-conf set network.forceencryption 1
sudo systemctl restart mssql-server
```

## Reference - Admin

Use [spBlitz (SQL First Responder Kit)](https://www.brentozar.com/blitz/) to detect problems with sql server. Follow the instructions on the spBlitz site.

A few examples:

```sql
-- Realtime performance advice that should be run first when responding to an incident or case
exec sp_BlitzFirst

-- Overall Health Check
exec sp_Blitz

-- Find the Most Resource-Intensive Queries
exec sp_BlitzCache

-- Tune Your Indexes
exec sp_BlitzIndex
```

Use the [Ola Hallengren SQL Server Maintenance Solutions](https://ola.hallengren.com/) for excellent pre-made community backed maintenance jobs.

Both of those install as stored procedures, so they are run the same way from any client, and scheduled with SQL Server Agent jobs.

## Client tools {#client-tools}

Azure Data Studio was the cross platform answer here for years. Microsoft retired it, with support ending in February 2026, and folded its functionality into the VS Code extension below. Anything still recommending it is out of date.

- [**MSSQL extension for VS Code**](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql) - Microsoft's replacement for Azure Data Studio, and where the effort now goes. Free, runs on linux, mac and windows, and includes query editing, a results grid, schema browsing and query plan viewing.
- [**DBeaver**](https://dbeaver.io/) - open source and cross platform. Worth knowing because one tool covers SQL Server, PostgreSQL, SQLite and more, which is most of the [Data](07-postgresql.html) part of this book rather than SQL Server alone.
- [**DataGrip**](https://www.jetbrains.com/datagrip/) - JetBrains, commercial, cross platform, and included with an All Products subscription if you already use Rider.
- [**SSMS**](https://learn.microsoft.com/en-us/ssms/download-sql-server-management-studio-ssms) - still the most complete tool for administration, and still windows only. Reach for it when you need the deeper admin surface; do not build a workflow around it if your team is on linux or mac.
- [**sqlcmd**](https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility) - the command line client, and the one that works over ssh, inside a container, and in a build pipeline.

The lesson worth taking from the retirement is to prefer T-SQL over a GUI workflow when writing anything down. A script in source control outlives whichever client is fashionable.

## SQL Profiler and Extended Events

The old SQL Profiler and its trace API are deprecated, and the graphical XEvent Profiler was an Azure Data Studio feature that went away with it. **Extended Events** is the supported mechanism, and because a session is created and read with T-SQL, it works from any client on any platform.

Capture statements that took longer than a second.

```sql
CREATE EVENT SESSION slow_queries ON SERVER
ADD EVENT sqlserver.sql_batch_completed (
    ACTION (sqlserver.client_app_name, sqlserver.database_name, sqlserver.sql_text)
    -- duration is in microseconds
    WHERE duration > 1000000
),
ADD EVENT sqlserver.rpc_completed (
    ACTION (sqlserver.client_app_name, sqlserver.database_name, sqlserver.sql_text)
    WHERE duration > 1000000
)
ADD TARGET package0.ring_buffer
WITH (MAX_MEMORY = 4096 KB, TRACK_CAUSALITY = ON, STARTUP_STATE = OFF);

ALTER EVENT SESSION slow_queries ON SERVER STATE = START;
```

The ring buffer target holds the results in memory. Read it by shredding the XML.

```sql
SELECT
    x.e.value('(@timestamp)[1]', 'datetime2') AS occurred,
    x.e.value('(data[@name="duration"]/value)[1]', 'bigint') / 1000 AS duration_ms,
    x.e.value('(action[@name="database_name"]/value)[1]', 'nvarchar(128)') AS database_name,
    x.e.value('(action[@name="client_app_name"]/value)[1]', 'nvarchar(256)') AS client_app,
    x.e.value('(action[@name="sql_text"]/value)[1]', 'nvarchar(max)') AS sql_text
FROM (
    SELECT CAST(st.target_data AS xml) AS target_data
    FROM sys.dm_xe_session_targets AS st
    JOIN sys.dm_xe_sessions AS s ON s.address = st.event_session_address
    WHERE s.name = 'slow_queries' AND st.target_name = 'ring_buffer'
) AS raw
CROSS APPLY raw.target_data.nodes('RingBufferTarget/event') AS x(e)
ORDER BY occurred DESC;
```

Stop it when finished. An event session left running on a busy server is overhead nobody remembers enabling.

```sql
ALTER EVENT SESSION slow_queries ON SERVER STATE = STOP;
DROP EVENT SESSION slow_queries ON SERVER;
```

Use a `package0.event_file` target instead of the ring buffer when a session should survive a restart or capture more than a few thousand events. For day to day "why was this slow", the [Query Store](#sql-query-store) below is usually the better first stop, because it is always on and keeps its history.

## SQL Query Store

[Monitor performance by using the Query Store](https://learn.microsoft.com/en-us/sql/relational-databases/performance/monitoring-performance-by-using-the-query-store?view=sql-server-ver16)

## SQL Watch

[SQL Watch](https://sqlwatch.io) - sql monitor.
