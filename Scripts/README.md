# Scripts

## Initializing the Database

When CAT is being installed on a new server, you must perform two main tasks.
1.  Install the CAT application<br>This will require configuring you web server (IIS, Azure, Apache, ...)
2.  Create the required MySQL databases.<br>This process is covered in this section.

### CAT Databases
CAT uses two MySQL databases, "cms" and "cmsusers".  The cmsusers database holds user account information and is managed by the Microsoft .NET Core Identity framework.

### Creating the CMS Database

In the Scripts directory, run the managedb.py Python script with the "-clean" option.  This will delete and existing cms database and create a new instance initialized with 
required tables.

```
cd Scripts
python managedb.py -clean -institution "<name>"
```

The value that you specify with the "-institution" flag will be the top level of you location hierarchyYou will be prompted for the user name and password for MySQL.  The script will generate
output similar to the following.

```
python managedb.py -clean -institution "University of New Mexico"
Enter your MySQL user name: root
Enter your MySQL password:


########################################################################
#
# Creating clean database
#
# Database host is localhost
#
########################################################################


Deleting cms database
Deleting cmsusers database
mysql: [Warning] Using a password on the command line interface can be insecure.

(Warning messages from .NET Core about .NET version)

Opening database on localhost as root ...
Database tables created: 0.7942075 seconds
Initializing the InventoryStatusNames table
Initializing the LocationLevelNames table
Creating the GetLocationName function
Database seeded: 0.4510716 seconds
Initialize Settings table


########################################################################
#
# Populating tables
#
########################################################################


Populating ChemicalsOfConcern table
    Inserted 602 records from .\Data\chemicals-of-concern.json into ChemicalsOfConcern table
Populating pictograms in CASDataItems table
Inserting data into HazardCodes table
Commiting 12721 records to HazardCodes
Inserting 3901 records into CASDataItems
There are 3901 rows in CASDataItems with pictograms
New records: 3901   Updated records: 26
Executing "insert into LocationTypes (Name, ValidChildren) Values (%s, '')" with parameters University of New Mexico returned 1
Executing "insert into StorageLocations (Name, ParentID, LocationLevel, LocationTypeID, IsLeaf) values (%s, 0, 0, 1, 0)" with parameters University of New Mexico returned 1
Executing "update StorageLocations set Path = GetLocationPath(LocationID)" with parameters None returned 1


########################################################################
#
# Creating default report definitions
#
########################################################################


Loading database queries
    Adding/Updating Inventory
    Adding/Updating Audits
    Adding/Updating LogEntries
    Adding/Updating TotalQuantities
Loading reports
    Adding/Updating Log Entries
    Adding/Updating Inventory Audit
    Adding/Updating Inventory


Database Queries
    1: Inventory
    2: Audits
    3: LogEntries
    4: TotalQuantities
Report Definitions
    1: Log Entries
    2: Inventory Audit
    3: Inventory


########################################################################
#
# The cms database has been created on localhost
# The the Institution setting is "University of New Mexico"
# An initial entry has been created in the LocationTypes table with
#     name "University of New Mexico"
# An initial entry has been created in the StorageLocations table with
#     name "University of New Mexico"
#
########################################################################


mysql: [Warning] Using a password on the command line interface can be insecure.
+------------------+-----------+
| User             | Host      |
+------------------+-----------+
| cms              | %         |
| cms              | localhost |
| fbpool           | localhost |
| mysql.infoschema | localhost |
| mysql.session    | localhost |
| mysql.sys        | localhost |
| picomm           | localhost |
| root             | localhost |
+------------------+-----------+
+----------------+--------------------------+---------------+
| LocationTypeID | Name                     | ValidChildren |
+----------------+--------------------------+---------------+
|              1 | University of New Mexico |               |
+----------------+--------------------------+---------------+
+------------+--------------------------+----------+----------------+--------+---------------+--------------------------+
| LocationID | Name                     | ParentID | LocationTypeID | IsLeaf | LocationLevel | Path                     |
+------------+--------------------------+----------+----------------+--------+---------------+--------------------------+
|          1 | University of New Mexico |        0 |              1 |        |             0 | University of New Mexico |
+------------+--------------------------+----------+----------------+--------+---------------+--------------------------+
+-----------+------------------------------+--------------------------+
| SettingID | SettingKey                   | SettingValue             |
+-----------+------------------------------+--------------------------+
|         1 | System.MaxInventoryRows      | 100                      |
|         2 | System.Announcement          |                          |
|         3 | System.TempDir               | /tmp                     |
|         4 | System.SearchLevel           | 1                        |
|         5 | System.CreateImportLocations | no                       |
|         6 | Institution                  | University of New Mexico |
+-----------+------------------------------+--------------------------+
Elapsed seconds: 21.218811511993408
```

