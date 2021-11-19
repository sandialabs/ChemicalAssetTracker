using System;
using System.Collections.Generic;

namespace Common
{
    public class CmdLine
    {
        public List<string> Args { get; set; }
        public int ArgCount { get { return Args.Count;  } }

        List<string> m_valid_flags = new List<string>();

        // convience indexer for command line arguments:  string dbname = cmdline["-db", "foobar.db"]
        public string this[string name, string default_value = null] { get { return GetArg(name, default_value);  } }

        public CmdLine(string[] argv, params string[] valid_flags)
        {
            Args = new List<string>(argv);
            foreach (var flag in valid_flags) m_valid_flags.Add(flag);
        }

        public bool HasFlag(string name)
        {
            return Args.Contains(name);
        }

        public string GetArg(string name, string default_value = null)
        {
            int pos = Args.IndexOf(name);
            if (pos < 0  || (pos + 1) >= ArgCount) return default_value;
            return Args[pos + 1];
        }

        public List<string> Positionals()
        {
            int ix = 0;
            List<string> result = new List<string>();
            while (ix < Args.Count)
            {
                string arg = Args[ix];
                if (m_valid_flags.Contains(arg))
                {
                    ix += 2;
                    continue;
                }
                if (arg.StartsWith('-'))
                {
                    ix += 1;
                    continue;
                }
                result.Add(arg);
                ix += 1;
            }
            return result;
        }
    }
}
