# DataModel Project

## Creating a clean database

Use the Scripts/managedb.py script to create a clean database.  <SITENAME> specifies the name of the top level of your location hierarchy, e.g. "University of XYZ" or "Ministry of Education".

```
cd Scripts
python3 managedb.py -clean -institution <SITENAME>
```

## Migrations

Note: if dbutil is used to create a fresh database, the __EFMigrationsHistory
table will not be created. To create a fresh database it is recommended that you first create
the database with the current.sql script in Migrations/MigrationScripts and then run dbutil.

### Creating a Migration (Visual Studio Package Manager)

The following package manager commands create a new migration scripts to set up an empty database and to update an existing database.  The example below also saves the from-scratch to initializecmsdb.sql.  This file should always contain the latest migration code.  It is used by scripts in the Scripts directory.

```
PM> Add-Migration -Name MIGRATION_NAME -Context CMSDB
PM> Script-Migration -Output DataModel\MigrationScripts\MIGRATION_NAME.sql -Context CMSDB
PM> Script-Migration -Output DataModel\MigrationScripts\initializecmsdb.sql -Context CMSDB
PM> Script-Migration -Output MIGRATION_NAME_Incremental.sql -Context CMSDB -From PREVIOUS_MIGRATION
```


## Migration History

|Name|Date|Comments|
|----|----|--------|
|01_InitialMigration|11/19/2021|Initial migration|
