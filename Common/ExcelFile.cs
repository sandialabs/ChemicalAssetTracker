using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class ExcelFile
    {
        protected XLWorkbook m_excelfile;

        protected Dictionary<string, int> m_headers = null;

        public IXLWorksheet Worksheet { get; private set; }

        public int RowCount { get { return Worksheet.RowCount();  } }

        public Dictionary<string, int> Headers
        {
            get
            {
                if (m_headers == null) m_headers = GetHeaders();
                return m_headers;
            }
        }

        public ExcelRow this[int n] { get { return NthRow(n); } }

        public ExcelFile(string filename)
        {
            m_excelfile = new XLWorkbook(filename);
            Worksheet = m_excelfile.Worksheets.First();
        }

        public List<ExcelRow> Rows()
        {
            List<ExcelRow> result = new List<ExcelRow>();
            foreach (var row in Worksheet.Rows())
            {
                result.Add(new ExcelRow(row));
            }
            return result;
        }

        public List<ExcelRow> RowsUsed()
        {
            List<ExcelRow> result = new List<ExcelRow>();
            foreach (var row in Worksheet.RowsUsed())
            {
                result.Add(new ExcelRow(row));
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       NthRow
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the n'th row of the worksheet - counting from 1
        /// </summary>
        ///
        /// <param name="n">row number, counting from 0</param>
        /// <returns>an ExcelRow or null</returns>
        ///
        ///----------------------------------------------------------------
        public ExcelRow NthRow(int n)
        {
            if (n < 1) throw new Exception($"Worksheet.NthRow - row number must be between 1 and {RowCount}");
            if (n >= Worksheet.RowCount()) return null;
            if (n == 1) return new ExcelRow(Worksheet.FirstRow());
            else return new ExcelRow(Worksheet.Rows(n-1, n-1).First());
        }

        public Dictionary<string, int> GetHeaders(int header_row = 1)
        {
            ExcelRow row = NthRow(header_row);
            if (row == null) throw new Exception($"ExcelFile.GetHeaders - row index out of range (header_row)");
            Dictionary<string, int> result = new Dictionary<string, int>();
            int ix = 1;
            foreach (var cell in row.Cells())
            {
                string value = cell.StringValue();
                if (!String.IsNullOrEmpty(value)) result[value] = ix;
                ix += 1;
            }
            m_headers = result;
            return result;
        }

        public ExcelCell GetCell(int rownum, string header_value)
        {
            if (m_headers == null) GetHeaders();
            ExcelRow row = NthRow(rownum);
            if (row != null)
            {
                int colnum = 0;
                if (Headers.TryGetValue(header_value, out colnum))
                {
                    return row[colnum];
                }
                else throw new Exception($"ExcellFile.GetCell - invalid header value: \"{header_value}\"");
            }
            else throw new Exception($"ExcellFile.GetCell - row number out of range: {rownum} - sheet has {RowCount} rows.");
        }
    }

    public class ExcelRow
    {
        protected IXLRow m_row;
        public ExcelCell this[int n]
        {
            get
            {
                if (n < 1) throw new Exception($"Worksheet.indexer - row number must be between 1 and {m_row.CellCount()}");
                if (n > m_row.CellCount()) return null;
                else return new ExcelCell(m_row.Cell(n));
            }
        }


        public ExcelRow(IXLRow row)
        {
            m_row = row;
        }

        public List<ExcelCell> Cells()
        {
            List<ExcelCell> result = new List<ExcelCell>();
            foreach (IXLCell cell in m_row.Cells()) result.Add(new ExcelCell(cell));
            return result;
        }

        public List<ExcelCell> CellsUsed()
        {
            List<ExcelCell> result = new List<ExcelCell>();
            foreach (IXLCell cell in m_row.CellsUsed()) result.Add(new ExcelCell(cell));
            return result;
        }

    }

    public class ExcelCell
    {
        private IXLCell m_cell;

        public ExcelCell(IXLCell cell)
        {
            m_cell = cell;
        }

        public (int, int) Address()
        {
            IXLAddress addr = m_cell.Address;
            return (addr.RowNumber, addr.ColumnNumber);
        }

        public string Name()
        {
            IXLAddress addr = m_cell.Address;
            return (addr.ColumnLetter);
        }

        public string StringValue(bool trim = false)
        {
            string result = m_cell.GetString();
            if (trim) return result.Trim();
            else return result;
        }
    }
}
