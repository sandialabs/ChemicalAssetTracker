import sys
import csv
import random
import pymysql
import time
import datetime
from utils import *
from location_model import *
from iraq_setup import *

chemicals = []
root = 'IRAQ'
sites = []
locales = []
buildings = []
rooms = []
storages = []
shelves = []

LocationTypes = {}

ItemID = 0

SHELF_LEVEL = 6

leaf_locations = []
location_id_map = {}  # map LocationID to storagelocation row data
locations = {}        # map full location name to LocationID

batch_size = 1000

db = None
cur = None

Verbose = True

CASFlagMap = {
    "7782-50-5": ' X ',
    "7782-44-7": '   ',
    "7732-18-5": '   ',
    "7722-84-1": ' X ',
    "7664-39-3": ' X ',
    "7647-01-0": ' X ',
    "76-06-2":   '3  ',
    "7439-94-4": ' X ',
    "7040-57-5": '1X ',
    "628-86-4":  ' X ',
    "51-75-2":   '1XX',
    "50782-69-9": '1X ',
    "464-07-3":  '2  ',
    "40334-70-1": '1X ',
    "7664-39-3": ' X ',
}

UniversityNames = [
    "Al-Mustansiriya University",
    "Al-Nahrain University",
    "Iraqi University",
    "University of Baghdad",
    "University of Technology",
    "University of Information Technology and Communications",
    "Uruk University",
    "Al-Rafidain University College",
    "Al- Nisour University College",
    "Al Muthana University",
]

CollegeNames = [
    "School of Medicine",
    "Chemistry Department",
    "Chemical Engineering College",
    "College of Pharmacy"
]

morenames = [
    "Babylon University",
    "Diyala University",
    "Kirkuk University",
    "Hawler Medical University",
    "Kufa University",
    "Misan University",
    "Samarra University",
    "Thi Qar University",
    "Tikrit University",
    "University of Al-Qadisiyah",
    "University of Anbar",
    "University of Basrah",
    "University of Karbala",
    "University of Mosul",
    "Wasit University",
    "Jabir ibn Hayyan Medical University",
    "Mussaib Technical College",
]

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
Owners = {}
StorageGroups = {}


def have_flag(name):
    return name in sys.argv


def get_arg(name, default_value):
    if have_flag(name):
        ix = sys.argv.index(name)
        if ix < (len(sys.argv) - 1):
            return sys.argv[ix+1]
    return default_value


def verbose_message(msg):
    global Verbose
    if Verbose:
        print(msg)


def random_date(start, end):
    """
    This function will return a random datetime between two datetime
    objects.
    """
    delta = end - start
    int_delta = (delta.days * 24 * 60 * 60) + delta.seconds
    random_second = random.randrange(int_delta)
    return start + datetime.timedelta(seconds=random_second)


def commit_changes():
    start_time = time.time()
    db.commit()
    elapsed_time = time.time() - start_time
    # print(f'Commit - {elapsed_time}')


def read_scalar(sql, column):
    rowcount = cur.execute(sql)
    if rowcount > 0:
        row = cur.fetchone()
        return row[column]
    else:
        return None


def count_rows(table):
    rc = int(read_scalar(f"select count(*) as 'Count' from {table}", 'Count'))
    return rc


def read_location_types():
    global cur, LocationTypes
    verbose_message("Reading LocationTypes")
    cur.execute('select * from LocationTypes')
    rows = cur.fetchall()
    for row in rows:
        type_id = row['LocationTypeID']
        name = row['Name']
        LocationTypes[name] = type_id
    verbose_message(f"    Read {len(LocationTypes)} location types")


def read_pictograms():
    global Pictograms
    banner("Building CAS# to Pictograms list")
    cur.execute("select * from CASDataItems where Pictograms is not null")
    rows = cur.fetchall()
    print(f"    CASDataItems contains {len(rows)} rows with pictograms")
    for row in rows:
        casnumber = row['CASNumber']
        pictograms = row['Pictograms']
        if pictograms:
            Pictograms[casnumber] = pictograms


def generate_list_of_chemicals(filename):
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
                chemicals.append(
                    {'casnum': casnumber, 'formula': formula, 'name': name})
                line_count += 1
        # print(f'Processed {line_count} lines.')


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


def generate_casdataitems():
    casmap = {}
    sql = "insert into CASDataItems (CASNumber, ChemicalName, CWCFlag, TheftFlag, CarcinogenFlag) values (%s, %s, %s, %s, %s)"
    count = 0
    print("Inserting CASDataItems ...")
    for chem in chemicals:
        casnum = chem['casnum']
        if casnum not in casmap:
            cwc = ' '
            theft = ' '
            carc = ' '
            casmap[casnum] = 1
            count += 1
            cur.execute(
                sql, (chem['casnum'], chem['name'], cwc, theft, carc))
    commit_changes()
    print(f'    inserted {count} items')


