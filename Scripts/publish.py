import sys
import os
import datetime
import re
import subprocess
from utils import *

PublishDir = "C:\\Publish"

def show_usage():
    banner("Use: python3 publish.py [-help] [-dir <directory>]")

if have_arg('-help'):
    show_usage()
    exit(0)

def publish(path):
    if os.path.isdir(path):
        run(f'rmdir /S/Q "{path}"')
    cmd = f'dotnet publish -c Release -f netcoreapp2.0 -o "{path}"'
    run(cmd, True)


banner(
    "Publish the CAT Web Application", 
    " ",
    "This script will package the CAT runtime files and store",
    f"them in {PublishDir}\\CAT and {PublishDir}\\CAT_YYYY-MM-DD.",
    "",
    "You can override the root directory with the -dir flag."
)

def run_and_read(cmd, verbose=True):
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    response = proc.stdout.read()
    return "".join(chr(x) for x in response)

    
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

def get_subdirs(rootdir):
    result = [f for f in os.listdir(rootdir) if os.path.isdir(os.path.join(rootdir, f))]
    return result



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

def update_commit_id(deploy_dir, show=False):
    version_path = find_file(deploy_dir, "gitcommit.txt")
    if version_path:
        commit_id = get_git_commit()
        if show:
            banner(f'Updating {version_path} with "{commit_id}"')
        with open(version_path, 'w') as fp:
            fp.writelines([commit_id])
            fp.write('\n')
        if show:
            print("")
            print("The output from msbuild is in msbuild.log")
    else:
        print("I can't find gitcommit.txt in the publish directory")



output_dir = get_arg("-dir", PublishDir)
suffix = datetime.datetime.now().strftime("%Y-%m-%d")
path1 = os.path.join(output_dir, "CAT")
path2 = os.path.join(output_dir, "CAT_" + suffix)

if askyn("Publish to " + path1):
    os.chdir("../CMS")
    run("npm run bundle")
    publish(path1)
    publish(path2)
    update_commit_id(path1, True)
    update_commit_id(path2, False)
    run(f'dir "{output_dir}"')