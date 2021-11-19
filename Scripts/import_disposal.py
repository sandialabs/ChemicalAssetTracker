import re
from excel import *
from utils import *
from cmsdb import *

SCHEDULE_COLUMN = 1
CATEGORY_COLUMN = 2
CAS_COLUMN = 3
CHEMNAME_COLUMN = 4
TREATMENT_COLUMN = 5
TECHNIQUES_COLUMN = 6
PRODUCTS_COLUMN = 7
DISPOSAL_COLUMN = 8


class DisposalProcedure:

    def __init__(self, rownum, row):
        self.m_cas_regex = re.compile('(\\d+-\\d+-\\d+)')
        self.m_rownum = rownum
        self.m_schedule = row[SCHEDULE_COLUMN]
        self.m_category = row[CATEGORY_COLUMN]
        self.m_casnums = self.get_cas_numbers(row)
        self.m_chems = row[CHEMNAME_COLUMN]
        self.m_treatment = row[TREATMENT_COLUMN]
        self.m_techniques = row[TECHNIQUES_COLUMN]
        self.m_products = row[PRODUCTS_COLUMN]
        self.m_disposal = row[DISPOSAL_COLUMN]

    def dump(self, fp):
        if fp:
            fp.write("------------------------------------------\n")
            fp.write(f"CAS Numbers: {self.m_casnums}\n")
            fp.write(f"    Row#:        {self.m_rownum}\n")
            fp.write(f"    Schedule:    {self.m_schedule}\n")
            fp.write(f"    Category     {self.m_category}\n")
            fp.write(f"    Chemicals:   {self.m_chems}\n")
            fp.write(f"    Treatment:   {self.m_treatment}\n")
            fp.write(f"    Techniques:  {self.m_techniques}\n")
            fp.write(f"    Products:    {self.m_products}\n")
            fp.write(f"    Disposal:    {self.m_disposal}\n")
            fp.write("\n")
        else:
            print("------------------------------------------")
            print(f"CAS Numbers: {self.m_casnums}")
            print(f"    Row#:        {self.m_rownum}")
            print(f"    Schedule:    {self.m_schedule}")
            print(f"    Category     {self.m_category}")
            print(f"    Chemicals:   {self.m_chems}")
            print(f"    Treatment:   {self.m_treatment}")
            print(f"    Techniques:  {self.m_techniques}")
            print(f"    Products:    {self.m_products}")
            print(f"    Disposal:    {self.m_disposal}")
            print("")

    def store(self, db):
        sql = """
            insert into DisposalProcedures 
            (Schedule, Category, ChemicalName, Treatment, Techniques, Products, WasteDisposal)
            values (%s, %s, %s, %s, %s, %s, %s)
        """
        values = [self.m_schedule, self.m_category, self.m_chems, self.m_treatment, self.m_techniques, self.m_products, self.m_disposal]
        db.execute_nonquery(sql, values)
        id = db.m_identity
        #print(f"IDENTITY is {id}")
        sql = "insert into CASDisposalProcedures (CASNumber, DisposalProcedureID) values (%s, %s)"
        for casnum in self.m_casnums:
            db.execute_nonquery(sql, [casnum, id])

    def get_cas_numbers(self, row):
        return self.m_cas_regex.findall(row[CAS_COLUMN])

    def get_multiline_values(self, row, col, regex=None):
        rowval = row[col]
        result = []
        if regex:
            for val in rowval.split('\n'):
                if val:
                    m = regex.match(val)
                    if m:
                        result.append(m[1])
            return result
        else:
            for val in rowval.split('\n'):
                if val:
                    result.append(val)
            return result


def create_disposal_tables(db):
    sql1 = "drop table if exists DisposalProcedures"
    sql2 = "drop table if exists CASDisposalProcedures"
    sql3 = """
        create table DisposalProcedures(DisposalProcedureID int auto_increment primary key, 
                                 Schedule varchar(8) not null, 
                                 Category varchar(32) not null,
                                 ChemicalName TEXT not null,
                                 Treatment varchar(64) not null,
                                 Techniques TEXT not null,
                                 Products TEXT not null,
                                 WasteDisposal TEXT not null)
    """
    sql4 = """
        create table CASDisposalProcedures(CASDisposalProcedureID int auto_increment primary key, 
                                 CASNumber varchar(16) not null, 
                                 DisposalProcedureID int not null)
    """
    sql5 = "create index IX_CASDisposalProcedures_CASNumber on CASDisposalProcedures(CASNumber)"
    for sql in [sql1, sql2, sql3, sql4, sql5]:
        print(sql)
        db.execute_nonquery(sql)


def parse_disposal_spreadsheet(ws, db):
    result = []

    last_dp = None
    fp = open("./Data/disposal.txt", "w", encoding='utf-8')
    for ix, row in enumerate(ws):
        if ix > 0:
            dp = DisposalProcedure(ix+1, row)
            result.append(dp)
            if last_dp:
                if dp.m_category == '':
                    dp.m_category = last_dp.m_category
                if dp.m_schedule == '':
                    dp.m_schedule = last_dp.m_schedule
                if len(dp.m_casnums) == 0:
                    dp.m_casnums = last_dp.m_casnums
                    if dp.m_chems == '':
                        dp.m_chems = last_dp.m_chems
                    if dp.m_treatment == '':
                        dp.m_treatment = last_dp.m_treatment
                    if dp.m_disposal == '':
                        dp.m_disposal = last_dp.m_disposal
            last_dp = dp
            dp.dump(fp)
            dp.store(db)
    fp.close()
    return result


wb = ExcelWorkbook('./Data/disposal.xlsx')
ws = wb.select_sheet(0)

user, pswd = get_mysql_info()
hostname = get_arg('-h', 'localhost')
if user and pswd:
    db = MySQL(hostname, user, pswd, 'cms')
    db.execute_nonquery("delete from CASDisposalProcedures")
    db.execute_nonquery("delete from DisposalProcedures")
    parse_disposal_spreadsheet(ws, db)
