using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DatabaseClasses
{
    public abstract class BaseDatabase : IDisposable
    {
        /// <summary>
        /// Called for each row in a resultset.
        /// </summary>
        /// <param name="reader">The reader to use to extract the result data.</param>
        /// <returns>true if processing should continue; false if it should stop</returns>
        public delegate bool DbReaderCallback(DbDataReader reader);

        protected DbConnection m_db = null;
        protected String m_last_error;
        protected string m_connection_string;

        public BaseDatabase(string connection_string)
        {
            m_connection_string = connection_string;
        }

        public virtual void Close()
        {
            if (m_db != null)
            {
                m_db.Close();
                m_db.Dispose();
                m_db = null;
            }
        }

        public DbConnection Handle { get { return m_db; } }
        public string ConnectionString { get { return m_connection_string; } }
        public bool IsOpen { get { return (m_db != null && m_db.State == ConnectionState.Open); } }

        //public static BaseDatabase Create(DatabaseConnectionInfo info)
        //{
        //    BaseDatabase result = null;
        //    if (info is SQLDBConnectionInfo) result = new SqlDatabase(info as SQLDBConnectionInfo);
        //    else if (info is MySqlDBConnectionInfo) result = new MySqlDatabase(info as MySqlDBConnectionInfo);
        //    return result;
        //}


        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Run a query and call a callback for each row
        /// </summary>
        ///
        /// <param name="query">the sql to run</param>
        /// <param name="callback">a function to call for each row</param>
        /// <returns>A DatabaseResult object with the results</returns>
        ///
        ///----------------------------------------------------------------
        public DatabaseResult ExecuteQuery(String query, DbReaderCallback callback, List<DatabaseQueryParameter> bindings = null)
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
                using (DbCommand cmd = CreateCommand(query, m_db, bindings, null))
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.IncrementRowsRead();
                            if (callback(reader) == false) break;
                        }

                        result.Result = true;
                    }
                }

            }
            catch (Exception e)
            {
                m_last_error = e.Message;
            }

            return result;
        }

        protected abstract DbCommand CreateCommand(string query, DbConnection connection, DbTransaction transaction);
        protected abstract DbCommand CreateCommand(string query, DbConnection connection, List<DatabaseQueryParameter> bindings, DbTransaction transaction);

        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Run a query and call a callback for each row
        /// </summary>
        ///
        /// <param name="query">the sql to run</param>
        /// <param name="transaction">an active database transaction</param>
        /// <param name="callback">a function to call for each row</param>
        /// <returns>A DatabaseResult object with the results</returns>
        ///
        ///----------------------------------------------------------------
        public DatabaseResult ExecuteQuery(String query, DbTransaction transaction, DbReaderCallback callback)
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
                using (DbCommand cmd = CreateCommand(query, m_db, transaction))
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.IncrementRowsRead();
                            if (callback(reader) == false) break;
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
            }
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Query
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Run a query and put the results in DatabaseResult.Rows
        /// </summary>
        ///
        /// <param name="query">the query sql</param>
        /// <param name="bindings">query parameter binings, or null</param>
        /// <param name="transaction">optional open transaction</param>
        /// <param name="timeout_seconds">max seconds before query times out</param>
        /// <returns>a DatabaseResult</returns>
        /// 
        /// <remarks>
        /// The sql statement will have placeholders for the bindings like "@name", "@value", etc.
        /// The bindings are built using something like bindings.Add(new DatabaseQueryParameter("name", "Bill"))
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public DatabaseResult Query(string query, List<DatabaseQueryParameter> bindings, DbTransaction transaction = null, int timeout_seconds = 0)
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
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                List<string> column_names = new List<string>();
                using (DbCommand cmd = CreateCommand(query, m_db, bindings, transaction))
                {
                    if (timeout_seconds > 0) cmd.CommandTimeout = timeout_seconds;
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string colname = reader.GetName(i);
                            column_names.Add(colname);
                        }
                        while (reader.Read())
                        {
                            result.IncrementRowsRead();
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            foreach (string colname in column_names)
                            {
                                object value = reader[colname];
                                row.Add(colname, value);
                            }
                            rows.Add(row);
                        }
                        result.Rows = rows;
                        result.Result = true;
                    }
                }
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                m_last_error = e.Message;
                result.Result = false;
                result.ErrorMessage = m_last_error;
            }
            return result;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteNonQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Execute a non-query SQL statement
        /// </summary>
        ///
        /// <param name="sql">the statement to execute</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>DatabaseResult object with the results - use LastError to get error message otherwise</returns>
        ///
        ///----------------------------------------------------------------
        public virtual DatabaseResult ExecuteNonQuery(String sql, DbTransaction trans)
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
                    result.RowsRead = cmd.ExecuteNonQuery();
                    result.Result = true;
                }
            }
            catch (Exception e)
            {
                m_last_error = e.Message;
                result.ErrorMessage = m_last_error;
                result.Result = false;
            }
            return result;
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
        public virtual DatabaseResult ExecuteNonQuery(String sql, List<DatabaseQueryParameter> bindings, DbTransaction trans)
        {
            DatabaseResult result = new DatabaseResult();
            m_last_error = "Database is not open.";
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ExecuteScalar
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Execute a query and return the value of the first column of the first row of the result
        /// </summary>
        ///
        /// <param name="sql">the statement to execute</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>a column value object or null</returns>
        ///
        ///----------------------------------------------------------------
        public object ExecuteScalar(String sql, DbTransaction trans)
        {
            object result = null;
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
                    result = cmd.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                result = null;
                m_last_error = e.Message;
            }
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ReadScaler
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Read a string using the supplied query.
        /// </summary>
        ///
        /// <param name="query">the query to run</param>
        /// <param name="value">(out) the value that is read</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>true if the query returned a value</returns>
        ///
        ///----------------------------------------------------------------
        public bool ReadScalar(String query, out String value, DbTransaction trans)
        {
            bool result = false;
            String rc = "";

            this.ExecuteQuery(query, trans, delegate (DbDataReader reader) {
                rc = reader[0].ToString();
                result = true;
                return false;
            });

            value = rc;
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ReadScaler
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Read an int using the supplied query.
        /// </summary>
        ///
        /// <param name="query">the query to run</param>
        /// <param name="value">(out) the value that is read</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>true if the query returned a value</returns>
        ///
        ///----------------------------------------------------------------
        public bool ReadScalar(String query, out int value, DbTransaction trans)
        {
            bool result = false;
            int rc = 0;

            this.ExecuteQuery(query, trans, delegate (DbDataReader reader)
            {
                rc = Convert.ToInt32(reader[0]);
                result = true;
                return false;
            });

            value = rc;
            return result;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       ReadIntegers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Read integer values using a supplied query.
        /// </summary>
        ///
        /// <param name="query">the query to run</param>
        /// <param name="trans">an active transaction or null</param>
        /// <returns>a list of ints</returns>
        ///
        ///----------------------------------------------------------------
        public List<int> ReadIntegers(String query, DbTransaction trans)
        {
            List<int> result = new List<int>();
            this.ExecuteQuery(query, trans, delegate (DbDataReader reader)
            {
                result.Add(Convert.ToInt32(reader[0]));
                return true;
            });

            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ReadIntegers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Read integer values using a supplied query.
        /// </summary>
        ///
        /// <param name="query">the query to run</param>
        /// <returns>a list of ints</returns>
        ///
        ///----------------------------------------------------------------
        public List<int> ReadIntegers(String query)
        {
            List<int> result = new List<int>();
            this.ExecuteQuery(query, delegate (DbDataReader reader)
            {
                result.Add(Convert.ToInt32(reader[0]));
                return true;
            });

            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       RowExists
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Determine if a query will return any rows
        /// </summary>
        ///
        /// <param name="query">the query to run</param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public bool RowExists(String query)
        {
            bool result = false;
            this.ExecuteQuery(query, delegate (DbDataReader reader)
            {
                result = true;
                return false;  // dont read any more rows
            });
            return result;
        }

        public virtual bool TableExists(string tablename)
        {
            string sql = $"select TABLE_SCHEMA, TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{tablename}'";
            return RowExists(sql);
        }



        public DbTransaction BeginTransaction()
        {
            return m_db.BeginTransaction();
        }

        //----------------------------------------------------------------
        //
        // PROPERTIES
        //
        //----------------------------------------------------------------

        ///----------------------------------------------------------------
        ///
        /// Property:       LastError
        /// Author:         Pete Humphrey
        ///
        /// <summary>error message from last exception thrown</summary>
        ///
        ///----------------------------------------------------------------         
        public String LastError
        {
            get { return m_last_error; }
            //set { m_last_error = value; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (m_db != null)
            {
                m_db.Dispose();
                m_db = null;
            }
        }

        #endregion
    }
}
