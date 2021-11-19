# CMS Web Application

## Prerequisites

---

### 1. MySQL (or MariaDB)

You will need to create a user 'cms' with password 'cms'.

```
create user 'cms'@'localhost' identified by 'cms';

grant all privileges on *.* to 'cms'@'localhost';

```

### 2. .NET Core

This can be downloaded from Microsoft

### 3. Python 3

Maintenance scripts are written in Python.

### 4. NodeJS

npm is used to install some prerequisite packages.

## Setup

---

CAT uses two MySQL databases: cms and cmsusers. The cms database holds inventory data. The cmsusers database contains user login information. The cms database can be created using a Python script or with an included utility application. Follow the steps below, in the order they are listed, to create
these databases and log in to the system for the first time.

### Step 1: Build the cms database

There are two options for creating the cms database: running a Python script or running an included .NET application. Both are described below.

#### Creating the cms database with a Python script

This method of creating a new database is suitable if there have been no changes to the database schema, because it uses SQL scripts to create the database. If the database has changed, use the second option - using the dbutil program.

```
C> cd Scripts
C> python managedb.py -clean
```

> You may have to install some additional Python packages before that will work:

```
pip install pymysql
pip install attrdict
pip install cryptography
```

#### Creating the cms database with the dbutil utility

This method of creating a new database relies on Entity Framework to create the database. This ensures that the database schema exactly corresponds to the database entities that are defined in the CAT code.

```
C> cd dbutil
C> dotnet run -create
```

### Step 2(optional): Create test data

```
C> cd ..\Scripts
C> python generate-testdata.py -for Iraq
```

### Step 3: Build and run the website

The project uses npm to install some dependencies and tools, including webpack. Before compiling you must run webpack to compile VueJS components that are used by the application.

```
C> cd ..\CMS
C> npm install
C> npm run bundle
C> dotnet run
```
If you are using Visual Studio, instead of the last step, open the solution file (ChemicalAssetTracker.sln) and run *Build All* from the *Build* menu.

### Step 4: Initialize predefined user accounts

Open a browser and navigate to<br>
http://localhost:5002/Admin/SeedUsers

Navigate to http://localhost:5002/ to start using the app.

## Deploying to Microsoft Azure

---

### Prerequisites

In order to deploy CAT to Azure, you will need to have an Azure account with an App Service and Azure Database for MySQL instance.

1.  Publish CAT to Azure<br>
    This step must be performed in Visual Studio 2017 or later. You will need your Azure account username and password in order to download the Azure publish profile for your Web Service.

    -   Right click the CMS project and select Publish from the context menu.
    -   Click the New Profile link
    -   Follow the steps to connect to Azure and download the publish profile for the CAT App Service.

2.  Rename the publish profile as needed and click the Publish button to begin the publish process.<br>

    -   Review the messages that are generated and confirm that the publish operation succeeded.
    -   If the publish operation succeeded, Visual Studio will open a browser window with your app's URL. This will fail with an internal server error because the app cannot connect to the database. Step 3 will fix this.

3.  Update the connection strings associated with your App Service to refer to your MySQL database.<br>
    For this step, you will need the connection string for your MySQL database. These can be obtained by opening the MySQL database resource and selecting _Connection Strings_ from the database action list on the left. <br>

    -   Open the MySQL database resource and click on _Connection Strings_ in the list of properties on the left.
    -   Open the App Service and select _Connection Strings_ in the App Service options on the left. Save the Web App connection string for use in the next step. It will look like the example below, but with your server name in place of servername. Note that there are placeholders for the database name and password. You will probably have a username other than "catadmin".

    > Database={your_database}; Data Source=servername.mysql.database.azure.com; User Id=catadmin@servername; Password={your_password}

    -   Return to your list of Azure resources and open the App Service. Select _Configuration_ from the property list on the left. In the list of configuration settings on the right, you will find a section labeled _Connection Strings_ - click the plush sign to create a new connection string. Enter "CMSConnection" for the name and use the connection string you saved from the previous step as the value. Replace "{your*database}" with "cms" and replace "{your_password}" with the password you specified for your MySQL administrator account. The full connection string should look like the example below - I have added "pooling=true" to enable connection pooling. Click the \_OK* button to save connection string. Note that your App Service has not yet been updated - you will need to explicitly save your changes after creating a second connection string.

    > Database=cms; Data Source=servername.mysql.database.azure.com; User Id=catadmin@servername; Password=whatever;

    -   Repeat the process above to create a connection string names _DefaultConnection_. The database name for this connection string will be "CMSUsers". The other values will be the same.
    -   Click the "Save" button at the top of the Configuration page to update the App Service and restart it with the new settings. Browse to the app to confirm that it now runs correctly and presents you with the login page.

## Projects

---

-   Common<br>
    A library of C# utility functions
-   DataModel<br>
    Data model classes and Entity Framework Core DbContext (CMSDB)
-   dbutil<br>
    Console application for database management tasks
-   CMS<br>
    Main .Net Core web application
-   BarcodeReader<br>
    Sample code for reading barcodes on mobile devices.

## Other Folders

-   DatabaseSnapshots - random database backups<br>
    Use backupdb.bat and restoredb.bat to create/restore database backups
    -   cms_schema.sql and cmsusers_schema.sql will have the latest clean
        database initialization scripts
-   Migrations - database migration scripts

## Schedule

### Project Schedule as of 06/13/2018

[Schedule](./schedule.png)

# Notes

## Nov 6, 2018

Tried unsuccessfully to get EF migrations to work. Upgraded Pomello to ver 2.1.2 and migrations improved, but idempotent migrations are not supported.

-   Ran "dotnet ef migrations script -o cms_initial_migration.sql"<br>
    This only works if you are in the model directory.<br>
    This works: "dotnet ef migrations script -o DatabaseMigrations\cms_initial_migration.sql --project DataModel --startup-project DataModel"
-   Created initial migrations in the Migrations folder for cms and cmsuser

# Issues

1.  Latest features<br>
    a) search options in Inventory and Search<br>
    b) additional User information
    c) LCSS data
    d) PDF output
2.  Location-dependent stock check<br>
    In very large installations a per-installation stock check is not practical. We need to have a stock check model that allows multiple stock checks to be active at the same time, covering different locations. How should we handle overlapping stock checks?
3.  Displaying hazard information.
    1.  Can we simplify the inventory page to just show the relevent pictograms on each line in a single column, rather than having a column for each possible hazard?
    2.  How should the hazards be shown in the inventory detail view?
    3.  How should additional hazard information be accessed?<br>
        a) a single pop-up that lets you select SDS doc, hazard details, and disposal information?
        b) put multiple links in the Inventory page where SDS currently is?
