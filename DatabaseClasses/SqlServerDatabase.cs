#define DONT_INCLUDE_SQL_SERVER

#if INCLUDE_SQL_SERVER

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseClasses
{
    /// <summary>
    /// Represents a connection to a SQL Server database. For other database types, use the OleDatabase class
    /// </summary>
    public class SqlServerDatabase : BaseDatabase
    {
        public SQLDBConnectionInfo ConnectionInfo { get; private set; }

        public SqlServerDatabase(SQLDBConnectionInfo conn_info) : base(conn_info.Connection)
        {
            ConnectionInfo = conn_info;

            try
            {
                m_db = new SqlConnection(ConnectionInfo.Connection);
                m_db.Open();
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                throw e;
            }
        }


        public SqlServerDatabase(string host, string dbname) : base(new SQLDBConnectionInfo(host, dbname).Connection)
        {
            ConnectionInfo = new SQLDBConnectionInfo(host, dbname);

            try
            {
                m_db = new SqlConnection(ConnectionInfo.Connection);
                m_db.Open();
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                throw e;
            }
        }

        public static SQLDBConnectionInfo BuildConnectionInfo(string host, string dbname)
        {
            return new SQLDBConnectionInfo(host, dbname);
        }



        protected override DbCommand CreateCommand(string query, DbConnection connection, DbTransaction transaction)
        {
            SqlCommand command = null;
            if (m_db is SqlConnection && (transaction == null || transaction is SqlTransaction))
            {
                if (transaction == null)
                    command = new SqlCommand(query, m_db as SqlConnection);
                else
                    command = new SqlCommand(query, m_db as SqlConnection, transaction as SqlTransaction);
            }
            return command;
        }

        protected override DbCommand CreateCommand(string query, DbConnection connection, List<DatabaseQueryParameter> bindings, DbTransaction transaction)
        {
            SqlCommand command = null;
            if (m_db is SqlConnection && (transaction == null || transaction is SqlTransaction))
            {
                if (transaction == null)
                    command = new SqlCommand(query, m_db as SqlConnection);
                else
                    command = new SqlCommand(query, m_db as SqlConnection, transaction as SqlTransaction);
                if (bindings != null)
                {
                    foreach (var binding in bindings)
                    {
                        if (binding.BlobValue != null) command.Parameters.AddWithValue(binding.Name, binding.BlobValue);
                        else command.Parameters.AddWithValue(binding.Name, binding.Value);
                    }
                }
            }
            return command;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetGlobalIdentity
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the last identity value from the database.
        /// </summary>
        ///
        /// <param name="trans">an active transaction or null</param>
        /// <returns>the last identity value created in the database</returns>
        ///
        ///----------------------------------------------------------------
        public ulong GetGlobalIdentity(DbTransaction trans)
        {
            if (m_db == null)
            {
                m_last_error = "Database is not open.";
                return 0;
            }
            m_last_error = "";
            ulong result = 0;
            try
            {
                using (DbCommand cmd = CreateCommand("SELECT @@IDENTITY AS IdentityValue", m_db, trans))
                {
                    object obj = cmd.ExecuteScalar();
                    result = Convert.ToUInt64(obj);
                    //String oval = o.ToString();
                    //if (oval.Length > 0)
                    //{
                    //    result = Convert.ToInt32(o);
                    //}
                }
            }
            catch (Exception e)
            {
                result = 0;
                //m_log.ErrorFormat("GetGlobalIdentity failed: {0}", e.Message);
                m_last_error = e.Message;
            }
            return result;
        }

        public override bool TableExists(string tablename)
        {
            string sql = $"select TABLE_SCHEMA, TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{tablename}'";
            return RowExists(sql);
        }

        public List<string> GetColumnNames(string tablename)
        {
            List<string> result = new List<string>();
            this.ExecuteQuery($"select name from sys.columns where object_id = OBJECT_ID('{tablename}')", (reader) =>
            {
                result.Add(reader[0].ToString());
                return true;
            });

            return result;
        }
    }
}

#endif