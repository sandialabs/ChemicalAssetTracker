using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//#################################################################
//
// Use
//
// var cvsfile = new CVSFile(filename, true);
// csvfile.Foreach((int rownum, CVSRow rowdata) => { 
//     // process row 
// });
// 
//
//#################################################################

namespace Common
{
    public class CSVLine
    {
        private string[] m_values;

        public string[] Values { get { return m_values; } }
        public int Count { get { return m_values.Length; } }
        public string this[int i]
        {
            get
            {
                if (i >= m_values.Length) throw new IndexOutOfRangeException("CSVLine item index is out of range");
                return m_values[i];
            }
        }

        public CSVLine(string text)
        {
            List<string> items = new List<string>();
            int textlen = text.Length;
            Tuple<string, int> item = GetNextField(text, 0);
            while (item != null)
            {
                items.Add(item.Item1);
                int pos = item.Item2;
                item = GetNextField(text, item.Item2 + 1);
            }
            m_values = items.ToArray();
        }

        public int FindItem(string value)
        {
            for (int i = 0; i < m_values.Length; i++)
            {
                if (m_values[i] == value) return i;
            }
            return -1;
        }


        public bool ContainsItem(string value)
        {
            return (FindItem(value) >= 0);
        }

        public Dictionary<string, string> GetValues(CSVLine header)
        {
            Dictionary<string, string> result = null;
            if (m_values.Length >= header.Count)
            {
                result = new Dictionary<string, string>();
                for (int i = 0; i < header.Count; i++) result.Add(header[i], m_values[i]);
            }
            return result;
        }

        private Tuple<string, int> GetNextField(string text, int offset)
        {
            Tuple<string, int> result = null;
            StringBuilder builder = new StringBuilder(256);
            int textlen = text.Length;
            if (offset < textlen)
            {
                if (text[offset] == '"')
                {
                    // parse quoted field
                    int i = offset + 1;
                    char ch;
                    while (i < textlen && (ch = text[i]) != '"')
                    {
                        i++;
                        if (ch == '\\' && i < textlen && text[i] == '"')
                        {
                            // escaped embedded quote
                            builder.Append('"');
                            i++;
                        }
                        else builder.Append(ch);
                    }
                    if (i < textlen) i++;  // skip trailing quote
                    // find the comma
                    while (i < textlen && text[i] != ',') i++;
                    result = new Tuple<string, int>(builder.ToString(), i);
                }
                else
                {
                    int pos = text.IndexOf(',', offset);
                    if (pos < 0)
                    {
                        // last field on this line
                        result = new Tuple<string, int>(text.Substring(offset).Trim(), textlen);
                    }
                    else
                    {
                        result = new Tuple<string, int>(text.Substring(offset, pos - offset).Trim(), pos);
                    }
                }
            }
            return result;
        }
    }

    public class CSVFile
    {
        private CSVLine m_header_line;
        private Dictionary<string, int> m_column_map;

        public string[] ColumnNames { get; set; }
        public List<CSVLine> Lines { get; set; }
        public string ErrorMessage { get; set; }
        public delegate bool RowDelegate(int rownum, CSVRow rowdata);


        private void ParseLines(string[] lines, bool has_header)
        {
            this.Lines = new List<CSVLine>();
            foreach (string text in lines)
            {
                CSVLine line = new CSVLine(text);
                if (has_header && m_header_line == null)
                {
                    m_header_line = line;
                    m_column_map = new Dictionary<string, int>();
                    ColumnNames = line.Values;
                }
                else this.Lines.Add(line);
            }
        }

        public CSVFile(string[] lines, bool has_header)
        {
            ParseLines(lines, has_header);
        }

        public CSVFile(string contents, bool has_header)
        {
            if (contents.Length > 0 && contents[0] > 127) contents = contents.Substring(1);
            string[] lines = contents.Split('\n');
            ParseLines(lines, has_header);
        }


