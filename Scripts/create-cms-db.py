import sys
import os
import json
from utils import *
import pymysql
from attrdict import AttrDict
import getpass
from load_reports import *
from import_chemicals_of_concern import *


ChemicalsOfConcern = r'.\Data\chemicals-of-concern.json'


class MySQL:

    def __init__(self, hostname, user, password, dbname):
        self.m_dbname = dbname
        self.m_db = pymysql.connect(host=hostname,
                                    user=user,
                                    password=password,
                                    db=dbname,
                                    cursorclass=pymysql.cursors.DictCursor)

    def disconnect(self):
        if self.m_db:
            self.m_db.close()
            self.m_db = None

    def execute_nonquery(self, sql, parameters=None, commit=True, verbose=False):
        if verbose:
            print('    ' + sql)
        with self.m_db.cursor() as cursor:
            rc = cursor.execute(sql, parameters)
            if commit:
                self.m_db.commit()
        return rc

    def queryone(self, sql, parameters=None):
        with self.m_db.cursor() as cursor:
            cursor.execute(sql, parameters)
            result = cursor.fetchone()
        return result

    def queryall(self, sql, parameters=None):
        result = []
        with self.m_db.cursor() as cursor:
            cursor.execute(sql, parameters)
            result = cursor.fetchall()
        return result

    def row_exists(self, sql, parameters=None):
        return (self.queryone(sql, parameters) != None)

    def database_exists(self, dbname):
        return (self.queryone(f"select SCHEMA_NAME from INFORMATION_SCHEMA.SCHEMATA where SCHEMA_NAME = '{dbname}'") != None)

    def table_exists(self, table_name):
        return self.row_exists(f"select TABLE_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='{self.m_dbname}' and TABLE_NAME='{table_name}'")

    def column_exists(self, table_name, column_name):
        return self.row_exists(f"select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='{self.m_dbname}' and TABLE_NAME='{table_name}' and COLUMN_NAME = '{column_name}'")


def insert_setting(db, key, value):
    if not db.row_exists("select * from Settings where SettingKey = %s", parameters=[key]):
        db.execute_nonquery("insert into Settings (SettingKey, SettingValue) values (%s, %s)", parameters=[key, value])


def initialize_settings(db, hostname):
    insert_setting(db, 'System.MaxInventoryRows', '100')
    insert_setting(db, 'System.Announcement', '')
    if 'azure' in hostname:
        insert_setting(db, 'System.TempDir', 'C:\\local\\Temp')
    else:
        insert_setting(db, 'System.TempDir', '/tmp')
    insert_setting(db, 'System.SearchLevel', '1')
    insert_setting(db, 'System.CreateImportLocations', 'no')


def execute_nonquery(db, sql, params=None):
    rc = db.execute_nonquery(sql, params)
    print(f'Executing "{sql}" with parameters {params} returned {rc}')


def show_usage():
    print("Use: create-cms-db -u <username> [-p <password>] [-force]")
    print("     -h <hostname>       MySQL database host name")
    print("     -u <username>")
    print("     -p <password>")
    print("     -v                  verbose mode")
    print("     -institution <name> institution name - default None")
    print("     -testdata           create random test data")
    print("     -locals <n>         number of locales per site - default 2")
    print("     -buildings <n>      number of buildings per locale - default 4")
    print("     -rooms <n>          number of rooms per building - default 6")
    print("     -stores <n>         number of stores per room - default 3")
    print("     -shelves <n>        number of shelves per store - default 5")
    print("     -items <n>          number of inventory items - default 10000")


hostname = get_arg('-h', "localhost")
#print("Hostname is " + hostname)
user, pswd = get_mysql_info()
if user == None or pswd == None:
    exit(0)

institution = get_arg('-institution')
create_testdata = have_arg('-testdata')
localecount = int(get_arg('locales', 2))
bldgcount = int(get_arg('-buildings', 4))
roomcount = int(get_arg('-rooms', 6))
storecount = int(get_arg('-stores', 3))
shelfcount = int(get_arg('-shelves', 5))
itemcount = int(get_arg('-items', 10000))
levelnames = get_arg(
    '-levelnames', 'University,Department,Building,Room,Storage,Shelf').split(',')
itemcount = get_int_arg('-items', 10000)

if institution == None:
    print('You must specify an institution name with the "-institution" flag')
    show_usage()
    exit(0)


verbose = False

