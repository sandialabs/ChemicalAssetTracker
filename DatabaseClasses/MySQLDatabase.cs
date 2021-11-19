#define INCLUDE_MYSQL

#if INCLUDE_MYSQL
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace DatabaseClasses
{
    public class MySqlDBConnectionInfo : DatabaseConnectionInfo
    {

        public MySqlDBConnectionInfo(String hostname, String dbname, String username, String password)
        {
            Hostname = hostname;
            DBName = dbname;
            Username = username;
            Password = password;
        }



        protected override String GenerateConnectionString()
        {
            return String.Format("Server={0};Database={1};Uid={2};Pwd={3};", Hostname, DBName, Username, Password);
        }

    }

    public class MySqlDatabase : BaseDatabase
    {
        public MySqlDatabase(String host, String database, String user, String password)
            : base(String.Format("Server={0};Database={1};Uid={2};Pwd={3};;Allow User Variables=True;", host, database, user, password))
        {
            try
            {
                m_db = new MySqlConnection(m_connection_string);
                m_db.Open();
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                throw e;
            }
        }


        public MySqlDatabase(MySqlDBConnectionInfo info)
            : base(info.Connection)
        {
            try
            {
                m_db = new MySqlConnection(m_connection_string);
                m_db.Open();
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                throw e;
            }
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteNonQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Execute a non-query SQL statement that has parameter bindings
        /// </summary>
        ///
        /// <param name="sql">the statement to execute</param>
        /// <param name="bindings">a list of parameter bindings</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>DatabaseResult object with the results - use LastError to get error message otherwise</returns>
        /// 
        /// <remarks>
        /// The sql statement will have placeholders for the bindings like "@name", "@value", etc.
        /// The bindings are built using something like bindings.Add(new DatabaseQueryParameter("name", "Bill"))
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public override DatabaseResult ExecuteNonQuery(String sql, List<DatabaseQueryParameter> bindings, DbTransaction trans)
        {
            DatabaseResult result = new DatabaseResult();
            if (m_db == null)
            {
                m_last_error = "Database is not open.";
                return result;
            }
            m_last_error = "";
            try
            {
                using (DbCommand cmd = CreateCommand(sql, m_db, trans))
                {
                    foreach (DatabaseQueryParameter binding in bindings)
                    {
                        if (binding.Value != null) cmd.Parameters.Add(new MySqlParameter(binding.Name, binding.Value));
                        else if (binding.BlobValue != null) cmd.Parameters.Add(new MySqlParameter(binding.Name, binding.BlobValue));

                    }
                    result.RowsRead = cmd.ExecuteNonQuery();
                    result.Result = true;
                }
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                result.Result = false;
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteNonQueryWithIdentity
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Convenience function to execute an INSERT, UPDATE, or DELETE
        /// and return the resulting identity value
        /// </summary>
        ///
        /// <param name="sql">the statement to execute</param>
        /// <param name="db">database</param>
        /// <param name="trans">open database transaction, or null</param>
        /// <returns>int identity value</returns>
        /// 
        /// <remarks>Throws an exception if operation fails</remarks>
        ///
        ///----------------------------------------------------------------
        public int ExecuteNonQueryWithIdentity(String sql, DbTransaction trans)
        {
            m_last_error = "";
            DatabaseResult rc = this.ExecuteNonQuery(sql, trans);
            if (rc.Result == false)
            {
                throw (new Exception("Exception in MySqlDatabase.ExecuteNonQueryWithIdentity - " + m_last_error));
            }
            return (int)GetGlobalIdentity(trans);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteNonQueryWithIdentity
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Convenience function to execute an INSERT, UPDATE, or DELETE
        /// and return the resulting identity value
        /// </summary>
        ///
        /// <param name="sql">the statement to execute</param>
        /// <param name="bindings">parameter binding list</param>
        /// <param name="trans">open database transaction, or null</param>
        /// <returns>int identity value</returns>
        /// 
        /// <remarks>Throws an exception if operation fails</remarks>
        ///
        ///----------------------------------------------------------------
        public int ExecuteNonQueryWithIdentity(String sql, List<DatabaseQueryParameter> bindings, DbTransaction trans)
        {
            DatabaseResult rc = this.ExecuteNonQuery(sql, bindings, trans);
            if (rc.Result == false)
            {
                throw (new Exception("Exception in MySqlDatabase.ExecuteNonQueryWithIdentity - " + m_last_error));
            }
            return (int)GetGlobalIdentity(trans);
        }



        protected override DbCommand CreateCommand(string query, DbConnection connection, DbTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand(query, m_db as MySqlConnection);
            return command;
        }

        protected override DbCommand CreateCommand(string query, DbConnection connection, List<DatabaseQueryParameter> bindings, DbTransaction transaction)
        {
            MySqlCommand command = CreateCommand(query, connection, transaction) as MySqlCommand;
            foreach (var binding in bindings)
            {
                command.Parameters.AddWithValue($"@{binding.Name}", binding.Value);
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
                using (DbCommand cmd = CreateCommand("SELECT LAST_INSERT_ID() AS IdentityValue", m_db, trans))
                {
                    object obj = cmd.ExecuteScalar();
                    result = Convert.ToUInt64(obj);
                }
            }
            catch (Exception e)
            {
                result = 0;
                m_last_error = e.Message;
            }
            return result;
        }
    }

}
#endif