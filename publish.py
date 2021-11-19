import os
import sys
import subprocess

def usage():
    banner("publish [-dry] [-deploy <path>] [<project>|all]", 
                "Build and publish win-x64 self-contained deployment files", 
                "-dry                  Show commands but don't run them", 
                "-deploy <path>         Copy win-x64 files to the specified path",
                "<project>              The project to publish: import, CMS, or barcode", 
                "all                    Publish all projects")

def have_arg(name):
    return name in sys.argv


def get_arg(name, default_value=None):
    if have_arg(name):
        ix = sys.argv.index(name)
        if ix < (len(sys.argv) - 1):
            return sys.argv[ix+1]
    return default_value
    
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


def run_and_read(cmd, verbose=True):
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    response = proc.stdout.read()
    return "".join(chr(x) for x in response)


def run(cmd, verbose=True, dryrun=False):
    print("Run: " + cmd)
    os.system(cmd)
    
def get_git_commit():
    regex = re.compile("^commit\s+([a-z0-9]+).*Date:\\s+(.+\\d+:\\d+:\\d+)")
    response = run_and_read('git log -n 1').replace('\r', ' ').replace('\n', ' ')
    print(response)
    m = regex.match(response)
    if m:
        id = m.group(1)[0:7]
        dt = m.group(2)
        return(id + ': ' + dt)
    else:
        return "n/a"



def find_file(rootdir, filename):
    filepath = os.path.join(rootdir, filename)
    if os.path.isfile(filepath):
        return filepath
    subdirs = get_subdirs(rootdir)
    for subdir in subdirs:
        result = find_file(os.path.join(rootdir, subdir), filename)
        if result:
            return result
    return None


def publish(project, dryrun = False):
    print(" ")
    banner('Publishing {0} for win-x64'.format(project))
    run('dotnet build {0}'.format(project), verbose=True, dryrun=dryrun)
    run('dotnet publish {0} -c Debug -r win-x64 --self-contained'.format(project), verbose=True, dryrun=dryrun)

def copyfiles(source, destination, dryrun=False):
    run('robocopy "{0}" "{1}" /E >> robocopy.log'.format(source, destination), dryrun=dryrun, verbose=True)

def deploy(project, destination, dryrun = False):
    subdir = os.path.join(destination,project)
    print(f"Copying files to {subdir}")
    if not os.path.isdir(subdir):
        os.mkdir(subdir)
    copyfiles("./{0}/bin/Debug/netcoreapp2.2/win-x64/publish".format(project), subdir, dryrun)

def update_commit_id(deploy_dir):
    version_path = find_file(deploy_dir, "gitcommit.txt")
    if version_path:
        commit_id = get_git_commit()
        banner(f'Updating gitcommit.txt with {commit_id}')
        with open(version_path, 'w') as fp:
            fp.writelines([commit_id])
            fp.write('\n')
        print("")
        print("The output from msbuild is in msbuild.log")
    else:
        print("I can't find gitcommit.txt in the publish directory")



# print("Args", args.m_args)

if have_arg('-help'):
    usage()
    quit()

dryrun = have_arg('-dry')

deploy_dir = get_arg('-deploy', None)
if deploy_dir and not os.path.isdir(deploy_dir):
    print('The deployment directory, "{0}", does not exist.'.format(deploy_dir))
    quit()

if deploy_dir:
    if os.path.isfile("robocopy.log"):
        os.unlink("robocopy.log")

if have_arg('import') or have_arg('all'):
    publish('import', dryrun)
    if deploy_dir:
        deploy('import', deploy_dir, dryrun=dryrun)


if have_arg('cms') or have_arg('all'):
    publish('CMS', dryrun)
    if deploy_dir:
        deploy('CMS', deploy_dir, dryrun=dryrun)

if have_arg('barcode') or have_arg('all'):
    publish('BarcodeReader', dryrun)
    if deploy_dir:
        deploy('BarcodeReader', deploy_dir, dryrun=dryrun)

