using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class TypedQueryParameter
    {
        public string DataType { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public string GetString()
        {
            return (Value.ToString());
        }

        public int GetInt()
        {
            return Convert.ToInt32(Value);
        }

        public double GetDouble()
        {
            return Convert.ToDouble(Value);
        }

        public DateTime GetDateTime()
        {
            return Convert.ToDateTime(Value);
        }
    }
}
