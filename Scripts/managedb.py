import os
import sys
import re
import time
from utils import *
from snapshot import *
from cmsdb import *

python = sys.executable

ChemicalsOfConcern = r'..\Data\Compiled Chemicals of Concern for Release SAND2019-4657 O.xlsx'


def run(cmd):
    nice = re.sub("-p[\\S]+", "-p*******", cmd)
    print(f'Run "{nice}"')
    os.system(cmd)


def run_python(cmd):
    os.system(python + " " + cmd)


def confirm(prompt):
    ans = input(prompt + "(y/n) ")
    return ans.lower().startswith('y')


def show_usage():
    banner("Use: python3 managedb.py [-help] [-clean]"
           , ""
           , "     -help              show this message and exit"
           , "     -clean -i <NAME>   create a clean database and exit"
           , "                        <NAME> is the institution name"
           , "                        include -users to inititize cmsusers too"
           , "                        include -testdata to generate test data"
           , " "
           , "If no command line arguments are passed, interactive mode is enabled"
    )

def show_cmds():
    print(" ")
    print("Commands:")
    print(" ")
    print("help      show this text")
    print("snap      create backups of cms and cmsusers")
    print("clean     create a clean database")
    print("users     show users")
    print("stats     show statistics")
    print("query     run database queries")
    print(" ")


def parse_command(cmd):
    result = []
    parts = cmd.split(' ')
    for part in parts:
        part = part.strip()
        if len(part) > 0:
            result.append(part.lower())
    return result


def do_drop():
    hostname = get_arg('-h')
    if hostname == None:
        print("You must provide the MySQL hostname with -h <hostname>")
        return

    banner("This script will delete the cms and cmsusers databases",
           f"from the the MySQL database on {hostname}",
           "",
           "This cannot be undone except by restoring from a backup.")

    if not confirm(f"Are you sure you want to delete the CAT database (cms and cmsusers) on {hostname}? "):
        return

    #print("Hostname is " + hostname)
    user, pswd = get_mysql_info()

    try:
        db = MySQL(hostname, user, pswd, 'mysql')
        print("connected")
        for database in ['cms', 'cmsusers']:
            rc = db.execute_nonquery(f"drop database if exists {database}")
            print(f"DROP request for {database} returned {rc}")
        db.execute_nonquery(f"show databases")

    except Exception as ex:
        print("")
        print("Unable to connect to MySQL on " + hostname)
        print(ex)


def do_clean(ask=True):
    if ask and not confirm("Create a clean cms database?"):
        return

    hostname = get_arg('-h', 'localhost')
    #print("Hostname is " + hostname)
    user, pswd = get_mysql_info()

    t1 = time.time()

    if have_arg('-users'):
        # This will cause create-cms-db.py to create a new cmsuser database
        # It will also initialize the database from cmsusers-full.  This is
        # necessary because EF for MySQL creates key values that are too long.
        run(f'mysql -u {user} -p{pswd} -e "drop database cmsusers;"')
        print(" ")

    testdata = have_arg('-testdata')
    institution = get_arg('-institution')
    cmd = f' create-cms-db.py -institution "{institution}" -h {hostname} -u {user} -p {pswd}  -force '
    if testdata:
        cmd += " -testdata"
    run_python(cmd)
    t2 = time.time()
    elapsed = t2 - t1
    if elapsed < 100:
        print(f"Elapsed seconds: {elapsed}")
    else:
        print(f"Elapsed minutes: {elapsed / 60.0}")
    exit(0)
    #run_python(f'database-stats.py -u {user} -p {pswd}')


def interactive():
    user = None
    pswd = None
    is_active = True
    while is_active:
        cmdline = input("MANAGE> ")
        parts = parse_command(cmdline)
        if len(parts) == 0:
            continue
        verb = parts[0]
        args = parts[1:]
        if verb == 'help':
            show_cmds()
        elif verb == 'clean':
            do_clean()
        elif verb == 'snap':
            if user == None or pswd == None:
                (user, pswd) = get_mysql_info()
            if user and pswd:
                do_snapshot(user, pswd, 'cms')
                do_snapshot(user, pswd, 'cmsusers')
        elif verb == 'users':
            if user == None or pswd == None:
                (user, pswd) = get_mysql_info()
            if user and pswd:
                do_snapshot(user, pswd)
            run(f"mysql -u {user} -p{pswd} cmsusers -e \"select UserName from aspnetusers\"")
        elif verb == 'query':
            run_python("query-test.py")
        elif verb == 'quit':
            is_active = False
        else:
            print("I didn't understand that command")


if have_arg('-help'):
    show_usage()
    exit(0)

if have_arg('-clean'):
    if get_arg('-institution') == None:
        show_usage()
        print("You must specify the institution name with -institution <NAME>")
        exit(0)
    do_clean(False)
    exit(0)

if have_arg('-drop'):
    do_drop()
    exit(0)

interactive()
