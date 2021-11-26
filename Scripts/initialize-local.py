import sys
import os
from utils import *


def prun(cmd):
    print(cmd)

def chdir(path):
    print("chdir to " + path)
    os.chdir(path)

def get_user_input(prompt):
    ans = input(prompt).strip()
    while len(ans) == 0:
        ans = input(prompt).strip()
    return ans

def wait_for_user(prompt):
    print("")
    print("-----------------------------------------------------------------")
    print("- " + prompt)
    print("-----------------------------------------------------------------")
    input("")

cwd = os.getcwd()
if not cwd.endswith('Scripts'):
    print("Please run this script from the Scripts directory")
    exit(0)

wait_for_user("Press RETURN to install python libraries")
run("pip install -r requirements.txt")

wait_for_user("Press RETURN to create a clean database")
institution = get_user_input("Enter the name of the root of your location hierarchy: ")
run(f'python managedb.py -clean -institution "{institution}"')

wait_for_user("Press RETURN to create NodeJS libraries")
chdir("../CMS")
run("npm install", True)
run("npm run bundle", True)

wait_for_user("Press RETURN to start Visual Studio:")
chdir("..")
run("start ChemicalAssetTracker.sln", True)

print("Done")