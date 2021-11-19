using System;

namespace DatabaseClasses
{
    public class DatabaseQueryParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public byte[] BlobValue { get; set; }

        public DatabaseQueryParameter(String name, String value)
        {
            Name = name;
            Value = value;
            BlobValue = null;
        }

        public DatabaseQueryParameter(String name, byte[] value)
        {
            Name = name;
            Value = null;
            BlobValue = value;
        }
    }
}
