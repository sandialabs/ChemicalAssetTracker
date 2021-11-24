import sys
import os
import re
import datetime
import getpass
from utils import *

now = datetime.datetime.now()
timestamp = now.strftime("%Y-%m-%d_%H%M%d")


def make_filename(dbname):
    if have_arg("-nodata"):
        return f"{dbname}-nodata_{timestamp}.sql"
    else:
        return f"{dbname}-full_{timestamp}.sql"


def fix_autoincrement(filename):
    with open(filename, 'r') as fp:
        data = fp.read()
        data = re.sub(r"AUTO_INCREMENT=[0-9]*\s", " ", data)
    with open(filename, "w") as fp:
        fp.write(data)


def show_usage():
    print("Use: python snapshot.py [-u <username>] [-p <password>] [-nodata] -db <dbname> [-o <filename>]")


def do_snapshot(user, pswd, dbname, outputfile=None):
    if outputfile == None:
        outputfile = '../DatabaseBackups/' + make_filename(dbname)
    if have_arg("-nodata"):
        cmd = f'mysqldump -u {user} -p{pswd} --databases --no-data {dbname} > {outputfile}'
    else:
        cmd = f'mysqldump -u {user} -p{pswd} --databases {dbname} > {outputfile}'

    print(cmd)
    os.system(cmd)
    if have_arg("-nodata"):
        fix_autoincrement(outputfile)


if __name__ == "__main__":
    print("snapshot.py is running as main")
    dbname = get_arg("-db")
    if have_arg("-help") or dbname == None:
        show_usage()
        exit(0)

    (user, pswd) = get_mysql_info()

    if user and pswd:
        outputfile = get_arg('-o')
        print("Calling do_snapshot")
        do_snapshot(user, pswd, dbname, outputfile)
