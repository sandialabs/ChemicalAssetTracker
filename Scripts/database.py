######################################################################
#
# Database classes
#
# This file contains classes for working with databases in python.
# These are just wrappers on top of pyodbc that abstracts away from
# what brand of database you are accessing and provides some convenience
# functions.
#
# Dependencies: pyodbc and dotmap
#     If you are going to access SQL Server you will need to add their
#     python drivers. (https://docs.microsoft.com/en-us/sql/connect/python/pyodbc/python-sql-driver-pyodbc?view=sql-server-2017)
#
# Use
#    from database import *
#
#    db = SQLServer().connect('(local)', 'mydbname')
#    for row in db.query("select * from mytable"):
#        print(row['Description'])
#            (or if you set the UseDotMap option)
#        print(row.Description)
#    db.execute_nonquery("drop table mytable")
#    db.execute_nonquery("insert into mytable (x) values (?)",
#                          parameters=[xvalue], commit=False)
#
# Database
#    This is the base class for the various database engines.  It
#    implements the following methods.
#
#    connect: open a connection to a database
#    query: run a query - returns a DBQuery instance (see below)
#    execute_nonquery: execute some SQL that is not a query
#    execute_query: executes a query and returns a Cursor object
#       this is what DBQuery calls in its constructor
#    show: runs a query and shows the result
#    commit: commits any unsaved changes
#        execute_nonquery commits by default, but you can disable that
#    disconnect: shuts down the database connection
#
# DBQuery
#    This is a convenience class for managing query results.  The
#    constructor takes some SQL and an open Database instance.  The
#    query is executed in the constructor.  You can then iterate on
#    the DBQuery object to get rows.
#
#    DotMap:  boolean class variable that controls whether row
#               columns are attributes (e.g. row.Name) or as plain
#               dictionaries.
#
#    use_dotmap: turns on DotMap for just the current query.
#               You have to call this before retrieving any rows.
#    next: get the next row of data
#    fetch: retrieve the next n rows
#    fetchall: retrieve all remaining rows
#    close: release the Cursor object
#    elapsed: return the elapsed time since the query was started
#    report: prints out how many rows were read and how long it took
#    show_rows: print out the next n rows
#    show: print out all remaining rows
#
# SQLServer - Microsoft SQL Server interface
#
# MDBFile - Microsoft Access MDB file interface
#
######################################################################

import pyodbc
from time import time


class DBQuery:
    UseDotMap = True

    def __init__(self, sql, db):
        self.m_use_dotmap = DBQuery.UseDotMap
        self.m_start_time = time()
        self.m_rowcount = 0
        self.m_cursor = db.execute_query(sql)
        self.m_columns = self.m_cursor.description
        self.m_column_names = [c[0] for c in self.m_columns]
        # print(self.m_column_names)

    def use_dotmap(self):
        self.m_use_dotmap = True
        return self

    def __iter__(self):
        return self

    def __next__(self):
        row = self.next()
        if row:
            return row
        else:
            raise StopIteration

    def next(self):
        row = self.m_cursor.fetchone()
        if row:
            self.m_rowcount += 1
            if self.m_use_dotmap:
                return DotMap(zip(self.m_column_names, row))
            else:
                return dict(zip(self.m_column_names, row))
        else:
            return None

    def fetchall(self):
        return self.fetch(99999999)

    def fetch(self, n):
        result = []
        count = 0
        row = self.next()
        while row and count < n:
            count += 1
            result.append(row)
            row = self.next()
        return result

    def close(self):
        if self.m_cursor:
            self.m_cursor.close()
            self.m_cursor = None

    def elapsed(self):
        return time() - self.m_start_time

    def report(self):
        print(f'Read {self.m_rowcount} rows in {self.elapsed()} seconds')

    def show_rows(self, rows, n=0):
        if n > 0:
            rows = rows[:n]
        colnames = [col[0] for col in self.m_columns]
        dividers = []
        fmts = []
        for ix, col in enumerate(colnames):
            maxlen = 0
            for row in rows:
                val = row[col]
                if val:
                    val = str(val)
                    maxlen = max(maxlen, len(val))
            maxlen = max(maxlen, len(col))
            fmts.append('{' + "{0}:<{1}".format(ix, maxlen) + '}')
            dividers.append('-'*maxlen)
        fmtstr = "  ".join(fmts)
        print(fmtstr.format(*colnames))
        print(fmtstr.format(*dividers))
        for row in rows:
            values = []
            for col in colnames:
                value = row[col]
                if value:
                    values.append(value)
                else:
                    values.append('null')
            print(fmtstr.format(*values))

    def show(self, rowcount=9999):
        rows = self.fetch(rowcount)
        self.show_rows(rows)


