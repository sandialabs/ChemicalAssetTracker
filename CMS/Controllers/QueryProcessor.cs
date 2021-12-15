using Common;
using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseClasses;
using System.Globalization;

namespace CMS.Controllers
{

    ///----------------------------------------------------------------
    ///
    /// Class:          QueryProcessor
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Class to run database queries (DatabaseQuery)
    /// </summary>
    ///
    ///----------------------------------------------------------------
    public class QueryProcessor
    {
        private DatabaseQuery m_query;
        private int m_maxrows = 100;

        public string WhereClause { get; set; }

        ///----------------------------------------------------------------
        ///
        /// Function:       Constructor
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Initialize a QueryProcessor instance
        /// </summary>
        ///
        /// <param name="query">a DatabaseQuery object</param>
        ///
        ///----------------------------------------------------------------
        public QueryProcessor(DatabaseQuery query, int maxrows, string where_clause = null)
        {
            m_query = query;
            m_maxrows = maxrows;
            WhereClause = where_clause;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       RunQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Execute a database query
        /// </summary>
        ///
        /// <param name="parameter_defs">list of parameter names and types</param>
        /// <param name="parameters">values for query parameters</param>
        /// <returns>a DatabaseResult instance or null</returns>
        /// 
        ///
        ///----------------------------------------------------------------
        public DatabaseResult RunQuery(List<TypedQueryParameter> parameter_defs)
        {
            DatabaseResult result = null;
            string sql = m_query.QueryText;
            if (!String.IsNullOrEmpty(WhereClause)) sql = sql.Replace("@Where", $" WHERE {WhereClause}");
            sql +=  $" limit {m_maxrows}";
            List<DatabaseQueryParameter> bindings = new List<DatabaseQueryParameter>();
            Dictionary<string, string> connection_settings = null;
            using (CMSDB db = new CMSDB())
            {
                connection_settings = db.GetConnectionSettings();
                if (parameter_defs != null)
                {
                    foreach (var p in parameter_defs)
                    {
                        switch (p.DataType)
                        {
                            case "location":
                                int root = p.GetInt();
                                StorageLocation loc = db.FindLocation(root);
                                if (loc != null)
                                {
                                    if (loc.LocationLevel > 0)
                                    {
                                        bindings.Add(new DatabaseQueryParameter(p.Name, loc.Path));
                                    }
                                    else
                                    {
                                        // this is the root, which is not included in the path
                                        // bind Path to something that will always match
                                        bindings.Add(new DatabaseQueryParameter(p.Name, ""));
                                    }
                                }
                                break;
                            case "string":
                                bindings.Add(new DatabaseQueryParameter(p.Name, p.GetString()));
                                break;
                            case "int":
                                sql = sql.Replace("@" + p.Name, $"{p.GetInt()}");
                                break;
                            case "double":
                                sql = sql.Replace("@" + p.Name, $"{p.GetDouble()}");
                                break;
                            case "datetime":
                                string dtval = DBHelper.FormatDateTime(p.GetDateTime());
                                sql = sql.Replace("@" + p.Name, $"'{dtval}'");
                                break;
                            default:
                                throw new Exception("QueryProcessor.RunQuery - unsupported data type: " + p.DataType);
                        }
                    }
                }
            }
            // data source, user id, password
            string hostname = GetConnectionSetting(connection_settings, "server", "data source");
            string user = GetConnectionSetting(connection_settings, "user", "user id");
            string pswd = GetConnectionSetting(connection_settings, "password");
            using (MySqlDatabase db = new MySqlDatabase(hostname, "cms", user, pswd))
            {
                result = db.Query(sql, bindings);
            }
            return result;
        }

        private string GetConnectionSetting(Dictionary<string, string> connection_settings, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (connection_settings.ContainsKey(key))
                {
                    return connection_settings[key];
                }
            }
            string keylist = String.Join(",", keys);
            throw new Exception($"Can't find any of \"{keylist}\" in connection settings");
        }
    }

}
