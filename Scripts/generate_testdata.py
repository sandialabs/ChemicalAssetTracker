import sys
import csv
import random
import pymysql
import time
import datetime
from utils import *
from cmsdb import MySQL

OwnerNames = [
    "Alda, Alan",
    "Bates, Betty",
    "Cruz, Charles",
    "Doe, David",
    "Evans, Eddie",
    "Franklin, Fred",
    "Goddel, George",
    "Hughes, Howard",
    "Inman, Iris",
    "Jones, Jennifer",
    "Korman, Kathy"
]

GroupNames = [
    "Group A",
    "Group B",
    "Group C",
    "Group D",
    "Group E"
]


CWCFLAG = 0
THEFTFLAG = 1
OTHERSECURITY = 2
CARCINOGEN = 3
HEALTHHAZARD = 4
IRRITANT = 5
ACUTETOXICITY = 6
CORROSIVE = 7
EXPLOSIVE = 8
FLAMABLE = 9
OXIDIZER = 10
COMPRESSEDGAS = 11
ENVIRONMENT = 12
OTHERHAZARD = 13


Pictograms = {}  # map CASNumber to Pictograms


def get_owners(db):
    global OwnerNames
    for name in OwnerNames:
        if not db.row_exists(f"select * from Owners where Name = '{name}'"):
            db.execute_nonquery(f"insert into Owners (Name) values ('{name}')")
    db.commit()
    return db.queryall("select * from Owners")
    
def get_groups(db):
    global GroupNames
    for name in GroupNames:
        if not db.row_exists(f"select * from StorageGroups where Name = '{name}'"):
            db.execute_nonquery(f"insert into StorageGroups (Name) values ('{name}')")
    db.commit()
    return db.queryall("select * from StorageGroups")
    
    

def random_date(start, end):
    """
    This function will return a random datetime between two datetime
    objects.
    """
    delta = end - start
    int_delta = (delta.days * 24 * 60 * 60) + delta.seconds
    random_second = random.randrange(int_delta)
    return start + datetime.timedelta(seconds=random_second)
    
def generate_list_of_chemicals(filename):
    result = []
    with open(filename) as csv_file:
        csv_reader = csv.reader(csv_file, delimiter=',')
        line_count = 0
        for row in csv_reader:
            if line_count == 0:
                # print(f'Column names are {", ".join(row)}')
                line_count += 1
            else:
                casnumber = row[0]
                formula = row[1]
                name = row[2]
                result.append(
                    {'casnum': casnumber, 'formula': formula, 'name': name})
                line_count += 1
    return result;


def read_pictograms(db):
    global Pictograms
    print(f"Building CAS# to Pictograms list")
    rows = db.queryall("select * from CASDataItems where Pictograms is not null")
    print(f"    CASDataItems contains {len(rows)} rows with pictograms")
    for row in rows:
        casnumber = row['CASNumber']
        pictograms = row['Pictograms']
        if pictograms:
            Pictograms[casnumber] = pictograms


def generate_cas_flags(casnumber):
    casflags = list('                ')
    if casnumber:
        if casnumber in Pictograms:
            pstr = Pictograms[casnumber]
            pictograms = pstr.split(',')
            # GHS06,GHS08,GHS05,GHS09,Dgr
            for name in pictograms:
                if name == "GHS01":   # Explosive
                    casflags[EXPLOSIVE] = 'X'
                elif name == "GHS02":   # Flammable
                    casflags[FLAMABLE] = 'X'
                elif name == "GHS03":   # Oxidizing
                    casflags[OXIDIZER] = 'X'
                elif name == "GHS04":   # Compressed Gas
                    casflags[COMPRESSEDGAS] = 'X'
                elif name == "GHS05":   # Corrosive
                    casflags[CORROSIVE] = 'X'
                elif name == "GHS06":   # Acute toxicity
                    casflags[ACUTETOXICITY] = 'X'
                elif name == "GHS07":   # Harmful (we call it "Irritant"
                    casflags[IRRITANT] = 'X'
                elif name == "GHS08":   # Health Hazard
                    casflags[HEALTHHAZARD] = 'X'
                elif name == "GHS09":   # Environmental Hazard
                    casflags[ENVIRONMENT] = 'X'
    return "".join(casflags)


def find_location(location_id, location_types):
    ancestors = [x for x in location_types if x.Name in x['ValidChildList']]
    if len(ancestors) == 0:
        return None
    else:
        return random.choice(ancestors)
    

            
def generate_child_locations(parent, location_map, location_type_map, db):
    parent_type = location_type_map[parent['LocationTypeID']]
    child_types = parent_type['ValidChildList']
    if len(child_types) > 0:
        for child_type_name in child_types:
            location_name = child_type_name + ' 1'
            parent_id  = parent['LocationID']
            level = int(parent['LocationLevel']) + 1
            location_type_id = location_type_map[child_type_name]['LocationTypeID']
            # print(f"Generating {location_name} under {parent['Name']}")
            args = (location_name, parent_id, location_type_id, level)
            sql = "insert into storagelocations (Name, ParentID, LocationTypeID, LocationLevel, IsLeaf) values (%s, %s, %s, %s, 0)"
            db.execute_nonquery(sql, args)
            db.commit()
            child_loc = db.queryone(f"select * from storagelocations where LocationID = {db.m_identity}");
            location_map[child_loc["LocationID"]] = child_loc
            generate_child_locations(child_loc, location_map, location_type_map, db)
    else:
        db.execute_nonquery(f"update storagelocations set IsLeaf = 1 where LocationID = {parent['LocationID']}")
        parent['IsLeaf'] = 1
    db.commit()
            
