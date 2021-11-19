import sys
import os

Verbose = False
DryRun = False


######################################################################
#
# Comand-line arguments
#
######################################################################

def get_flag(name):
    return name in sys.argv

def get_arg(name, default_value=None):
    result = default_value
    try:
        pos = sys.argv.index(name)
        if pos < (len(sys.argv) - 1):
            result = sys.argv[pos+1]
    except:
        result = default_value
    return result

def non_flag_args():
    result = []
    for arg in sys.argv[1:]:
        if not arg.startswith('-'):
            result.append(arg)
    return result

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

def run(cmd):
    print("Run:" + cmd)
    if not DryRun:
        os.system(cmd)



def ask(question):
    ans = ''
    while len(ans) == 0:
        ans = input(question)
    return ans
    
def askyn(question):
    ans = ask(question + " [y/n] ")
    return ans.lower().startswith('y')
    
def chdir(dir):
    print("chdir to " + dir)
    if not DryRun:
        os.chdir(dir)
    
def show_usage(errmsg = None):
    usage = "Use: add-migration [-v] [-dryrun] -db [cms|cmsusers] -name <migration_name>"
    if errmsg:
        banner(errmsg, usage)
    else:
        banner(usage)

DryRun = get_flag('-dryrun') or get_flag('-dry')
Verbose = get_flag('-v')

database = get_arg('-db')
migration_name = get_arg('-name')

if migration_name == None:
    show_usage("You must specify a migration name")
    quit()
    
if database != 'cms' and database != 'cmsusers':
    show_usage("You must specify either cms or cmsusers as the database name")
    quit()

subdir = 'DataModel'
if database == 'cmsusers':
    subdir = 'CMS'

chdir(subdir)
run('dotnet ef migrations add "{0}"'.format(migration_name))
run('dotnet ef migrations script -o "..\\DatabaseMigrations\\{0}_full.sql"'.format(migration_name) )
chdir('..')

    
