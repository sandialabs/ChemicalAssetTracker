import json




def read_location_schema():
    with open('iraq_location_schema.json') as f:
        schema_text = f.read()
    schema = json.loads(schema_text);
    return (schema_text, schema)


def initialize_location_schema(dbcursor):
    print("Initializing the LocationTypes table")
    dbcursor.execute(f"delete from Settings where SettingKey = 'System.Site.LocationSchema'")
    schema_text, schema = read_location_schema()
    dbcursor.execute(
        f"insert into Settings (SettingKey, SettingValue) values ('System.Site.LocationSchema', '{schema_text}')"
    )
    processed = ['root']
    for key in schema:
        if key not in processed:
            locdef = schema[key]
            children = locdef['children']
            chstr = ','.join(children)
            sql = f"insert into LocationTypes (Name, ValidChildren) values ('{key}', '{chstr}')"
            print(sql)
            dbcursor.execute(sql)
            processed.append(key)
    return schema
