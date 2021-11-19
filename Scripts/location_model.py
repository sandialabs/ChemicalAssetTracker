import random

random.seed()


class LocationModel:

    def __init__(self):
        self.m_location_types = {}
        self.m_new_location_count = 0
        self.AllLocations = []      # every locationid we generate
        self.LeafLocations = []     # every leaf LocationID we generate

        level_0_names = ['Ministry:ministry']
        level_1_names = [
            "Al-Mustansiriya University:university",
            "Al-Nahrain University:university",
            "Iraqi University:university",
            "University of Baghdad:university",
            "University of Technology:university",
            "University of Information Technology and Communications:university",
            "Uruk University:university",
            "Al-Rafidain University:university",
            "Al- Nisour University:university",
            "Al Muthana University:university",
        ]
        level_2_names = [
            "Building 1:building",
            "Institute 1:institution",
            #"Institute for Chemistry:institution",
            #"Arts and Sciences College:college",
            #"School of Medicine:college",
            #"College of Pharmacy:college"
        ]
        level_3_names = [
            "Chemistry Department:department",
            "Physics Department:department",
            "Local Branch:branch",
            "Remote Branch:branch",
            "Biology Department:department"
        ]
        level_4_names = []
        self.generate_names("Lab", 2, 'lab', level_4_names)
        self.generate_names("Store", 2, 'store', level_4_names)
        level_5_names = []
        self.generate_names("Shelf", 2, 'shelf', level_5_names)
        self.generate_names("Cabinet", 2, 'cabinet', level_5_names)

        self.m_names = [level_0_names, level_1_names, level_2_names, level_3_names, level_4_names, level_5_names]

    # names will have ":loccation_type" appended
    def generate_names(eelf, prefix, count, loctype, result):
        for i in range(count):
            name = f"{prefix} {i:02d}:{loctype}"
            result.append(name)

    def random_location(self):
        parts = [self.m_names[0][0]]
        for i in range(1, 6):
            parts.append(random.choice(self.m_names[i]))
        return '/'.join(parts)

    def all_locations(self):
        result = []
        for l0 in self.m_names[0]:
            for l1 in self.m_names[1]:
                for l2 in self.m_names[2]:
                    for l3 in self.m_names[3]:
                        for l4 in self.m_names[4]:
                            for l5 in self.m_names[5]:
                                result.append(f"{l0}/{l1}/{l2}/{l3}/{l4}/{l5}")
        return result

    def add_all_locations(self, dbcursor):
        result = []
        self.m_new_location_count = 0
        self.read_location_types(dbcursor)
        for l0 in self.m_names[0]:
            l0loc = self.add_location(l0, 0, 0, dbcursor)
            for l1 in self.m_names[1]:
                l1loc = self.add_location(l1, l0loc, 1, dbcursor)
                for l2 in self.m_names[2]:
                    l2loc = self.add_location(l2, l1loc, 2, dbcursor)
                    for l3 in self.m_names[3]:
                        #print("Adding " + l3)
                        l3loc = self.add_location(l3, l2loc, 3, dbcursor)
                        for l4 in self.m_names[4]:
                            l4loc = self.add_location(l4, l3loc, 4, dbcursor)
                            for l5 in self.m_names[5]:
                                l5loc = self.add_location(l5, l4loc, 5, dbcursor)
                                self.LeafLocations.append(l5loc)
                                if (self.m_new_location_count % 1000) == 0:
                                    print(f"{self.m_new_location_count}      \r", end='')
        print(f"{self.m_new_location_count} locations added")
        return result

    def add_location(self, locname, parent, level, dbcursor):
        (name, loctype) = locname.split(':')
        location_type_id = self.m_location_types[loctype]
        is_leaf = (loctype == 'shelf' or loctype == 'cabinet')

        rowcount = dbcursor.execute(
            "insert into StorageLocations (Name, ParentID, LocationLevel, LocationTypeID, IsLeaf) values (%s, %s, %s, %s, %s)", (name, parent, level, location_type_id, is_leaf))
        if rowcount != 1:
            print("Insert failed")
            exit(1)
        else:
            location_id = dbcursor.lastrowid
            # if self.m_new_location_count < 20:
            #    print(f"Added location {locname} with parent {parent}: new LocationID is {location_id}")
            self.AllLocations.append(location_id)
            self.m_new_location_count += 1
            return location_id

    def read_location_types(self, dbcursor):
        print("Reading LocationTypes")
        result = {}
        dbcursor.execute('select * from LocationTypes')
        rows = dbcursor.fetchall()
        for row in rows:
            type_id = row['LocationTypeID']
            name = row['Name']
            result[name] = type_id
        print(f"    read {len(result)} location types")
        if len(result) == 0:
            print("ERROR - no location types in database")
            exit(1)
        self.m_location_types = result
