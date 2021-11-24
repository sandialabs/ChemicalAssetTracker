import sys
import re
import json
from dotmap import DotMap
from cmsdb import *
from utils import *

CHEM_COLUMN = 1
CAS_COLUMN = 3
HAZARD_CLASS_COLUMN = 4
HAZARD_COLUMN = 5
PICTOGRAM_COLUMN = 6

#GHSHazardXlsx = './Data/echa_ghs_hazards.xlsx'
GHSHazardJson = './Data/echa_ghs_hazards.json'
GHSHazardCsv = './Data/hazard_codes.csv'
GHSText = './Data/ghs.txt'





def create_hazard_table(db):
    sql1 = "drop table if exists HazardCodes"
    sql2 = """
        create table HazardCodes(HazardCodeID int auto_increment primary key, 
                                 GHSCode varchar(16) not null, 
                                 CASNumber varchar(32) not null,
                                 HazardClass varchar(128) not null);
    """
    sql3 = "create index IX_HazardCodes_CASNumber on HazardCodes(CASNumber)"
    sql4 = "create index IX_HazardCodes_GHSCode on HazardCodes (GHSCode)"
    for sql in [sql1, sql2, sql3, sql4]:
        print(sql)
        db.execute_nonquery(sql)


HazardCodeInserts = {}


def update_hazard_codes(db, casnumber, hazardcodes, hazardclasses):
    global HazardCodeInserts
    casnumber = casnumber.strip()
    if casnumber not in HazardCodeInserts:
        inserts = []
        for ix, code in enumerate(hazardcodes):
            hclass = hazardclasses[ix].strip()
            if len(code) > 1:
                rec = DotMap({
                    'GHSCode': code,
                    'CASNumber': casnumber,
                    'HazardClass': hclass
                })
                inserts.append(rec)
        HazardCodeInserts[casnumber] = inserts
        # db.commit()
    # else:
    #     print(f"Duplicate CAS#: {casnumber}")


def store_hazard_codes(db):
    count = 0
    print("Inserting data into HazardCodes table")
    for casnumber in HazardCodeInserts.keys():
        inserts = HazardCodeInserts[casnumber]
        for insert in inserts:
            code = insert.GHSCode
            hclass = insert.HazardClass
            count += 1
            db.execute_nonquery("insert into HazardCodes (GHSCode, CASNumber, HazardClass) values (%s, %s, %s)", [code.strip(), casnumber, hclass], commit=((count % 100) == 0))
            print(f"{count}    \r", end='')
    print(f'Commiting {count} records to HazardCodes')
    db.commit()


CASDataItems = {}


def update_db(db, casnums, pictograms, chem):
    global CASDataItems
    newcount = 0
    updatecount = 0
    commit = False

    counter = 0
    for cas in casnums:
        #row = db.queryone(f"select * from CASDataItems where CASNumber = '{cas}'")
        counter += 1
        if cas in CASDataItems:
            row = CASDataItems[cas]
            # print(f'Updating CAS # {cas}: current count = {row.Count}')
            if pictograms != row.Pictograms:
                # take the union of the two sets of pictograms
                s1 = set_from_string(row.Pictograms)
                s2 = set_from_string(pictograms)
                if s1 != s2:
                    newpictograms = ','.join(s1.union(s2))
                    #print(f'Updating pictograms for {cas}: "{row.Pictograms}" + "{pictograms}" = "{newpictograms}"')
                    #db.execute_nonquery("update CASDataItems set Pictograms = %s where CASNumber = %s", [newpictograms, cas], commit=commit)
                    updatecount += 1
                    row.Pictograms = newpictograms
        else:
            #print(f"Adding {cas} ({chem}) to the database with {pictograms}")
            # sql = """
            #     insert into CASDataItems (CASNumber, ChemicalName, CWCFlag, TheftFlag, CarcinogenFlag, Pictograms)
            #     values (%s, %s, ' ', ' ', ' ', %s)
            # """
            # db.execute_nonquery(sql, [cas, chem, pictograms], commit=commit)
            CASDataItems[cas] = DotMap({'ChemicalName': chem, 'Pictograms': pictograms, 'Count': 1})
            newcount += 1
    return (newcount, updatecount)


def store_cas_data_items(db):
    sql = """
        insert into CASDataItems (CASNumber, ChemicalName, CWCFlag, TheftFlag, CarcinogenFlag, Pictograms)
        values (%s, %s, ' ', ' ', ' ', %s)
    """
    print(f"Inserting {len(CASDataItems)} records into CASDataItems")
    count = 0
    for casnumber in CASDataItems.keys():
        rec = CASDataItems[casnumber]
        count += 1
        print(f'{count}   \r', end='')
        db.execute_nonquery(sql, [casnumber, rec.ChemicalName, rec.Pictograms], commit=((count % 10) == 0))
    db.commit()




def set_from_string(text):
    parts = text.split(',')
    for i in range(len(parts)):
        parts[i] = parts[i].strip()
    return {x for x in parts}




def parse_codes(ghsdata, db):
    '''
    Update CASDataItems and HazardCodes from echa_ghs_hazards.json.  
    Also writes ghs.txt and hazard_codes.csv.
    '''
    newcount = 0
    updatecount = 0
    global GHSHazardJson
    global GHSHazardCsv

    db.execute_nonquery("delete from HazardCodes")
    db.execute_nonquery("delete from CASDataItems")

    sql_stmts = []

    with open(GHSText, 'w', encoding='utf-8') as ghsfp:
        with open(GHSHazardCsv, 'w', encoding='utf-8') as hazardfp:
            hazardfp.write(f"CASNumber,HazardCode\n")
            for item in ghsdata:
                if len(item.casnums) > 0 and len(item.pictograms) > 0:
                    for casnum in item.casnums:
                        update_hazard_codes(db, casnum, item.hazard_codes, item.hazard_classes)
                    n, u = update_db(db, item.casnums, item.pictograms, item.chem)
                    newcount += n
                    updatecount += u
    db.commit()
    store_hazard_codes(db)
    store_cas_data_items(db)
    prows = db.row_count("select * from CASDataItems where Pictograms is not null")
    print(f"There are {prows} rows in CASDataItems with pictograms")
    print(f"New records: {newcount}   Updated records: {updatecount}")


# wb = ExcelWorkbook(GHSHazardXlsx)
# ws = wb.select_sheet(0)

with open(GHSHazardJson) as fp:
    ghstext = fp.read()
    jsonlist = json.loads(ghstext)
    ghs_data = [DotMap(x) for x in jsonlist]

user, pswd = get_mysql_info()
hostname = get_arg('-h', 'localhost')
#print("hostname is " + hostname)
if user and pswd:
    db = MySQL(hostname, user, pswd, 'cms')
    db.execute_nonquery("delete from HazardCodes")
    parse_codes(ghs_data, db)
