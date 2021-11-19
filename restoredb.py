import sys
import os
import re
import time
import datetime

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

user = get_arg('-u')
pswd = get_arg('-p')

if user and pswd:
    selected = os.path.join(BACKUPDIR, select_file())
    cmd = f"mysql -u {user} -p{pswd} cms < {selected}"
    os.system(cmd)
else:
    print("Use: python3 restoredb.py -u <username> -p <password>")