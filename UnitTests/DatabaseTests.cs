using System;
using System.IO;
using ImportLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void SQLiteTestMethod1()
        {
            string dbpath = FindTestFile("cms.db");
            Assert.IsNotNull(dbpath);
            using (ImportLib.LegacyDatabase db = new ImportLib.LegacyDatabase(dbpath))
            {
                db.RefreshOwners();
                db.RefreshStorageGroups();
                Assert.IsTrue(db.FetchOwners().Count > 0);
                Assert.IsTrue(db.FetchStorageGroups().Count > 0);
                Assert.IsNotNull(db.FindByBarcode("AQ00879817"));
            }
        }

        private static string FindTestFile(string filename, string path = null)
        {
            // if the input path is null, use the current directory
            // if the input path is an empty string, return null
            if (path == null) path = Environment.CurrentDirectory;
            else if (path.Length == 0) return null;

            string filepath = Path.Combine(path, filename);
            if (File.Exists(filepath)) return filepath;
            else
            {
                string parent_path = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar));
                if (parent_path != null) return FindTestFile(filename, parent_path);
                else return null;
            }
        }
    }
}
