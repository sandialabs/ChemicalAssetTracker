using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class Utility
    {

        ///----------------------------------------------------------------
        ///
        /// Function:       SplitString
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Split a string trimming each substring and returning only those
        /// with at least one character.
        /// </summary>
        ///
        /// <param name="input">the string to split</param>
        /// <param name="dlm">the substring delimiter</param>
        /// <returns>a list of strings</returns>
        ///
        ///----------------------------------------------------------------
        ///
        public static List<string> SplitString(string input, char dlm)
        {
            string[] parts = input.Split(dlm, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            foreach (string part in parts)
            {
                string str = part.Trim();
                if (str.Length > 0) result.Add(str);
            }
            return result;
        }
    }
}