class Database(object):

    def __init__(self, driver):
        #print("In Database constructor: " + driver)
        self.m_drivername = driver
        self.m_conn = None

    def connect(self, host, db, user=None, pswd=None):
        constr = f'DRIVER={self.m_drivername};SERVER={host};DATABASE={db}'
        if user and pswd:
            constr += f';UID={user};PWD={pswd}'
        else:
            print("Using trusted connection")
            constr += ';Trusted_Connection=yes;'
        self.m_conn = pyodbc.connect(constr)
        return self

    def query(self, sql):
        return DBQuery(sql, self)

    def queryall(self, sql):
        query = DBQuery(sql, self)
        result = query.fetchall()
        query.close()
        return result

    def queryone(self, sql):
        query = DBQuery(sql, self)
        result = query.next()
        query.close()
        return result

    def execute_nonquery(self, sql, parameters=None, commit=True):
        if parameters:
            result = self.m_conn.execute(sql, *parameters)
        else:
            result = self.m_conn.execute(sql)
        if commit:
            self.commit()
        return result

    def execute_query(self, sql, parameters=None, commit=True):
        return self.m_conn.execute(sql)

    def run_query(self, sql, maxrows=9999, verbose=True):
        if verbose:
            print('-'*72)
            print('- ' + sql)
            print('-'*72)
            print(' ')
        q = self.query(sql)
        q.show(maxrows)

    def row_exists(self, sql):
        query = self.query(sql)
        result = query.next()
        query.close()
        return result

    def table_exists(self, tablename):
        print("Not supported")

    def show_columns(self, tablename):
        print("Not supported")

    def show_tables(self):
        print("Not supported")

    def show(self, sql, rows=9999):
        query = self.execute_query(sql)
        query.show(rows)
        query.close()

    def commit(self):
        self.m_conn.commit()
        return self

    def disconnect(self):
        self.m_conn.close()
        self.m_conn = None
        return self


class SQLServer(Database):
    def __init__(self):
        #print("In SQLServer constructor")
        super().__init__('ODBC Driver 17 for SQL Server')

    def table_exists(self, tablename):
        return self.row_exists(f"select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{tablename}'")

    def query_columns(self, tablename):
        sql = f'''
            select COLUMN_NAME as 'Name', 
                   DATA_TYPE as 'Type', 
                   CHARACTER_MAXIMUM_LENGTH as 'Length', 
                   COLUMN_DEFAULT as 'Default'
            from INFORMATION_SCHEMA.COLUMNS 
            where TABLE_NAME = '{tablename}'
        '''
        # print('#'*72)
        # print(sql)
        # print('#'*72)
        return self.query(sql)

    def get_columns(self, tablename):
        return self.query_columns(tablename).fetchall()

    def show_columns(self, tablename):
        self.query_columns(tablename).show()

    def show_tables(self):
        self.run_query(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'",
            verbose=False)


class MDBFile(Database):
    def __init__(self):
        #print("In MDBFile constructor")
        super().__init__('Microsoft Access Driver (*.mdb, *.accdb)')

    def connect(self, filepath):
        constr = f'DRIVER={self.m_drivername};DBQ={filepath};'
        self.m_conn = pyodbc.connect(constr)
        return self


def dbtest():
    print("In dbtest()")
    db = SQLServer().connect('(local)', 'AIS2018')
    if db.row_exists("select * from Ports where PortID = 0"):
        print("row_exists test failed")
    else:
        print("row_exists test succeeded")
    for tabname in ['AIS2018', 'Ports', 'Foobar']:
        if db.table_exists(tabname):
            print(f"Table {tabname} exists")
        else:
            print(f"Table {tabname} does not exist")