def generate_list(prefix, count):
    global ItemID
    result = []
    for i in range(count):
        ItemID += 1
        item = f'{prefix} {ItemID:05d}'
        result.append(item)
    return result


def get_location_id(loc, level, parent, create=True, commit=True):
    result = None

    (stripped_loc, loctype_name) = loc.split(':')
    rowcount = cur.execute(
        "select LocationID from StorageLocations where Name = %s and ParentID = %s", (stripped_loc, parent))
    if rowcount > 0:
        row = cur.fetchone()
        result = row['LocationID']
    elif create:
        loctype_id = LocationTypes[loctype_name]
        rowcount = cur.execute(
            "insert into StorageLocations (Name, ParentID, LocationLevel, LocationTypeID) values (%s, %s, %s, %s)", (stripped_loc, parent, level, loctype_id))
        if rowcount != 1:
            print("Insert failed")
        else:
            result = cur.lastrowid
            # print(f'Adding "{loc}" at level {level} with parent {parent} => {result}')
            if commit:
                commit_changes()
    return result


def add_location(parts, level, parent, commit=True):
    result = None
    if level < len(parts):
        result = get_location_id(parts[level], level, parent, commit=commit)
    return result


def add_location_path(path, commit=True):
    locid = 0
    parts = path.split('/')
    parent = 0
    for level in range(len(parts)):
        locid = add_location(parts, level, parent, commit=commit)
        parent = locid
    leaf_locations.append(locid)


def full_location(locid):
    loc = location_id_map[locid]
    parent = loc['ParentID']
    if parent > 0:
        prefix = full_location(parent)
        return prefix + '/' + loc['Name']
    else:
        return loc['Name']


def read_locations():
    global SHELF_LEVEL, sites, buildings, rooms, storages, shelves, leaf_locations, location_id_map, locations
    cur.execute('select * from StorageLocations order by LocationLevel, Name')
    rows = cur.fetchall()
    for row in rows:
        locid = row['LocationID']
        level = row['LocationLevel']
        location_id_map[locid] = row

    for row in rows:
        locid = row['LocationID']
        level = row['LocationLevel']
        path = full_location(locid)
        locations[path] = locid


def generate_locations(model):
    locs = model.all_locations()
    print(f"Generating {len(locs)} locations ...")
    count = 0
    for path in model.all_locations():
        add_location_path(path)
        count += 1
        if (count % 100) == 0:
            commit_changes()
            print(f"    {count}     \r", end='')
    print(f"    {count}")
    commit_changes()

    cur.execute("update StorageLocations set Path = GetLocationPath(LocationID)")
    commit_changes()
    print("Location generation complete")


def show_locations():
    cur.execute('select * from StorageLocations order by LocationLevel, Name')
    rows = cur.fetchall()
    for row in rows:
        print(row)


def generate_inventory(count, verbose=False):
    global chemicals, sites, buildings, rooms, storages, shelves
    global Owners, StorageGroups
    global leaf_locations
    sdslist = []
    state_list = ['solid', 'liquid', 'gas']
    unit_list = {
        'solid': ['g', 'kg', 'mg', 'ft3', 'cm3'],
        'gas': ['ft3', 'cm3'],
        'liquid': ['L', 'mL']
    }

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
        locid = random.choice(leaf_locations)

        ownername = random.choice(OwnerNames)
        owner_id = Owners[ownername]

        groupname = random.choice(GroupNames)
        group_id = StorageGroups[groupname]

        expiry = random_date(d1, d2)
        # expiry_str = expiry.strftime('%Y-%m-%d')
        chemname = chem['name']
        casnum = chem['casnum']
        # barcode = 'BC' + '{0:08d}'.format(random.randint(1, 10000000))
        barcodenum += 1
        barcode = 'BC' + '{0:08d}'.format(barcodenum)
        flags = generate_cas_flags(casnum)
        if verbose:
            print(chemname)
            print(f'    CAS:      {casnum}')
            print(f'    Location: {full_location(locid)} ({locid})')
            print(f'    Barcode:  {barcode}')

        sds = random.choice(sdslist)

        state = random.choice(state_list)
        units = random.choice(unit_list[state])
        container_size = random.choice([5, 10, 15, 20, 25, 30, 35, 40, 45, 50])
        remaining = random.randint(1, container_size)

        sql = "insert into InventoryItems (Barcode, CASNumber, ChemicalName, LocationID,  Flags, OwnerID, GroupID, DateIn, ExpirationDate, State, ContainerSize, Units, RemainingQuantity, SDS, DisposeFlag) values (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, 0)"
        rc = cur.execute(sql, (barcode, casnum, chemname, locid,
                               flags, owner_id, group_id, now, expiry,
                               state, container_size, units, remaining, sds))

        # Generate an audit record for half of the items
        if have_flag('-generateaudits'):
            if (random.random() >= 0.5):
                inventory_id = cur.lastrowid
                audit_time = now + datetime.timedelta(seconds=1)
                sql = "INSERT INTO inventoryaudits (audittime, barcode, inventoryid, locationid, user) VALUES (%s, %s, %s, %s, %s)"
                rc = cur.execute(sql, (audit_time, barcode, inventory_id, locid, owner_id))

        if (i % batch_size) == 0:
            commit_changes()
            print(f"{i}    \r", end='')
    print("End          ")
    commit_changes()