banner("Creating clean database", "", f"Database host is {hostname}")
try:
    db = MySQL(hostname, user, pswd, 'mysql')
    db_exists = db.database_exists('cms')
    userdb_exists = db.database_exists('CMSUsers')

    if db_exists:
        print("Deleting cms database")
        db.execute_nonquery('drop database cms')
    if userdb_exists:
        print('Deleting cmsusers database')
        db.execute_nonquery('drop database cmsusers')
except Exception as ex:
    print(f"Unable to connect to {hostname}")
    print(ex)
    exit(1)


# The app will create cmsdb with EnsureCreated
# if not userdb_exists:
#     print("*** Creating CMSUsers database")
#     run(f'mysql -u {user} -p{pswd} -h {hostname} -e "create database CMSUsers CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci"', verbose)
#     run(f'mysql -u {user} -p{pswd} -h {hostname} CMSUsers < cmsusers-full.sql', verbose)
#     userdb_exists = True

# if db_exists:
#     if have_arg('-force'):
#         run(f'mysql -u {user} -p{pswd} -h {hostname} -e "drop database cms"', verbose)
#     else:
#         print("Database cms already exists.  Use -force to force creation")
#         exit(1)

# create empty database
db.execute_nonquery("create database cms CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci")

# run initialization script
os.chdir("../DataModel")
run(f"mysql -u {user} -p{pswd} -h {hostname} cms < initializecmsdb.sql", verbose)

# create the cms user account
#run(f"mysql -u {user} -p{pswd} -h {hostname} cms -e \"create user 'cms'@'%' identified by 'cms'\"")
#run(f"mysql -u {user} -p{pswd} -h {hostname} cms -e \"grant all privileges on cms.* to 'cms'@'%'\"")

# run dbutil to complete initialization
os.chdir("../dbutil")
run(f'dotnet run -create -noprompt -hostname {hostname} -cmsuser {user} -cmspswd {pswd}', verbose=verbose, password=pswd)

print("Initialize Settings table")
db = MySQL(hostname, user, pswd, 'cms')
initialize_settings(db, hostname)
db.disconnect()

os.chdir("../Scripts")
banner("Populating tables")
print("Populating ChemicalsOfConcern table")
import_json(ChemicalsOfConcern)
#run(f'python3 import-chemicals-of-concern.py -i "{ChemicalsOfConcern}" -u {user} -p {pswd} -h {hostname}', verbose=verbose)
print("Populating pictograms in CASDataItems table")
run(f'python3 import_ghs.py -u {user} -p {pswd} -h {hostname}', verbose=verbose)

# The following code imported disposal information into the database
# This data is no longer used - the original SNL paper for disposal 
# is available from the UI
# print("Populating disposal tables")
# run(f'python3 import_disposal.py -u {user} -p {pswd} -h {hostname}', verbose=verbose)


#run(f'mysql -u {user} -p{pswd} cms -e "source cms-nodata.sql"', verbose)


root_location = institution

if not create_testdata:
    db = MySQL(hostname, user, pswd, 'cms')
    db.execute_nonquery(
        "insert into Settings (SettingKey, SettingValue) values (%s, %s)", ('Institution', institution))
    execute_nonquery(db, "insert into LocationTypes (Name, ValidChildren) Values (%s, '')", root_location)
    root_type_id = db.queryone(f"select LocationTypeID from LocationTypes where Name = %s", root_location)["LocationTypeID"]
    execute_nonquery(db, f"insert into StorageLocations (Name, ParentID, LocationLevel, LocationTypeID, IsLeaf) values (%s, 0, 0, {root_type_id}, 0)", institution)
    execute_nonquery(db, "update StorageLocations set Path = GetLocationPath(LocationID)")
    load_reports()
    banner(
        f'The cms database has been created on {hostname}',
        f'The the Institution setting is "{institution}"',
        f'An initial entry has been created in the LocationTypes table with',
        f'    name "{root_location}"',
        f'An initial entry has been created in the StorageLocations table with ',
        f'    name "{institution}"'
    )
    run(f'mysql -h {hostname} -u {user} -p{pswd} cms -e "select User, Host from mysql.user;  select * from LocationTypes; select * from StorageLocations; select * from Settings;"', verbose=verbose)
    db.disconnect()
    exit(0)

banner("Generating test data")
run(f'python3 generate-testdata.py -for "{institution}" -locales {localecount} -buildings {bldgcount} -rooms {roomcount} -stores {storecount} -shelves {shelfcount}  -items {itemcount}', verbose)
load_reports()
