import sys
import csv
import random
import time
import datetime
from dotmap import DotMap
from utils import *
from cmsdb import *

LocationTypes = DotMap()
RootType = None
Institution = None
RootLocation = None
Verbose = False


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

TestLocations = DotMap({ 
    "type": "ROOT", 
    "count": 1,
    "children": [ 
        { 
            "type": "University", 
            "count": 3, 
            "children": [ 
                { 
                    "type": "College", 
                    "count": 3, 
                    "children": [
                        {
                            "type": "Building",
                            "count": 3,
                            "children": [
                                {
                                    "type": "Room",
                                    "count": 4,
                                    "children": [
                                        {
                                            "type": "Cabinet",
                                            "count": 2,
                                            "children": [ 
                                                {
                                                    "type": "Shelf",
                                                    "count": 4,
                                                    "children": [],
                                                }
                                            ]
                                        },
                                        {
                                            "type": "Refrigerator",
                                            "count": 2,
                                            "children": [],
                                        },
                                        {
                                            "type": "Shelf",
                                            "count": 6,
                                            "children": [],
                                        }
                                    ]
                                }
                            ]
                        }
                    ] 
                }, 
            ] 
        }  
    ] 
}, _dynamic=False)


def log(text):
    global Verbose
    if Verbose:
        print(text)


def connect():
    global LocationTypes
    global RootLocation
    global Institution
    global RootType

    hostname = get_arg('-h', 'localhost')
    user, pswd = get_mysql_info()

    db = CMSDB(hostname, user, pswd)
    print('Connected to database')
    RootLocation = db.get_location_by_id(1)
    Institution = db.queryone(f"select * from Settings where SettingKey = 'Institution'").SettingValue
    refresh_location_types(db)
    RootType = LocationTypes[Institution]
    TestLocations.type = Institution
    return db



def get_owners(db):
    global OwnerNames
    for name in OwnerNames:
        if not db.row_exists(f"select * from Owners where Name = '{name}'"):
            db.execute_nonquery(f"insert into Owners (Name) values ('{name}')")
    db.commit()
    return list(db.queryall("select * from Owners"))
    
def get_groups(db):
    global GroupNames
    for name in GroupNames:
        if not db.row_exists(f"select * from StorageGroups where Name = '{name}'"):
            db.execute_nonquery(f"insert into StorageGroups (Name) values ('{name}')")
    db.commit()
    return list(db.queryall("select * from StorageGroups"))
    
    

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
    rows = list(db.queryall("select * from CASDataItems where Pictograms is not null"))
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

   
def refresh_location_types(db):
    global LocationTypes

    LocationTypes = DotMap(_dynamic=False)
    for row in db.queryall("select * from LocationTypes"):
        LocationTypes[row.Name] = row


def define_location_type(name, db):
    global LocationTypes
    if name in LocationTypes:
        return LocationTypes[name]
    else:
        db.execute_nonquery(f"insert into LocationTypes (Name, ValidChildren) values ('{name}', '')")
        db.commit()
        log("Created location type " + name)
        refresh_location_types(db)
        return LocationTypes[name]

def update_location_type(loctype, valid_children, db):
    sql = f"update LocationTypes set ValidChildren = '{valid_children}' where LocationTypeID = {loctype.LocationTypeID}"
    log("    update_location_type: " + sql)
    db.execute_nonquery(sql)
    db.commit()


def create_location(name, parent, type_id, is_leaf, db):
    existing = db.get_location_by_name(name, parent.LocationID)
    if existing:
        log(f"Location {name} with parent {parent.LocationID} already exists")
    else:
        db.execute_nonquery(f"insert into StorageLocations (Name, ParentID, LocationTypeID, IsLeaf, LocationLevel) values ('{name}', {parent.LocationID}, {type_id}, {is_leaf}, {parent.LocationLevel + 1})" )
        id = db.m_identity;
        db.commit();
        return db.get_location_by_id(id)