def open_db():
    global db, cur
    db = pymysql.connect(host='localhost', user='cms', passwd='cms', db='cms')
    cur = db.cursor(pymysql.cursors.DictCursor)


def generate_casdata(db):
    generate_list_of_chemicals('casdata.csv')
    # generate_casdataitems() - NO LONGER NEEDED - CASDataItems is now populated in import_ghs.py
    db.commit()


def generate_owners(db):
    global OwnerNames, Owners
    for owner in OwnerNames:
        cur.execute(f"insert into Owners (Name) values ('{owner}')")
    db.commit()
    cur.execute("select * from Owners")
    for row in cur.fetchall():
        Owners[row['Name']] = int(row['OwnerID'])


def generate_groups(db):
    global GroupNames, StorageGroups
    for group in GroupNames:
        cur.execute(f"insert into StorageGroups (Name) values ('{group}')")
    db.commit()
    cur.execute("select * from StorageGroups")
    for row in cur.fetchall():
        StorageGroups[row['Name']] = int(row['GroupID'])


def show_usage():
    print("Use: python3 generate-testdata.py <OPTIONS>")
    print("Options:")
    print("    -help           display this message")
    print("    -for <name>     use <name> for the Institution (default IRAQ)")
    print("    -items <n>      generate <n> inventory items (default 10,000)")
    print("    -sites <n>      generate <n> sites (default 10)")
    print("    -locales <n>    generate <n> locales for each site (default 2)")
    print("    -buildings <n>  generate<n> buildings / site (default 4)")
    print("    -rooms <n>      generate<n> rooms / building (default 6)")
    print("    -stores <n>     generate<n> storage areas / room (default 3)")
    print("    -shelves <n>    generate<n> shelves / storage area (default 5)")
    print("")
    print("If you are running interactively, i.e. with -i, use -nomain to prevent")
    print("execution of the main() function.")


if have_flag('-help'):
    show_usage()
    quit()


def main():
    global leaf_locations
    open_db()
    root = get_arg('-for', 'Ministry of Education')
    localecount = int(get_arg('locales', 2))
    bldgcount = int(get_arg('-buildings', 4))
    roomcount = int(get_arg('-rooms', 6))
    storecount = int(get_arg('-stores', 3))
    shelfcount = int(get_arg('-shelves', 5))
    itemcount = int(get_arg('-items', 10000))
    levelnames = get_arg(
        '-levelnames', 'University,College,Building,Room,Storage,Shelf').split(',')

    if len(levelnames) != 6:
        print("Error: -levelnames argument must be six, comma-separated names.")
        exit(0)

    t1 = Timer()

    read_pictograms()

    generate_casdata(db)
    generate_owners(db)
    generate_groups(db)
    initialize_location_schema(cur)
    read_location_types()

    loccount = count_rows('StorageLocations')
    invcount = count_rows('InventoryItems')
    if True:
        institution = root
    else:
        institution = read_scalar(
            "select SettingValue from Settings where SettingKey = 'Institution'", 'SettingValue')

        if institution == None:
            cur.execute(
                "insert into Settings (SettingKey, SettingValue) values (%s, %s)", ('Institution', root))
            commit_changes()
            institution = read_scalar(
                "select SettingValue from Settings where SettingKey = 'Institution'", 'SettingValue')

    if loccount == 0:
        model = LocationModel()
        print("Generating locations ...")
        levelcount = read_scalar("select count(*) as 'Count' from LocationLevelNames", 'Count')
        if levelcount == 0:
            cur.execute(
                f"insert into LocationLevelNames (LocationLevel, Name) values (0, 'Institution')")
            for i in range(6):
                cur.execute(
                    f"insert into LocationLevelNames (LocationLevel, Name) values ({i+1}, '{levelnames[i]}')")
        db.commit()
        model.add_all_locations(cur)
        db.commit()
        cur.execute("update StorageLocations set Path = GetLocationPath(LocationID)")
        leaf_locations = model.LeafLocations
        loccount = read_scalar(
            "select count(*) as 'Count' from StorageLocations", 'Count')
        print(f"Created {loccount} storage locations")
        print(f"Leaf locations: {len(leaf_locations)}")

    read_locations()

    if invcount == 0:
        print("Generating inventory ...")
        generate_inventory(itemcount)
        invcount = read_scalar(
            "select count(*) as 'Count' from InventoryItems", 'Count')

    print(f'Locations:   {loccount}')
    print(f'Inventory:   {invcount}')
    print(f'Institution: {institution}')

    print(f"Elapsed time: {t1.elapsed():0.2f} seconds")


if not have_arg('-nomain'):
    main()
