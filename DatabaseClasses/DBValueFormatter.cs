using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseClasses
{
    public class DBValueFormatter
    {
        private StringBuilder m_names = new StringBuilder();
        private StringBuilder m_values = new StringBuilder();

        public String Names { get { return m_names.ToString(); } }
        public String Values { get { return m_values.ToString(); } }
        public List<DatabaseQueryParameter> Parameters { get; private set; } = new List<DatabaseQueryParameter>();

        public String InsertSQL(String table)
        {
            return String.Format("INSERT INTO {0} ({1}) VALUES ({2})", table, Names, Values);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format a integer value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(int value, String field_name)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            m_values.Append(value);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format an unsigned integer value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(uint value, String field_name)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            m_values.Append(value);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format a string value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(String value, String field_name)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            m_values.Append("@" + field_name);
            DatabaseQueryParameter parameter = new DatabaseQueryParameter(field_name, value);
            Parameters.Add(parameter);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format a DateTime value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        /// <param name="gmt">true if the DateTime should be as Universal/GMT time</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(DateTime value, String field_name, bool gmt)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            if (value.Year >= 1900)
            {
                string strval = String.Format("{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}.{6:000}", value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond);
                m_values.Append("'" + strval + "'");
            }
            else
            {
                m_values.Append("'1970-01-01 00:00:00'");
            }
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format a float value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(float value, String field_name)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            m_values.Append(String.Format("{0:0.0000000}", value));
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Insert
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Format a float value for an SQL INSERT statement
        /// </summary>
        ///
        /// <param name="value">the value to format</param>
        /// <param name="field_name">the database column that holds this value</param>
        ///
        ///----------------------------------------------------------------
        public void Insert(double value, String field_name)
        {
            if (m_names.Length > 0)
            {
                m_names.Append(',');
                m_values.Append(',');
            }
            m_names.Append(field_name);
            m_values.Append(String.Format("{0}", value));
        }
    }
}
