import sys
import os
import re
import datetime
import getpass
from utils import *


user = get_arg('-u', None)
pswd = get_arg('-p', None)
host = get_arg('-h', 'localhost')
dbname = get_arg('-db', 'cms')

if user and pswd and host:
    parts = host.split('.')
    short_host = parts[0]
    outputfile = f'{short_host}_{dbname}_{timestamp()}.bak'
    cmd = f'mysqldump -h {host} -u {user} -p{pswd} --column-statistics=0 {dbname} > {outputfile}'
    run(cmd, True)
else:
    banner('Use: python3 dump.py -u <username> -p <password> [h <hostname>] [-db <dbname>',
           '',
           '<hostname> defaults to localhost',
           'dbname defaults to cms'
           )
