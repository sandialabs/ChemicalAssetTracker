import sys
import os
import datetime

def backup_db(dbname):
    now = datetime.datetime.now()
    ts = now.strftime('%Y-%m-%d_%H%M')
    cmd = "mysqldump -u root -proot {0} > .\\DatabaseSnapshots\\{0}_backup_{1}.sql".format(dbname, ts)
    print(cmd)
    os.system(cmd)

backup_db('cms')
backup_db('cmsusers')
