from utils import *
from cmsdb import *
from dotmap import DotMap
import getpass
from queries import predefined_queries, predefined_reports


def load_reports():

    hostname = get_arg('-h', None)
    if hostname == None:
        print("You must specify the hostname with -h")
        exit(0)

    banner("Creating default report definitions")

    user, pswd = get_mysql_info()

    db = MySQL(hostname, user, pswd, 'cms')

    print("Loading database queries")
    for name in predefined_queries:
        sql = predefined_queries[name]
        print("    Adding/Updating " + name)
        db.execute_nonquery(f"delete from DatabaseQueries where Name = '{name}'")
        db.execute_nonquery("insert into DatabaseQueries (Name, QueryText) values (%s, %s)", [name, sql])

    print("Loading reports")
    for report in predefined_reports:
        name = report["ReportName"]
        description = report["Description"]
        queryname = report["Query"]
        roles = report["Roles"]
        widgets = report["Widgets"]
        whereclause = report["Where"]

        row = db.queryone(f"select DatabaseQueryID from DatabaseQueries where Name = '{queryname}'")
        if row:
            print("    Adding/Updating " + name)
            db.execute_nonquery(f"delete from ReportDefinitions where ReportName = '{name}'")
            queryid = row['DatabaseQueryID']
            sql = "insert into ReportDefinitions (ReportName, WhereClause, Description, DatabaseQueryID, Roles, Widgets) values (%s, %s, %s, %s, %s, %s)"
            params = [name, whereclause, description, queryid, roles, widgets]
            db.execute_nonquery(sql, params)
        else:
            print("    Error - there is no DatabaseQuery named " + queryname)

    print(" ")
    print(" ")
    rows = db.queryall("select * from DatabaseQueries")
    if (len(rows) > 0):
        print("Database Queries")
        for row in rows:
            print(f"    {row['DatabaseQueryID']}: {row['Name']}")
    else:
        print("Error: no queries found")
    rows = db.queryall("select * from ReportDefinitions")
    if (len(rows) > 0):
        print("Report Definitions")
        for row in rows:
            print(f"    {row['ReportID']}: {row['ReportName']}")
    else:
        print("Error: no reports found")

if __name__ == "__main__":
    load_reports()
