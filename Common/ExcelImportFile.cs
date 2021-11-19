using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class ExcelImportFile : ExcelFile
    {
        private int m_header_row;
        public List<string> Owners { get; private set; }
        public List<string> StorageGroups { get; private set; }
        public List<string> Locations { get; private set; }

        public ExcelImportFile(string filename, int header_row = 1) : base(filename)
        {
            m_header_row = header_row;
            GetHeaders(header_row);
            Owners = GetOwners();
            StorageGroups = GetGroups();
            Locations = GetLocations();
        }

        public List<string> GetOwners()
        {
            return GetDistinctColumnValues("Owner");
        }

        public List<string> GetGroups()
        {
            return GetDistinctColumnValues("Storage Group");
        }

        public List<string> GetLocations()
        {
            return GetDistinctColumnValues("Location");
        }

        public List<ExcelRow> GetInventoryRows()
        {
            List<ExcelRow> result = new List<ExcelRow>();
            foreach (ExcelRow row in Rows().Skip(1))
            {
                string barcode = GetRowValue(row, "Barcode");
                if (!String.IsNullOrEmpty(barcode)) result.Add(row);
            }
            return result;
        }

        public int MissingLocationCount()
        {
            return MissingValueCount("Location");
        }

        public int MissingValueCount(string column_name)
        {
            int result = 0;
            if (Headers.ContainsKey(column_name))
            {
                int column = Headers[column_name];
                foreach (var row in Rows().Skip(m_header_row))
                {
                    string value = row[column].StringValue(true);
                    if (String.IsNullOrEmpty(value)) result += 1;
                };
            }
            return result;
        }

        public string GetRowValue(ExcelRow row, string header)
        {
            int column_number = 0;
            if (Headers.TryGetValue(header, out column_number))
            {
                return row[column_number].StringValue(true);
            }
            else return null;
        }

        public double? GetRowDoubleValue(ExcelRow row, string header)
        {
            double? result = null;
            string valstr = GetRowValue(row, header);
            if (valstr  != null)
            {
                double val = 0;
                if (Double.TryParse(valstr, out val)) result = val;
            }
            return result;
        }

        public char GetRowCharValue(ExcelRow row, string header, char default_value = ' ')
        {
            string valstr = GetRowValue(row, header);
            if (!String.IsNullOrEmpty(valstr))
            {
                return valstr[0];
            }
            return default_value;
        }

        public List<string> GetDistinctColumnValues(string column_name)
        {
            if (Headers.ContainsKey(column_name))
            {
                HashSet<string> values = new HashSet<string>();
                int column = Headers[column_name];
                foreach (var row in Rows().Skip(m_header_row))
                {
                    string value = row[column].StringValue(true);
                    if (!String.IsNullOrEmpty(value)) values.Add(value);
                };
                return values.OrderBy(x => x).ToList();
            }
            return new List<string>();
        }

    }
}

