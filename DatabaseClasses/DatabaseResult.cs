using System.Collections.Generic;

namespace DatabaseClasses
{
    public class DatabaseResult
    {
        public int RowsRead { get { return m_rows_read; } set { m_rows_read = value; } }
        public bool Result { get { return m_result; } set { m_result = value; } }
        public List<System.Collections.Generic.Dictionary<string, object>> Rows { get; set; }
        public string ErrorMessage { get; set; }

        public int IncrementRowsRead() { return ++m_rows_read; }

        private int m_rows_read = 0;
        private bool m_result = false;
    };
}
