from cmsdb import *
from excel import *
from database import *
import os
import sys
import platform
import json
from utils import *


if platform.system() != 'Windows':
    if os.path.isfile('coc.sql'):
        run('mysql -u cms -p cms -e "source coc.sql"')
    else:
        print("Error: coc.sql not found.")
    exit(0)


def parse_yesno(text):
    if text == 'yes':
        return 1
    else:
        return 0

def import_json(filepath, verbose=False):
    if not os.path.isfile(filepath):
        raise Exception(f"{filepath} does not exist")

    hostname = get_arg('-h', 'localhost')
    user, pswd = get_mysql_info()
    db = MySQL(hostname, user, pswd, 'cms')
    db.execute_nonquery("truncate table ChemicalsOfConcern")

    jsondata = None
    with open(filepath) as fp:
        jsondata = json.load(fp)
    count = 0
    for data in jsondata:
        sql = f'insert into ChemicalsOfConcern (ChemicalName, CASNumber, CWC, CFATS, EU, AG, WMD, OTHER) VALUES (%s, %s, %s, %s, %s, %s, %s, False)'
        db.execute_nonquery(sql, parameters=[data['ChemicalName'], data['CASNumber'], data['CWC'], data['CFATS'], data['EU'], data['AG'], data['WMD']])
        count += 1
    db.commit()
    print(f"    Inserted {count} records from {filepath} into ChemicalsOfConcern table")


    

def import_spreadsheet(filepath, verbose=False):
    if not os.path.isfile(filepath):
        raise Exception(f"{filepath} does not exist")

    wb = ExcelWorkbook(filepath)
    ws = wb.select_sheet('Combined List No Duplicates')

    ws.read_column_names(0)

    cocdata = []

    for i in range(1, ws.RowCount):
        row = ws.get_row(i)
        name = row['Chemical Names'].strip()
        casnum = row['Chemical Abstracts Service (CAS) Registry Numbers'].strip()
        cwc = row['CWC'].strip()
        cfats = row['CFATS'].strip()
        eu = row['EU'].strip()
        ag = row['AG'].strip()
        wmd = row['WMD'].strip()
        if verbose:
            print(f'Name: {name} at row {i}')
            print(f'    CAS:    "{casnum}"')
            print(f'    CWC:    "{cwc}"')
            print(f'    CFATS:  "{cfats}"')
            print(f'    EU: "{eu}"')
            print(f'    AG: "{ag}"')
            print(f'    WMD: "{wmd}"')

        iscwc = parse_yesno(cwc)
        iscfats = parse_yesno(cfats)
        iseu = parse_yesno(eu)
        isag = parse_yesno(ag)
        iswmd = parse_yesno(wmd)
        cocobj = {
            'ChemicalName': name,
            'CASNumber': casnum,
            'CWC': iscwc,
            'CFATS': iscfats,
            'EU': iseu,
            'AG': isag,
            'WMD': iswmd,
            'OTHER': False
        }
        cocdata.append(cocobj)
    (dirname, _) = os.path.split(filepath)
    jsonfile = os.path.join(dirname, "chemicals-of-concern.json")
    with open(jsonfile, 'w') as fp:
        jsonstr = json.dumps(cocdata, indent=4)
        fp.write(jsonstr)
    print(f"Wrote {len(cocdata)} records to {jsonfile}")


def show_usage():
    banner("Use: python3 import-chemicals-of-concern.py [-help] -i <filepath>"
         , " "
         , "<filepath>            path to an .xlsx or .json file"
         , " "
         , "If the input file is a spreadsheet, it will be converted into"
         , "a JSON file and saved as chemicals-of-concern.json,"
         , "otherwise the json file will be read and the data stored in the"
         , "ChemicalsOfConcern table")


def import_coc_main():
    input_file = get_arg('-i')
    if input_file:
        if os.path.isfile(input_file):
            if input_file.lower().endswith('.xlsx'):
                import_spreadsheet(input_file)
            elif input_file.lower().endswith('.json'):
                import_json(input_file)
            else:
                print("Error: invalid input file extension")
        else:
            print(f'Error: {input_file} does not exist')
    else:
        show_usage()

if __name__ == "__main__":
    import_coc_main()