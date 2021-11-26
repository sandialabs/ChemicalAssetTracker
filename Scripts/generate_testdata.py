import sys
import csv
import random
import pymysql
import time
import datetime
from utils import *
from location_model import *
from iraq_setup import *


def random_location(location_types, parent_name, parent_path):
    match = [x for x in location_types if x['Name'] == parent_name]
    if len(match) > 0:
        parent = match[0]
        if parent['ValidChildren']:
            children = parent['ValidChildren'].split(',')
            child = random.choice(children)
            return random_location(location_types, child, parent_path + '/' + child)
        else:
            return parent_path
    else:
        raise Exception(f"Unknown location type: {parent_name}")

def create_location(name, parent_id, location_type, is_leaf, db):
    loc = db.queryone(f"select * from StorageLocations where Name = %s and ParentId = %s", (name, parent_id))
    if loc:
        print(f"create_location - found location {name} with ParentID = {parent_id}")
        return loc['LocationID']
    else:
        parent = db.queryone(f"select * from StorageLocations LocationID = %s", (name, parent_id))
        if parent:
            is_leaf = len(location_type['ValidChildren']) == 0
            sql = """
                insert into StorageLocations 
                (Name, ParentID, LocationTypeID, IsLeaf, LocationLevel)
                values (%s, %s, %s, %s, %s)
            """;
            db.execute_nonquery(sql, [name, parent_id, location_type, is_leaf, parent.LocationLevel + 1])


def generate_testdata():
    return

if __name__ == "__main__":
    generate_testdata()
