using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace DatabaseClasses
{
    public class DBHelper
    {
        public static String GetStringColumn(DbDataReader reader, String name, bool trim = true)
        {
            String result = null;
            Object val = reader[name];
            if (val != null && val != DBNull.Value)
            {
                result = val.ToString();
                if (trim) result = result.Trim();
            }
            return result;
        }


        public static int? GetIntColumn(DbDataReader reader, String name)
        {
            int? result = null;
            try
            {
                Object val = reader[name];
                if (val != null && val != DBNull.Value) result = Convert.ToInt32(val);
            }
            catch (Exception /*ex*/)
            {
                result = null;
            }
            return result;
        }

        public static double? GetDoubleColumn(DbDataReader reader, String name)
        {
            double? result = null;
            try
            {
                Object val = reader[name];
                if (val != null && val != DBNull.Value) result = Convert.ToDouble(val);
            }
            catch (Exception /*ex*/)
            {
                result = null;
            }
            return result;
        }

        public static DateTime? GetDateTimeColumn(DbDataReader reader, String name)
        {
            DateTime? result = null;
            try
            {
                Object val = reader[name];
                if (val != null && val != DBNull.Value)
                {
                    string valstr = val.ToString().Trim();
                    if (!String.IsNullOrEmpty(valstr))
                    {
                        result = Convert.ToDateTime(valstr);
                    }
                }
            }
            catch (Exception /*ex*/)
            {
                result = null;
            }
            return result;
        }

        public static string FormatDateTime(DateTime dt)
        {
            return String.Format("{0:yyyy-MM-dd HH:mm:ss}", dt);
        }

        public static string GetDateTimeString(DbDataReader reader, string name)
        {
            DateTime? dt = GetDateTimeColumn(reader, name);
            if (dt.HasValue)
            {
                return FormatDateTime(dt.Value);
            }
            Object val = reader[name];
            if (val != null && val != DBNull.Value) return val.ToString();
            else return null;
        }

    }
}
