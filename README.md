# CMS Web Application

## Prerequisites

---

### 1. MySQL

The community edition of MySQL is recommended. Do not use MariaDB - there are feature differences between it
and MySQL that will break CAT.

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

The recommended option for creating the database is with the managedb.py script in the Scripts directory. It takes two arguments: "-clean" 
to create a clean database, and "-institution NAME", where NAME will be the top level of your location hierarchy.

```
C> cd Scripts
C> python managedb.py -clean -institution ROOT
```

> You may have to install some additional Python packages before that will work:

```
pip install pymysql
pip install attrdict
pip install cryptography
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

