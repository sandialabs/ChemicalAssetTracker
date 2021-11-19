using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class UniqueStringList : List<string>
    {
        public new void Add(string value)
        {
            if (!this.Contains(value)) base.Add(value);
        }
    }
}
