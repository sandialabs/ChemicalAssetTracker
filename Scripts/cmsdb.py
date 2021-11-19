import sys
import os
from utils import *
import pymysql
from attrdict import AttrDict
import getpass


class MySQL:

    def __init__(self, hostname, user, password, dbname):
        self.m_dbname = dbname
        self.m_identity = 0
        self.m_db = pymysql.connect(host=hostname,
                                    user=user,
                                    password=password,
                                    db=dbname,
                                    cursorclass=pymysql.cursors.DictCursor)

    def disconnect(self):
        if self.m_db:
            self.m_db.close()
            self.m_db = None

    def commit(self):
        self.m_db.commit()

    # use %s for placeholders in sql
    def execute_nonquery(self, sql, parameters=None, commit=True, verbose=False):
        self.m_identity = -1
        sql = sql.strip()
        if verbose:
            print(sql)
        with self.m_db.cursor() as cursor:
            rc = cursor.execute(sql, parameters)
            if commit:
                self.m_db.commit()
                if sql.lower()[0:6] == "insert":
                    self.m_identity = cursor.lastrowid
        return rc

    def queryone(self, sql, parameters=None):
        with self.m_db.cursor() as cursor:
            cursor.execute(sql, parameters)
            result = cursor.fetchone()
        return result

    def queryall(self, sql, parameters=None):
        result = []
        with self.m_db.cursor() as cursor:
            cursor.execute(sql, parameters)
            result = cursor.fetchall()
        return result

    def row_exists(self, sql, parameters=None):
        return (self.queryone(sql, parameters) != None)

    def row_count(self, sql, parameters=None):
        rows = self.queryall(sql, parameters)
        return len(rows)

    def database_exists(self, dbname):
        return (self.queryone(f"select SCHEMA_NAME from INFORMATION_SCHEMA.SCHEMATA where SCHEMA_NAME = '{dbname}'") != None)

    def table_exists(self, table_name):
        return self.row_exists(f"select TABLE_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='{self.m_dbname}' and TABLE_NAME='{table_name}'")

    def column_exists(self, table_name, column_name):
        return self.row_exists(f"select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='{self.m_dbname}' and TABLE_NAME='{table_name}' and COLUMN_NAME = '{column_name}'")
