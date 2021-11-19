import sys
import os
from utils import *
from cmsdb import *


def show_table(db, tablename):
    row = db.queryone(f"select count(*) as RowCount from {tablename}")
    print(f"{tablename:<30} {row['RowCount']:9d}")


user, pswd = get_mysql_info()
if user and pswd:
    db = MySQL('localhost', user, pswd, 'cms')
    tables = ['casdataitems', 'casdisposalprocedures', 'chemicalsofconcern', 'databasequeries',
              'disposalprocedures', 'ghsclassifications', 'hazardcodes', 'inventoryaudits', 'inventoryitems',
              'inventorystatusnames', 'locationtypes', 'logentries', 'owners', 'removeditems',
              'reportdefinitions', 'settings', 'storagegroups', 'storagelocations']
    print("Table                          Row Count")
    print("----------------------------   ---------")
    for table in tables:
        show_table(db, table)
