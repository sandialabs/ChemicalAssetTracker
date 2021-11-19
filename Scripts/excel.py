import os
import datetime
import xlrd


class ExcelWorkbook:

    def __init__(self, filepath):
        if not os.path.isfile(filepath):
            raise FileNotFoundError(f'ExcelWorkbook: file not found - "{filepath}"')
        self.m_filepath = filepath
        self.m_workbook = xlrd.open_workbook(filepath)
        self.m_column_names = None
        self.m_current_sheet = None

    def select_sheet(self, sheet_number):
        self.m_current_sheet = ExcelWorksheet(self.m_workbook, sheet_number)
        return self.m_current_sheet


class ExcelWorksheet:

    def __init__(self, workbook, sheet_number):
        self.m_workbook = workbook
        self.m_sheet_number = sheet_number
        if type(sheet_number) == int:
            self.m_sheet = workbook.sheet_by_index(self.m_sheet_number)
        else:
            self.m_sheet = workbook.sheet_by_name(self.m_sheet_number)
        self.RowCount = self.m_sheet.nrows
        self.ColumnCount = self.m_sheet.ncols
        self.m_header_row = None
        self.m_header_start_column = None
        self.m_column_names = None
        self.m_row_cursor = 0

    def __getitem__(self, n):
        if n < self.RowCount:
            return self.get_row(n)
        else:
            return None

    def __iter__(self):
        self.m_row_cursor = 0
        return self

    def __next__(self):
        if self.m_row_cursor < self.RowCount:
            result = self.get_row(self.m_row_cursor)
            self.m_row_cursor += 1
            return result
        else:
            raise StopIteration

    def get_row(self, row_number):
        return ExcelRow(self, row_number)

    def cell_value(self, row_number, column_spec):
        if type(column_spec) == str:
            if column_spec in self.m_column_names:
                column_spec = self.m_column_names.index(column_spec)
            else:
                raise Exception(f'ExcelWorksheet.cell_value - column "{column_spec}" does not exist')
        return self.m_sheet.cell_value(row_number, column_spec)

    def int_value(self, row_number, column_spec):
        val = self.cell_value(row_number, column_spec)
        if type(val) == int:
            return val
        if type(val) == float:
            return int(val)
        if type(val) == str:
            return int(val)
        return None

    def float_value(self, row_number, column_spec):
        val = self.cell_value(row_number, column_spec)
        if type(val) == int:
            return float(val)
        if type(val) == float:
            val
        if type(val) == str:
            return float(val)
        return None

    def datetime_value(self, row_number, column_spec):
        numeric = self.cell_value(row_number, column_spec)
        date_tuple = xlrd.xldate_as_tuple(numeric, self.m_workbook.datemode)
        return datetime.datetime(*date_tuple)

    def datetimestring_value(self, row_number, column_spec):
        dt = self.datetime_value(row_number, column_spec)
        return dt.strftime("%Y-%m-%d %H:%M:%S")

    def datestring_value(self, row_number, column_spec):
        dt = self.datetime_value(row_number, column_spec)
        return dt.strftime("%Y-%m-%d")

    def read_column_names(self, row_number, column_count=0, start_column=0):
        names = []
        if column_count == 0:
            column_count = self.ColumnCount - start_column
        for column_number in range(start_column, start_column + column_count):
            cellvalue = self.cell_value(row_number, column_number)
            names.append(cellvalue)
        self.m_header_row = row_number
        self.m_header_start_column = start_column
        self.m_column_names = names
        return names


class ExcelRow:

    def __init__(self, worksheet, rownum):
        self.m_worksheet = worksheet
        self.m_rownum = rownum

    def __getitem__(self, col):
        return self.m_worksheet.cell_value(self.m_rownum, col)

    def get_datestring(self, col):
        return self.m_worksheet.datestring_value(self.m_rownum, col)
