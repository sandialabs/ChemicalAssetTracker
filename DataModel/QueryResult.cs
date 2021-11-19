using System;
using System.Collections.Generic;
using System.Text;

namespace DataModel
{
    public enum EQueryDataType { UNDEFINED, STRING, INT, FLOAT, DATE }

    public class QueryResult
    {
        public List<QueryResultColumn> Columns { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }

        public QueryResult()
        {
            Columns = new List<QueryResultColumn>();
            Rows = new List<Dictionary<string, object>>();
        }

        public QueryResult AddColumn(string name, EQueryDataType datatype, double width)
        {
            Columns.Add(new QueryResultColumn(name, datatype, width));
            return this;
        }

        public QueryResult AddRow(params ColumnData[] columndata)
        {
            if (columndata.Length != Columns.Count) throw new Exception("QueryResult.AddRow - wrong number of data items.");
            Dictionary<string, object> values = new Dictionary<string, object>();
            for (int i = 0; i < Columns.Count; i++)
            {
                values.Add(Columns[i].ColumnName, columndata[i].obj);
            }
            Rows.Add(values);
            return this;
        }
    }


    public class QueryResultColumn
    {
        public string ColumnName { get; set; }
        public EQueryDataType DataType { get; set; }
        public double ColumnWidth { get; set; }

        public QueryResultColumn(string name, EQueryDataType datatype, double width)
        {
            ColumnName = name;
            DataType = datatype;
            ColumnWidth = width;
        }

    }

    public class QueryResultRow
    {
        public Dictionary<string, object> Values { get; private set; }

        public QueryResultRow(List<QueryResultColumn> column_defs, ColumnData[] columndata)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            for (int i = 0;  i < column_defs.Count;  i++)
            {
                values.Add(column_defs[i].ColumnName, columndata[i].obj);
            }
            Values = values;
        }

    }

    public class ColumnData
    {
        public object obj { get; set; }
        public string Value { get; set; }
        public bool IsNull { get; set; }

        public ColumnData()
        {
            IsNull = true;
        }

        public ColumnData(string value)
        {
            obj = value;
            Value = $"{value}";
            IsNull = false;
        }

        public ColumnData(int? value)
        {
            if (value.HasValue)
            {
                obj = value.Value;
                Value = $"{value.Value}";
                IsNull = false;
            }
            else IsNull = true;
        }

        public ColumnData(double? value)
        {
            if (value.HasValue)
            {
                obj = value.Value;
                Value = $"{value.Value}";
                IsNull = false;
            }
            else IsNull = true;
        }

        public ColumnData(DateTime? value)
        {
            if (value.HasValue)
            {
                Value = $"{value.Value}";
                obj = Value;
                IsNull = false;
            }
            else IsNull = true;
        }


    }
}
