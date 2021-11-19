using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{

    ///----------------------------------------------------------------
    ///
    /// Class:          ObjectDictionary
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Generic name/value dictionary
    /// </summary>
    ///
    ///----------------------------------------------------------------
    public class ObjectDictionary : Dictionary<string, object>
    {

        ///----------------------------------------------------------------
        ///
        /// Function:       GetValue
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a value as object
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>an object or null</returns>
        ///
        ///----------------------------------------------------------------
        public object GetValue(string key, object default_value = null)
        {
            if (this.ContainsKey(key)) return this[key];
            else return default_value;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetString
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a string value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>a string or null</returns>
        ///
        ///----------------------------------------------------------------
        public string GetString(string key, string default_value = null)
        {
            object val = GetValue(key);
            if (val != null)
            {
                if (val is String) return val as String;
                else return val.ToString();
            }
            else return default_value;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetInt
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get an Int32 value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>an int</returns>
        ///
        ///----------------------------------------------------------------
        public int GetInt(string key, int default_value = 0)
        {
            object val = GetValue(key);
            if (val != null)
            {
                if (val is Int16 || val is Int32 || val is Int64) return Convert.ToInt32(val);
                if (val is String)
                {
                    int result;
                    if (Int32.TryParse(val as String, out result)) return result;
                    else return default_value;
                }
            }
            return default_value;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetDouble
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a double value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>a double</returns>
        ///
        ///----------------------------------------------------------------
        public double GetDouble(string key, double default_value = 0)
        {
            object val = GetValue(key);
            if (val != null)
            {
                if (val is Double || val is Int16 || val is Int32 || val is Int64) return Convert.ToDouble(val);
                if (val is String)
                {
                    double result;
                    if (Double.TryParse(val as String, out result)) return result;
                    else return default_value;
                }
            }
            return default_value;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetBool
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a boolean value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>a bool</returns>
        ///
        ///----------------------------------------------------------------
        public bool GetBool(string key, bool default_value = false)
        {
            object val = GetValue(key);
            if (val != null)
            {
                if (val is bool) return (bool)val;
                if (val is Int16 || val is Int32 || val is Int64) return (Convert.ToInt32(val) != 0);
            }
            return default_value;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetDateTime
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a nullable DateTime value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>a DateTime or null</returns>
        ///
        ///----------------------------------------------------------------
        public DateTime? GetDateTime(string key, DateTime? default_value = null)
        {
            object val = GetValue(key);
            if (val != null)
            {
                if (val is DateTime) return (DateTime)val;
                if (val is String)
                {
                    DateTime result;
                    if (DateTime.TryParse(val as String, out result)) return result;
                    else return default_value;
                }
            }
            return default_value;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetDateTime
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a DateTime value
        /// </summary>
        ///
        /// <param name="key">the name to look for</param>
        /// <param name="default_value">what to return if the key does not exist</param>
        /// <returns>a DateTime</returns>
        ///
        ///----------------------------------------------------------------
        public DateTime GetDateTime(string key, DateTime default_value)
        {
            DateTime? val = GetDateTime(key, null);
            if (val.HasValue) return val.Value;
            else return default_value;
        }
    }


}
