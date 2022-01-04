import sys
import os
import re
import time
import datetime
from Scripts.utils import *

BACKUPDIR = ".\\DatabaseSnapshots"

def have_arg(name):
    return name in sys.argv


def get_arg(name, default_value=None):
    if have_arg(name):
        ix = sys.argv.index(name)
        if ix < (len(sys.argv) - 1):
            return sys.argv[ix+1]
    return default_value


def ctime(file):
    return os.path.getctime(os.path.join(BACKUPDIR, file))

def select_file():
    global BACKUPDIR
    backups = [f for f in os.listdir(BACKUPDIR) if f.startswith("cms_") and f.endswith(".sql")]
    backups.sort(key=ctime)
    for i in range(len(backups)):
        filename = backups[i]
        file_date = time.ctime(ctime(filename))
        print(f'{i+1}: {file_date}   {filename} ')
    response = 0
    while response < 1 or response > len(backups):
        ans = input("Select: ") 
        if ans == 'quit':
            exit(0)
        else:
            if re.match("\\d+", ans):
                response = int(ans)
    return backups[response-1]
    
def show_usage():
    banner('Use: python3 restoredb.py [-help] [-h <host>] [-db <dbname>] [-u <user>] [-p <pswd>] [-from <path>]'
           , ' '
           , '   host defaults to localhost'
           , '   user defaults to root'
           , '   pswd defaults to root')



if have_arg("-help"):
    show_usage()
    exit(0)
    
host = get_arg("-h", "localhost")
dbname = get_arg('-db')
backuppath = get_arg("-from")
(user, pswd) = get_mysql_info()

if host and dbname and user and pswd and backuppath:
    cmd = f"mysql -h {host} -u {user} -p{pswd} {dbname} < {backuppath}"
    print(cmd)
    os.system(cmd)
else:
    show_usage()