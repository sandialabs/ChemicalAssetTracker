import os
import sys
import json
import re

######################################################################
#
# Comand-line arguments
#
######################################################################


def have_arg(name):
    return name in sys.argv


def get_arg(name, default_value=None):
    if have_arg(name):
        pos = sys.argv.index(name)
        if pos < (len(sys.argv) - 1):
            return sys.argv[pos+1]
    return default_value

######################################################################
#
# Classes
#
######################################################################


class PackageInfo:

    def __init__(self, path):
        self.IsValid = False
        self.Error = None
        self.Repository = None
        self.Version = None
        self.LicenseURL = None
        with open(path, encoding='utf8') as fp:
            jsondata = json.load(fp)
        if 'name' in jsondata:
            try:
                self.Name = jsondata['name']
                if self.Name.startswith('@'):
                    self.Name = self.Name[1:]
                self.Description = jsondata['description']
                if 'repository' in jsondata:
                    self.Repository = self.nice_url(jsondata['repository'])
                if 'version' in jsondata:
                    self.Version = jsondata['version']
                if 'license' in jsondata:
                    self.LicenseType = jsondata['license']
                    self.IsValid = True
                elif 'licenses' in jsondata:
                    license_info = jsondata['licenses']
                    infotype = type(license_info)
                    #print(f'{path} license information is {infotype}')
                    if infotype == list:
                        license_info = license_info[0]
                        self.LicenseType = license_info['type']
                        self.LicenseURL = self.nice_url(license_info['url'])
                        self.IsValid = True
                    else:
                        self.Error = f'Invalid license: {infotype}'
                else:
                    self.Error = f'{indent}No license in {path}'
            except Exception as ex:
                self.Error = f'Exception in {path}: {ex}'
        else:
            self.Error = f'No package name in {path}'

    def nice_url(self, url):
        if type(url) == dict:
            return self.nice_url(url['url'])
        else:
            regex = re.compile(r'^(.*/[^/]+)\.git$')
            urlsave = url
            if url.startswith('git+https:'):
                url = "https:" + url[10:]
            if url.startswith('git+ssh:'):
                # git+ssh://git@github.com/yargs/y18n)
                url = url[8:]
                if url.startswith('//git@'):
                    url = 'https://' + url[6:]
            if url.startswith('http:'):
                pass
            if url.startswith('https:'):
                pass
            if url.startswith('git:'):
                url = "https:" + url[4:]
            m = regex.match(url)
            if m:
                url = m[1]
            # print(f'"{urlsave}" => "{url}"')
            return url

    def to_csv(self, fp):
        url = self.LicenseURL or self.Repository
        description = self.Description
        if ',' in description:
            description = '"' + description + '"'
        csvline = f'{self.Name},{self.LicenseType},{url},{description}'
        fp.write(csvline + '\n')
        
    def to_object(self):
        url = self.LicenseURL or self.Repository
        description = self.Description
        if ',' in description:
            description = '"' + description + '"'
        info = {
            "name": self.Name,
            "license": self.LicenseType,
            "version": self.Version,
            "url": url
        }
        return info

    def __str__(self):
        url = self.LicenseURL or self.Repository
        return f'<Package {self.Name}/{self.LicenseType} ({url})>'


######################################################################
#
# Utility Routines
#
######################################################################


######################################################################
#
# Main
#
######################################################################

node_dir = get_arg('-dir', 'node_modules')
output_file = get_arg('-o', 'package-list.csv')
json_file = get_arg('-json', 'package-list.json')

if not os.path.isdir(node_dir):
    print(f'{node_dir} does not exist or is not a directory.')
    exit()


sep = '\\'
packages = []
for subdir, child_dirs, files in os.walk(node_dir):
    # subdir is the current subdirectory
    # child_dirs is an array of subdirs of the current subdirectory
    # files is an array of filenames
    parts = subdir.split(sep)
    depth = len(parts) - 1
    indent = ''
    #indent = '    '*depth
    #print(f'{indent}Processing subdir: {subdir}')
    if 'package.json' in files:
        package_info = PackageInfo(subdir + sep + 'package.json')
        if package_info.IsValid:
            packages.append(package_info)
        else:
            print(package_info.Error)


with open(output_file, 'w', encoding='utf8') as fp:
    fp.write('Package Name,LicenseType,URL,Description\n')
    for package in packages:
        package.to_csv(fp)

package_data = []     
for package in packages:
    package_data.append(package.to_object())

with open(json_file, "w", encoding='utf8') as ofp:    
    foo = json.dumps({ "packages": package_data}, indent=4)
    ofp.write(foo);


