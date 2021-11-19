#define INCLUDE_MYSQL
#define INCLUDE_SQL_SERVER

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseClasses
{
    public class DatabaseConnectionInfo
    {
        public String Hostname { get; protected set; }
        public String DBName { get; protected set; }
        public String Username { get; protected set; }
        public String Password { get; protected set; }
        public String Connection { get { return GenerateConnectionString(); } }

        protected virtual String GenerateConnectionString() { return null; }
    }


#if INCLUDE_SQL_SERVER
    /// <summary>
    /// Used to specify the connection information necessary for connecting to a SQL Server database.
    /// Specify a username/password to use SQL Server security; no username/password indicates Windows security.
    /// </summary>
    public partial class SQLDBConnectionInfo : DatabaseConnectionInfo
    {
        //public String EntityFrameworkConnection { get; private set; }
        public bool UsingIntegratedSecurity { get { return Username == String.Empty && Password == String.Empty; } }

        private string m_connection_string;

        public SQLDBConnectionInfo(String connection_string)
        {
            m_connection_string = connection_string;

            Regex datasource_regex = new Regex("Data Source=([^;]+)", RegexOptions.IgnoreCase);
            Regex catalog_regex = new Regex("Initial Catalog=([^;]+)", RegexOptions.IgnoreCase);
            Regex userid_regex = new Regex("User ID=([^;]+)", RegexOptions.IgnoreCase);
            Regex password_regex = new Regex("Password=([^;]+)", RegexOptions.IgnoreCase);

            Match m = datasource_regex.Match(connection_string);
            if (m.Success)
            {
                Hostname = m.Groups[1].Value;
            }

            m = catalog_regex.Match(connection_string);
            if (m.Success)
            {
                DBName = m.Groups[1].Value;
            }

            m = userid_regex.Match(connection_string);
            if (m.Success)
            {
                Username = m.Groups[1].Value;
            }

            m = password_regex.Match(connection_string);
            if (m.Success)
            {
                Password = m.Groups[1].Value;
            }

            //if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            //    EntityFrameworkConnection = String.Format("metadata=res://*/SLDDatabase.csdl|res://*/SLDDatabase.ssdl|res://*/SLDDatabase.msl;provider=System.Data.SqlClient;provider connection string=\"data source={0};initial catalog={1};integrated security=True;multipleactiveresultsets=True;App=EntityFramework\"", Hostname, DBName);
            //else
            //    EntityFrameworkConnection = String.Format("metadata=res://*/SLDDatabase.csdl|res://*/SLDDatabase.ssdl|res://*/SLDDatabase.msl;provider=System.Data.SqlClient;provider connection string=\"data source={0};initial catalog={1};User ID={2};Password={3};multipleactiveresultsets=True;App=EntityFramework\"", Hostname, DBName, Username, Password);
        }

        /// <summary>
        /// Used when connecting using SQL security.
        /// </summary>
        /// <param name="hostname">The machine name hosting the database of interest, or its IP address, or (local)</param>
        /// <param name="dbname">The name of the database of interest, e.g. "DHS"</param>
        /// <param name="username">The username to log-in as</param>
        /// <param name="password">The username's password</param>
        public SQLDBConnectionInfo(String hostname, String dbname, String username, String password)
        {
            Hostname = hostname;
            DBName = dbname;
            Username = username;
            Password = password;
            //if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            //{
            //    Connection = String.Format("Data Source={0};Initial Catalog={1};Integrated Security=True", hostname, dbname);
            //    EntityFrameworkConnection = String.Format("metadata=res://*/SLDDatabase.csdl|res://*/SLDDatabase.ssdl|res://*/SLDDatabase.msl;provider=System.Data.SqlClient;provider connection string=\"data source={0};initial catalog={1};integrated security=True;multipleactiveresultsets=True;App=EntityFramework\"", hostname, dbname);
            //}
            //else
            //{
            //    Connection = String.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}", hostname, dbname, username, password);
            //    EntityFrameworkConnection = String.Format("metadata=res://*/SLDDatabase.csdl|res://*/SLDDatabase.ssdl|res://*/SLDDatabase.msl;provider=System.Data.SqlClient;provider connection string=\"data source={0};initial catalog={1};User ID={2};Password={3};multipleactiveresultsets=True;App=EntityFramework\"", hostname, dbname, username, password);
            //}
        }

        /// <summary>
        /// Used when connecting using Windows security
        /// </summary>
        /// <param name="hostname">The machine name hosting the database of interest, or its IP address, or (local)</param>
        /// <param name="dbname">The name of the database of interest, e.g. "DHS"</param>
        public SQLDBConnectionInfo(String hostname, String dbname)
        {
            Hostname = hostname;
            DBName = dbname;
            Username = String.Empty;
            Password = String.Empty;
            //Connection = String.Format("Data Source={0};Initial Catalog={1};Integrated Security=True", hostname, dbname);
            //EntityFrameworkConnection = String.Format("metadata=res://*/SLDDatabase.csdl|res://*/SLDDatabase.ssdl|res://*/SLDDatabase.msl;provider=System.Data.SqlClient;provider connection string=\"data source={0};initial catalog={1};integrated security=True;multipleactiveresultsets=True;App=EntityFramework\"", hostname, dbname);
        }

        public override string ToString()
        {
            return GenerateConnectionString();
        }

        protected override string GenerateConnectionString()
        {
            if (m_connection_string != null)
                return m_connection_string;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                return string.Format("Data Source={0};Initial Catalog={1};Integrated Security=True", Hostname, DBName);
            else
                return string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}", Hostname, DBName, Username, Password);
        }
    }
#endif


#if INCLUDE_MYSQL
#endif

}
