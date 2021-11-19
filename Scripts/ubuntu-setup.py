import sys
import os
import time

def run(cmd):
    print("Run: " + cmd)
    os.system(cmd)

def sudo(cmd):
    run("sudo " + cmd)


def banner(lines):
    if type(lines) == str:
        lines = [lines]
    print(" ")
    print('#' * 72)
    print ("#")
    for line in lines:
        print('# ' + line)
    print ("#")
    print('#' * 72)
    print(" ")

def minibanner(lines):
    if type(lines) == str:
        lines = [lines]
    print(" ")
    print ("#")
    for line in lines:
        print('# ' + line)
    print ("#")
    print(" ")

def prompt(question = "Continue? (y/n) "):
    ans = ''
    while ans == '':
        ans = input(question).lower()
    return(ans[0] == 'y')

def run_with_prompt(cmd, lines):
    banner(lines)
    if prompt():
        run(cmd)

def install_pip(askfirst):
    banner("Install pip3")
    if askfirst == False or prompt("Install pip3? "):
        run('sudo apt install python3-pip')

def install_git(askfirst):
    banner("Install GIT")
    if askfirst == False or prompt("Install GIT? "):
        run("sudo apt-get install git")

def install_dotnet(askfirst):
    banner(["Install .NET Core 2.2 SDK", "From https://code.visualstudio.com/docs/?dv=linux64_deb"])
    if askfirst == False or prompt("Install .NET Core 2.2? "):
        run('wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb')
        run('sudo dpkg -i packages-microsoft-prod.deb')
        run('sudo add-apt-repository universe')  
        run('sudo apt-get update')
        run('sudo apt-get install apt-transport-https')
        run('sudo apt-get install dotnet-sdk-2.2')

def install_mysql(askfirst):
    banner("Install MySQL")
    if askfirst == False or prompt("Install MySQL? "):
        run("sudo apt install mysql-server")
        run("sudo systemctl status mysql")
        mysql_users = ['pete', 'cms']
        with open('createusers.sql', "w") as fp:
            for user in mysql_users:
                fp.write(f"create user '{user}'@'localhost' identified by '{user}';\n");
                fp.write(f"grant all on *.* to '{user}'@'localhost';\n")
        run("sudo mysql < createusers.sql")

def install_npm(askfirst):
    banner("Install npm")
    if askfirst == False or prompt("Install npm? "):
        run("sudo apt-get install npm")

def install_vscode(askfirst):
    banner("Install Visual Studio Code")
    if askfirst == False or prompt("Install VSCode? "):
        print("*** A Firefox window will appear asking if you want to install VSCode")
        time.sleep(10)
        run('xdg-open https://code.visualstudio.com/docs/?dv=linux64_deb')

def install_apache(askfirst):
    banner("Install and Set Up Apache")
    conftext = [
        "<VirtualHost *:80>",
        "ProxyPreserveHost On",
        "ProxyPass / http://127.0.0.1:5000/",
        "ProxyPassReverse / http://127.0.0.1:5000/",
        "ServerName www.catserver.com",
        "ServerAlias *.catserver.com",
        "ErrorLog ${APACHE_LOG_DIR}-cat-error.log",
        "CustomLog ${APACHE_LOG_DIR}-cat-access.log common",
        "</VirtualHost>"
    ]
    if askfirst == False or prompt("Install Apache? "):
        sudo("apt update")
        sudo("apt install apache2")
        minibanner("Configure firewall to allow port 80 traffic")
        sudo("ufw app list")
        sudo("ufw allow 'Apache'")
        sudo("ufw enable")
        minibanner("Show Firewall and Apache status")
        sudo("ufw status")
        sudo("systemctl status apache2")
        minibanner("Enable necessary Apache modules")
        sudo("a2enmod proxy")
        sudo("a2enmod proxy_http")
        sudo("a2enmod proxy_balancer")
        sudo("a2enmod lbmethod_byrequests")
        # create conf file for CAT
        minibanner("Configure Apache Virtual Host (www.catserver.com)")
        with open('cat.conf', 'w') as fp:
            for line in conftext:
                fp.write(line + "\n")
        sudo("cp cat.conf /etc/apache2/sites-available/")
        # enable our site
        sudo("a2ensite cat.conf")
        # disable default site
        sudo("a2dissite 000-default.conf")
        # test configuration
        sudo("systemctl restart apache2")
        sudo("apache2ctl configtest")
        banner([
            "Apache logs for CAT will be in",
            "    /var/log/apache2-cat-access.log and ",
            "    /var/log/apache2-cat-error.log"
        ])


def install_cat(askfirst):
    banner("Install CAT")
    if askfirst == False or prompt("Install CAT? "):
        run('git clone https://gitlab.petehumphrey.net:1953/wrhumph/cat.git CAT')

def initialize_cat(askfirst):
    banner("Initialize CAT Database")
    if askfirst == False or prompt("Initialize CAT databases? "):
        os.chdir('CAT/Scripts')
        run('pip3 install -r requirements.txt')
        run('mysql -u pete -ppete CMSUsers < cmsusers-full.sql')
        run('python3 create-cms-db.py -u pete -p pete -force -testdata -institution "Ministry of Education"')
        os.chdir('../..')

def start_cat(askfirst):
    banner("Start CAT")
    if askfirst == False or prompt("Start CAT now? "):
        os.chdir("CAT/CMS")
        run("npm install")
        run("npm run bundle")
        run("dotnet run")
        os.chdir('../..')
    else:
        banner([
            'To start CAT the first time, do the following.',
            '    $ cd CAT/CMS     - this is where the CAT web server is',
            '    $ npm install    - downloads packages used by CAT',
            '    $ npm run bundle - uses webpack to bundle libraries for CAT',
            '    $ dotnet run     - start the server',
        ])


def setup():
    install_pip(True)
    install_git(True)
    install_dotnet(True)
    install_mysql(True)
    install_npm(True)
    install_vscode(True)
    install_cat(True)
    initialize_cat(True)
    install_apache(True)
    start_cat(True)


setup()