        public static CSVFile FromFile(string filename, bool has_header)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);
            return new CSVFile(lines, has_header);
        }


        public void Foreach(int start, int count, RowDelegate callback)
        {
            int linecount = Lines.Count;
            int end = start + count;
            if (end > linecount) end = linecount;

            bool keep_reading = true;
            CSVRow rowdata = new CSVRow(ColumnNames);
            for (int i = start; keep_reading &&  i < end; i++ )
            {
                rowdata.SetValues(Lines[i].Values);
                keep_reading = callback(i, rowdata);
            }
        }
        public void Foreach(RowDelegate callback)
        {
            Foreach(0, Lines.Count, callback);
        }

        public bool HasHeader(string name)
        {
            bool result = false;
            ErrorMessage = null;
            if (m_header_line != null)
            {
                result = m_header_line.ContainsItem(name);
                if (result == false) ErrorMessage = $"Header not found: {name}";
            }
            else ErrorMessage = ("CSV has no header line.");
            return result;
        }

        public CSVData ExtractItems(int start_row, int count, params string[] column_names)
        {
            CSVData result = null;
            for (int i = 0; i < column_names.Length; i++)
            {
                if (!this.HasHeader(column_names[i])) throw (new Exception("CVSData.ExtractItems - invalid column name " + column_names[i]));
            }
            if (Lines.Count > start_row)
            {
                result = new CSVData(column_names);
                int end_row = start_row + count;
                if (end_row > Lines.Count) end_row = Lines.Count;
                for (int i = start_row; i < end_row; i++)
                {
                    var values = Lines[i].GetValues(m_header_line);
                    if (values != null)
                    {
                        List<string> row = new List<string>();
                        foreach (string col in column_names)
                        {
                            row.Add(values[col]);
                        }
                        result.Add(row);
                    }
                }
            }
            else throw(new Exception("CVSData.ExtractLines - not enough lines"));
            return result;
        }
    }

    public class CSVRow
    {
        public Dictionary<string, int> ColumnMap { get; set; }
        public string[] ColumnValues { get; set; }

        public string this[string column_name]
        {
            get
            {
                int index = ColumnMap[column_name];
                if (ColumnValues.Length > index) return ColumnValues[index];
                else return null;
            }
        }

        public CSVRow(string[] column_names)
        {
            ColumnMap = new Dictionary<string, int>();
            for (int i = 0; i < column_names.Length; i++) ColumnMap[column_names[i]] = i;
            ColumnValues = null;
        }


        public void SetValues(string[] values)
        {
            ColumnValues = values;
        }
    }

    public class CSVData
    {
        public string[] ColumnNames { get; private set; }
        public Dictionary<string, int> ColumnMap { get; private set; }
        public List<List<string>> ColumnData { get; private set; }
        public int RowCount { get { return ColumnData.Count; } }
        public int ColumnCount { get { return ColumnNames.Length; } }

        public delegate bool MapDelegate(int row, List<string> linedata);

        public List<string> this[int i]
        {
            get
            {
                if (i >= ColumnData.Count) throw new IndexOutOfRangeException("CVSData item index is out of range");
                return ColumnData[i];
            }
        }


        public CSVData(string[] column_names)
        {
            ColumnNames = column_names;
            ColumnMap = new Dictionary<string, int>();
            for (int i = 0; i < column_names.Length; i++) ColumnMap[column_names[i]] = i;
            ColumnData = new List<List<string>>();
        }

        public void Foreach(MapDelegate callback, int start_index = 0, int end_index = 999999999)
        {
            for (int i = start_index;  i < ColumnData.Count  &&  i < end_index;  i++)
            {
                if (!callback(i, ColumnData[i])) break;
            }
        }

        public void Add(List<string> row)
        {
            ColumnData.Add(row);
        }

        void RenameColumn(string from, string to)
        {
            if (!ColumnMap.ContainsKey(from)) throw (new Exception("RenameColumn - invalide column name " + from));
            int col = ColumnMap[from];
            ColumnNames[col] = to;
            ColumnMap.Remove(from);
            ColumnMap.Add(to, col);
        }
    }
}
