import os
import sys
import json
import requests
from utils import *


def show_usage():
    banner("Use: python3 initialize-users.py [-help] [-url <caturl>] -p <pswd>",
           "",
           "This script will initialize the root account in CAT.",
           "",
           "Options:",
           "    -help            display this text",
           "    -url <url>       specify the CAT base URL",
           "    -anycert         allow any HTTPS certificate",
           "    -p <pswd>        specify the password for the root account",
           "",
           "Ex: python3 initialize-users.py -url https://mycat.azure.com -p root")


if have_arg('-help'):
    show_usage()
    exit(0)

url = get_arg("-url", None)
rootpswd = get_arg("-p", None)

if url and rootpswd:
    api = f"{url}/api/CreateRootAccount/{rootpswd}"
    print(api)
    verify = not have_arg('-anycert')
    response = requests.get(api, verify=verify)
    print(json.dumps(response.json(), indent=4))
else:
    show_usage()