def generate_inventory(count, db, verbose=False):
    sdslist = []
    state_list = ['solid', 'liquid', 'gas']
    unit_list = {
        'solid': ['g', 'kg', 'mg', 'ft3', 'cm3'],
        'gas': ['ft3', 'cm3'],
        'liquid': ['L', 'mL']
    }
    
    chemicals = generate_list_of_chemicals('./Data/casdata.csv')
    leaf_locations = db.queryall("select * from StorageLocations where IsLeaf = 1")
    groups = get_groups(db)
    owners = get_owners(db)
    
    read_pictograms(db)

    today = datetime.date.today()
    now = datetime.datetime.now()

    # start and end dates for random expiration dates
    d1 = datetime.datetime(now.year + 1, now.month, now.day)
    d2 = datetime.datetime(now.year + 2, now.month, now.day)

    for file in os.listdir('../CMS/wwwroot/SDS'):
        sdslist.append(file)

    barcodenum = 0
    for i in range(count):
        global location_id_map
        chem = random.choice(chemicals)
        location = random.choice(leaf_locations)
        location_id = location['LocationID']

        owner = random.choice(owners)
        owner_id = owner['OwnerID']
        group = random.choice(groups)
        group_id = group['GroupID']

        expiry = random_date(d1, d2)
        # expiry_str = expiry.strftime('%Y-%m-%d')
        chemname = chem['name']
        casnum = chem['casnum']
        # barcode = 'BC' + '{0:08d}'.format(random.randint(1, 10000000))
        barcodenum += 1
        barcode = 'BC' + '{0:08d}'.format(barcodenum)
        if verbose:
            print(chemname)
            print(f'    CAS:      {casnum}')
            print(f'    Location: {location["PATH"]} ({locid})')
            print(f'    Barcode:  {barcode}')

        sds = random.choice(sdslist)

        state = random.choice(state_list)
        units = random.choice(unit_list[state])
        container_size = random.choice([5, 10, 15, 20, 25, 30, 35, 40, 45, 50])
        remaining = random.randint(1, container_size)
        flags = generate_cas_flags(casnum)

        sql = "insert into InventoryItems (Barcode, CASNumber, ChemicalName, LocationID,  Flags, OwnerID, GroupID, DateIn, ExpirationDate, State, ContainerSize, Units, RemainingQuantity, SDS, DisposeFlag) values (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, 0)"
        rc = db.execute_nonquery(sql, (barcode, casnum, chemname, location_id,
                               flags, owner_id, group_id, now, expiry,
                               state, container_size, units, remaining, sds))


        if (i % 10) == 0:
            db.commit()
            print(f"{i}    \r", end='')
    print("            ")
    db.commit()
    print(f"Inventory records created: {count}")


            
def generate_locations(db):
    location_types = db.queryall("select * from locationtypes")
    location_type_map = { }
    for lt in location_types:
        children = lt['ValidChildren']
        lt['ValidChildList'] = children.split(',') if len(children) > 0 else []
        location_type_map[lt['Name']] = lt
        location_type_map[lt['LocationTypeID']] = lt
        
    locations = db.queryall("select * from storagelocations")
    location_map = { }
    for location in locations:
        location_map[location['LocationID']] = location
        generate_child_locations(location, location_map, location_type_map, db)
    db.commit();
    # set the PATH column for every location  
    db.execute_nonquery("update StorageLocations set Path = GetLocationPath(LocationID)")

def clean_inventory(db):
    db.execute_nonquery("delete from InventoryItems")
    db.commit()
    print("Removing existing items from InventoryItems")

if __name__ == "__main__":
    if have_arg('-help'):
        banner('Use: python3 generate_testdata.py [-nolocs] [-count <n>]',
               '',
               '    -clean        delete any existing inventory',
               "    -nolocs       don't create new locations",
               '    -count <n>    number of inventory records to create',
               '    -help         display this message'
               )
        exit(0)
    (user, pswd) = get_mysql_info()
    db = MySQL('localhost', user, pswd, 'cms')
    if not have_arg('-nolocs'):
        generate_locations(db)
    if have_arg('-clean'):
        clean_inventory(db)
    count = int(get_arg("-count", 1000))
    t1 = datetime.datetime.now()
    generate_inventory(count, db)
    elapsed = datetime.datetime.now() - t1
    seconds = elapsed.total_seconds()
    minutes = seconds / 60.0
    print(f"Elapsed: {minutes:1.3} minutes")
    inventory_size = db.queryone('select count(*) as RowCount from InventoryItems')['RowCount']
    print(f"Inventory size: {inventory_size}")
    
