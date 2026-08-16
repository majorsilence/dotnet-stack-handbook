---
layout: chapter
title: "Microsoft SQL Server"
number: 8
part: 3
---

All sql scripts included in this section expect to be run in sql server management studio, azure data studio, or your preferred sql tool. If you need to install sql server skip to [SQL - Install](#sql-install).

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

Set some initial configuration options in [sql management studio](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16) or [azure data studio](https://learn.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver16&tabs=redhat-install%2Credhat-uninstall). Run the following sql.

```sql
sp_configure 'show advanced options', 1
reconfigure with override

sp_configure 'max server memory (MB)', -- 90% of OS MEM
reconfigure with override
```

- SQL Management Studio

  - sql options (database properties)
    - recovery model: full
      - If the data is non production or not important feel free to use the simple recovery mode.
    - Log and Data Growth: 10%
    - Compatibility: latest version
    - Query Store - enable “Read write”

- SQL Server Configuration Manager -> Protocols
  - Set "Force Encryption" to "yes"

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

![azure data studio adminpack](images/sql-server/azure-data-studio-adminpack.png)

![azure data studio sql agent jobs](images/sql-server/azure-data-studio-sql-agent-jobs.png)

## SQL Profiler

![azure data studio launch profiler](images/sql-server/azure-data-studio-launch-profiler.png)

![azure data studio profiler 1](images/sql-server/azure-data-studio-profiler1.png)

![azure data studio profiler 2](images/sql-server/azure-data-studio-profiler2.png)

## SQL Query Store

[Monitor performance by using the Query Store](https://learn.microsoft.com/en-us/sql/relational-databases/performance/monitoring-performance-by-using-the-query-store?view=sql-server-ver16)

## SQL Watch

[SQL Watch](https://sqlwatch.io) - sql monitor.