def create_type(name, parent, db):
    global TestLocations
    global LocationTypes

    typeobj = define_location_type(name, db)
    parent_children = parent.ValidChildren.split(',') if len(parent.ValidChildren) > 0 else []
    if name not in parent_children:
        parent_children.append(name)
        childlist = ",".join(parent_children)
        log(f"    Adding {name} as child of {parent.Name} => {childlist}")
        update_location_type(parent, childlist, db)
        parent.ValidChildren = childlist   # the calling function needs to have the latest version of the parent
        refresh_location_types(db)
    else:
        log(f"    {parent.Name} already has a child named {name}")
    return typeobj


def initialize_location_type(typedef, parent, db):
    log(f"initialize location type {typedef.type} with parent {parent.Name}")
    typeobj = create_type(typedef.type, parent, db)
    
    for child in typedef.children:
        initialize_location_type(child, typeobj, db)


def create_locations(typedef, parent, db, indent = ''):
    locs = []
    count = typedef.count
    is_leaf = 0
    if len(typedef.children) == 0:
        is_leaf = 1

    # create n locations of this type
    log(indent + f'creating {count} {typedef.type} locations for for {parent.Name}')
    for ix in range(count):
        name = f"{typedef.type}{ix+1}"
        typeid = LocationTypes[typedef.type].LocationTypeID
        newloc = create_location(name, parent, typeid, is_leaf, db)
        locs.append(newloc)

    log(indent + f"Generating child locations for {typedef.children}")
    # for each location we just created, create its child locations
    for newlocation in locs:
        for child in typedef.children:
            create_locations(child, newlocation, db, indent + '  ')

def generate_location_types(db):
    global RootType
    global TestLocations

    print("Generating LocationTypes table")

    for typedef in TestLocations.children:
        initialize_location_type(typedef, RootType, db)


def generate_locations(db):
    global TestLocations, RootLocation

    print("Generating StorageLocations table")

    for typedef in TestLocations.children:
        create_locations(typedef, RootLocation, db)
    db.execute_nonquery("update StorageLocations set Path = GetLocationPath(LocationID)")


            
def generate_inventory(count, db, verbose=False):
    sdslist = []
    state_list = ['solid', 'liquid', 'gas']
    unit_list = {
        'solid': ['g', 'kg', 'mg', 'ft3', 'cm3'],
        'gas': ['ft3', 'cm3'],
        'liquid': ['L', 'mL']
    }
    
    chemicals = generate_list_of_chemicals('./Data/casdata.csv')
    leaf_locations = list(db.queryall("select * from StorageLocations where IsLeaf = 1"))
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

    print(f"Generate {count} inventory items")
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
            print(f'    Location: {location.Path} ({location_id})')
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


def generate_testdata(db):
    count = int(get_arg("-count", 1000))
    generate_location_types(db)
    generate_locations(db)
    generate_inventory(count, db, Verbose)



def clean_inventory(db):
    db.execute_nonquery("delete from InventoryItems")
    db.commit()
    print("Removing existing items from InventoryItems")


Verbose = have_arg('-v')

if __name__ == "__main__":
    if have_arg('-help'):
        banner('Use: python3 generate_testdata.py [-nolocs] [-count <n>]',
               '',
               '    -clean        delete any existing inventory',
               '    -count <n>    number of inventory records to create',
               '    -help         display this message'
               )
        exit(0)
    db = connect()
    if have_arg('-clean'):
        clean_inventory(db)
    t1 = datetime.datetime.now()
    generate_testdata(db)
    elapsed = datetime.datetime.now() - t1
    seconds = elapsed.total_seconds()
    minutes = seconds / 60.0
    print(f"Elapsed: {minutes:1.3} minutes")
    inventory_size = db.queryone('select count(*) as RowCount from InventoryItems')['RowCount']
    print(f"Inventory size: {inventory_size}")
    
