using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class DTW
    {
        public int InsertCost { get; set; } = 1;
        public int ReplaceCost { get; set; } = 1;

        private int[] m_memo;
        private string m_value1;
        private string m_value2;
        private int m_len1;
        private int m_len2;

        static public double CompareStrings(string value1, string value2, bool ignore_case = true)
        {
            DTW dtw = new DTW();
            return dtw.Compare(value1, value2, ignore_case);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Search
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find the best match in a list of strings
        /// </summary>
        ///
        /// <param name="needle">what you are looking for</param>
        /// <param name="haystack">strings to search</param>
        /// <returns>a tuple containing the best match and its score (0.0 - 1.0)</returns>
        ///
        ///----------------------------------------------------------------
        public static (string, double) Search(string needle, List<string> haystack, bool ignore_case = true)
        {
            DTW dtw = new DTW();
            string best_match = null;
            double best_score = -1;
            haystack.ForEach(tstr =>
            {
                double score = dtw.Compare(needle, tstr, ignore_case);
                if (score > best_score)
                {
                    best_score = score;
                    best_match = tstr;
                }
            });
            return (best_match, best_score);
        }

        public double Compare(string value1, string value2, bool ignore_case = true)
        {
            if (ignore_case)
            {
                m_value1 = value1.ToLower();
                m_value2 = value2.ToLower();
            }
            else
            {
                m_value1 = value1;
                m_value2 = value2;
            }
            m_len1 = m_value1.Length;
            m_len2 = m_value2.Length;
            if (m_len1 == 0 || m_len2 == 0) return 0;
            InitializeMemo();
            double cost = DoDtw(m_len1 - 1, m_len2 - 1);
            return 1.0 - (cost / (double)(this.InsertCost * Math.Max(m_len1, m_len2)));
        }

        private int DoDtw(int ix1, int ix2)
        {
            int result = GetMemo(ix1, ix2);
            if (result < 0)
            {
                int cost = ReplaceCost;
                if (m_value1[ix1] == m_value2[ix2]) cost = 0;
                if (ix1 == 0 && ix2 == 0) result = cost;
                else if (ix2 == 0) result = cost + (ix1 - 1) * InsertCost;
                else if (ix1 == 0) result = cost + (ix2 - 1) * InsertCost;
                else
                {
                    int ci1 = InsertCost + DoDtw(ix1 - 1, ix2);
                    int ci2 = InsertCost + DoDtw(ix1, ix2 - 1);
                    int cr = cost + DoDtw(ix1 - 1, ix2 - 1);
                    result = Math.Min(Math.Min(ci1, ci2), cr);
                    SetMemo(ix1, ix2, result);
                }
            }
            return result;
        }

        private void InitializeMemo()
        {
            m_memo = new int[m_len1 * m_len2];
            for (var i = 0; i < m_len1 * m_len2; i++)
            {
                m_memo[i] = -1;
            }
        }

        private int MemoOffset(int i1, int i2)
        {
            return (m_len2 * i1) + i2;
        }

        private void SetMemo(int i1, int i2, int value)
        {
            int offset = MemoOffset(i1, i2);
            m_memo[offset] = value;
        }

        private int GetMemo(int i1, int i2)
        {
            int offset = MemoOffset(i1, i2);
            return m_memo[offset];
        }
    }
}
