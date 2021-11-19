using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DataModel
{
    public class DbContextHelper
    {
        public enum EDatabaseType { MYSQL, SQLSERVER, SQLITE };
        public delegate bool QueryCallback(DbDataReader reader);
        protected static EDatabaseType s_dbtype = EDatabaseType.MYSQL;

        public static void UseMySQL() { s_dbtype = EDatabaseType.MYSQL;  }

        //#################################################################
        //
        // RAW SQL QUERIES 
        //
        //#################################################################

        public static System.Data.DataTable ExecuteUnboundQuery(DbContext db, string sql, QueryCallback callback)
        {
            System.Data.DataTable schema = null;
            DbConnection connection = null;
            try
            {
                connection = db.Database.GetDbConnection();
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            //schema = reader.GetSchemaTable();
                            //List<string> columns = new List<string>();
                            //foreach (DataColumn column in schema.Columns)
                            //{
                            //    columns.Add(column.ColumnName);
                            //    column.DataType
                            //}
                            while (reader.Read())
                            {
                                if (callback(reader) == false) break;
                            }
                        }
                    }
                }
                connection.Close();
                connection = null;
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                Console.WriteLine($"Exception in CMSDB.ExecuteUnboundQuery: {ex.InnerException}");
                if (connection != null) connection.Close();
                schema = null;
            }
            return schema;
        }

        public static bool RowExists(DbContext db, string sql)
        {
            bool result = false;
            var queryresult = ExecuteUnboundQuery(db, sql, (reader) => { result = true; return false; } );
            return result;
        }

        public static bool ProcedureExists(DbContext db, string database, string procname)
        {
            string sql = null;
            switch (s_dbtype)
            {
                case EDatabaseType.MYSQL:
                    sql = $"select ROUTINE_NAME from INFORMATION_SCHEMA.ROUTINES where ROUTINE_TYPE='PROCEDURE' and ROUTINE_SCHEMA='{database}' and ROUTINE_NAME='{procname}'";
                    break;
                case EDatabaseType.SQLSERVER:
                    break;
                case EDatabaseType.SQLITE:
                    break;
                default:
                    break;
            }
            if (sql == null) throw new Exception($"DbContextHelper: {s_dbtype} is not yet supported.");
            return RowExists(db, sql);
        }

        public static bool FunctionExists(DbContext db, string database, string funcname)
        {
            string sql = null;
            switch (s_dbtype)
            {
                case EDatabaseType.MYSQL:
                    sql = $"select ROUTINE_NAME from INFORMATION_SCHEMA.ROUTINES where ROUTINE_TYPE='FUNCTION' and ROUTINE_SCHEMA='{database}' and ROUTINE_NAME='{funcname}'";
                    break;
                case EDatabaseType.SQLSERVER:
                    break;
                case EDatabaseType.SQLITE:
                    break;
                default:
                    break;
            }
            if (sql == null) throw new Exception($"DbContextHelper: {s_dbtype} is not yet supported.");
            return RowExists(db, sql);
        }

    }
}
