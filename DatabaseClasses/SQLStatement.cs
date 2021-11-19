using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseClasses
{
    public abstract class SQLStatement
    {
        protected string m_table_name;
        protected List<string> m_column_names = new List<string>();
        protected List<ColumnData> m_column_values = new List<ColumnData>();
        List<DatabaseQueryParameter> m_parameters = new List<DatabaseQueryParameter>();

        public SQLStatement(string table)
        {
            m_table_name = table;
        }

        public void AddField(string name, string value)
        {
            //if (!String.IsNullOrEmpty(value))
            // if you changed a field to blank it wouldn't be saved
            if (value != null)
            {
                m_column_names.Add(name);
                m_column_values.Add(new ColumnData(value));
            }
        }

        public void AddField(string name, int value)
        {
            m_column_names.Add(name);
            m_column_values.Add(new ColumnData(value));
        }

        public void AddField(string name, double value)
        {
            m_column_names.Add(name);
            m_column_values.Add(new ColumnData(value));
        }

        public void AddField(string name, DateTime value)
        {
            m_column_names.Add(name);
            m_column_values.Add(new ColumnData(value));
        }

        public void AddPlaceholder(string placeholder_name, string column_name, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                m_column_names.Add(column_name);
                m_column_values.Add(new ColumnData(placeholder_name, value));
                m_parameters.Add(new DatabaseQueryParameter(placeholder_name, value));
            }
        }

        public int Execute(BaseDatabase db)
        {
            string sql = this.Format();
            DatabaseResult rc = db.ExecuteNonQuery(sql, m_parameters.Count > 0 ? m_parameters : null, null);
            return rc.RowsRead;
        }

        public abstract string Format();
    }

    public class InsertStmt : SQLStatement
    {
        public InsertStmt(string tablename) : base(tablename)
        {
        }

        public override string Format()
        {
            string column_names = String.Join(", ", m_column_names);
            string column_values;
            List<string> vals = new List<string>();

            foreach (ColumnData col in m_column_values)
            {
                vals.Add(col.FormatForInsert());
            }
            column_values = String.Join(", ", vals);
            String result = String.Format("INSERT INTO {0} ({1}) VALUES ({2})", m_table_name, column_names, column_values);
            return result;
        }
    }

    public class UpdateStmt : SQLStatement
    {
        private string m_key_column_name;
        private int m_key_column_value;

        public UpdateStmt(string tablename, string key_column_name, int key_value)
            : base(tablename)
        {
            m_key_column_name = key_column_name;
            m_key_column_value = key_value;
        }

        public override string Format()
        {
            string column_names = String.Join(", ", m_column_names);
            string column_values;
            List<string> vals = new List<string>();

            for (int i = 0; i < m_column_names.Count; i++)
            {
                string clause = String.Format("{0} = {1}", m_column_names[i], m_column_values[i].FormatForInsert());
                vals.Add(clause);
            }
            column_values = String.Join(", ", vals);
            String result = String.Format("UPDATE {0} SET {1} WHERE {2} = {3}", m_table_name, column_values, m_key_column_name, m_key_column_value);
            return result;
        }
    }

    public class ColumnData
    {
        public enum EDataType { UNKNOWN, INTEGER, DOUBLE, STRING, TEXT, DATE, PLACEHOLDER };
        public EDataType DataType { get; set; }
        private string m_value_string;

        public ColumnData(string value)
        {
            m_value_string = value;
            DataType = EDataType.STRING;
        }

        public ColumnData(string placeholder, string value)
        {
            m_value_string = placeholder;
            DataType = EDataType.PLACEHOLDER;
        }

        public ColumnData(int value)
        {
            m_value_string = Convert.ToString(value);
            DataType = EDataType.INTEGER;
        }

        public ColumnData(double value)
        {
            m_value_string = Convert.ToString(value);
            DataType = EDataType.DOUBLE;
        }

        public ColumnData(DateTime value)
        {
            m_value_string = String.Format("{0:yyyy-MM-dd}", value);
            DataType = EDataType.DATE;
        }

        public string FormatForInsert()
        {
            string result = m_value_string;
            switch (DataType)
            {
                case EDataType.UNKNOWN:
                    break;
                case EDataType.INTEGER:
                    break;
                case EDataType.DOUBLE:
                    break;
                case EDataType.STRING:
                    result = "'" + Escape(m_value_string) + "'";
                    break;
                case EDataType.TEXT:
                    result = "'" + Escape(m_value_string) + "'";
                    break;
                case EDataType.DATE:
                    result = "'" + Escape(m_value_string) + "'";
                    break;
                case EDataType.PLACEHOLDER:
                    result = ":" + m_value_string;
                    break;
                default:
                    break;
            }
            return result;
        }

        public static string Escape(string value)
        {
            return value.Replace("'", "''");
        }
    }

}
