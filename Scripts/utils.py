import sys
import os
import time
import getpass
import re
import datetime


class Timer:
    def __init__(self):
        self.m_start_time = time.time()

    def elapsed(self):
        return time.time() - self.m_start_time


def have_arg(name):
    return name in sys.argv


def get_arg(name, default_value=None):
    if have_arg(name):
        ix = sys.argv.index(name)
        if ix < (len(sys.argv) - 1):
            return sys.argv[ix+1]
    return default_value


def get_int_arg(name, default_value=None):
    strval = get_arg(name)
    if strval:
        try:
            val = int(strval)
        except:
            val = default_value
        return val
    return default_value


def get_float_arg(name, default_value=None):
    strval = get_arg(name)
    if strval:
        try:
            val = float(strval)
        except:
            val = default_value
        return val
    return default_value

# --------------------------------------------------------------------
#
# Function:      get_mysql_info
# Author:        Pete Humphrey
# Description:   Get mysql user and password
#                This looks for -u <name> and -p <name> on command line.
#                If either is not found, the user will be prompted.
#
# Return:        (user, pswd) or (none, none)
#
# --------------------------------------------------------------------


def get_mysql_info():
    user = get_arg('-u')
    pswd = get_arg('-p')

    if user == None:
        user = input("Enter your MySQL user name: ")

    if len(user) == 0:
        return (None, None)

    if pswd == None:
        pswd = getpass.getpass("Enter your MySQL password: ")

    if len(pswd) == 0:
        return (None, None)

    return (user, pswd)


def banner(*argv):
    b1 = '#' * 72
    b2 = '# '
    print("\n")
    print(b1)
    print(b2)
    for arg in argv:
        print(b2 + arg)
    print(b2)
    print(b1)
    print("\n")


def timestamp(dt=datetime.datetime.now()):
    return dt.strftime('%Y-%m-%d')


def askyn(prompt):
    ans = ""
    while len(ans) == 0:
        ans = input(prompt + " (y/n)? ").lower().strip()
    return (ans[0] == 'y')


def run(cmd, verbose=True, password=None):
    nice = re.sub("-p\\s+[\\S]+", "-p *******", cmd)
    nice = re.sub("-p[\\S]+", "-p*******", nice)
    if password:
        nice = nice.replace(password, "******")
    if verbose:
        print("Run: " + nice)
    os.system(cmd)
