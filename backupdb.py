import sys
import os
import datetime
from Scripts.utils import *

def backup_db(host, user, pswd, dbname):
    now = datetime.datetime.now()
    ts = now.strftime('%Y-%m-%d_%H%M')
    outdir = f".\\DatabaseSnapshots\\{host}"
    if not os.path.isdir(outdir):
        os.makedirs(outdir)
    if not os.path.isdir(outdir):
        print(f'Output directory "{outdir}" does not exist and cannot be created')
        exit(1)
    cmd = f"mysqldump --column-statistics=0 -h {host} -u {user} -p{pswd} {dbname} > {outdir}\\{dbname}_backup_{ts}.sql"
    print(cmd)
    os.system(cmd)
    print('')
    print(f"Contents of {outdir}")
    os.system(f'dir /B {outdir}')

def show_usage():
    banner('Use: python3 backupdb.py [-help] [-h <host>] [-u <user>] [-p <pswd>]'
           , ' '
           , '   host defaults to localhost'
           , '   user defaults to root'
           , '   pswd defaults to root'
           , ' '
           , '   Dump files are stored in .\\DatabaseSnapshots\\<host>')

if have_arg("-help"):
    show_usage()
    exit(0)
    
host = get_arg("-h", "localhost")
(user, pswd) = get_mysql_info()

if host and user and pswd:
    backup_db(host, user, pswd, 'cms')
    backup_db(host, user, pswd, 'cmsusers')
