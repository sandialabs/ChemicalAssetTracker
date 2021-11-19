using CMS.Services;
using DataModel;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using UAParser;

namespace CMS.Controllers
{
    public class ControllerHelper
    {
        private static DateTime? s_build_date = null;


        public static UserRoles InitializeViewData(ViewDataDictionary viewdata, string login = null, IAccountHelper account_helper = null)
        {
            if (s_build_date == null)
            {
                BuildInformation buildinfo = new BuildInformation();
                s_build_date = buildinfo.BuildDate;
            }
            viewdata["BuildTime"] = s_build_date.Value.ToFileTime().ToString();
            if (login != null) viewdata["UserName"] = login;
            UserRoles permissions = GetUserRoles(login, account_helper);
            if (account_helper != null)
            {
                List<string> roles = account_helper.GetUserRoles(login);
                viewdata["UserRoles"] = roles;

                permissions.Assign(roles);
                viewdata["Permissions"] = permissions;
                viewdata["Roles"] = String.Join(",", roles);
                viewdata["IsAdmin"] = permissions.IsAdmin;
                viewdata["IsManager"] = permissions.IsManager;
                viewdata["IsAuditor"] = permissions.IsAuditor;
                viewdata["IsEditor"] = permissions.IsEditor;
                viewdata["IsViewer"] = permissions.IsViewer;
            }
            return permissions;
        }

        public static UserRoles GetUserRoles(string login, IAccountHelper account_helper)
        {
            UserRoles permissions = new UserRoles();
            if (login != null  &&  account_helper != null)
            {
                List<string> roles = account_helper.GetUserRoles(login);

                permissions.Assign(roles);
            }
            return permissions;
        }

        public static void CreateLogEntry(string login, string category, string text, int message_level = 0, bool save_changes = true)
        {
            using (CMSDB db = new CMSDB())
            {
                db.AddLogEntry(login, category, text, message_level, save_changes);
            }

        }

        public static void CreateLogEntry(CMSDB db, string login, string category, string text, int message_level = 0, bool save_changes = true)
        {
            db.AddLogEntry(login, category, text, message_level, save_changes);
        }

        public static void LogError(string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(login, category, text, 2, save_changes);
        }

        public static void LogError(CMSDB db, string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(db, login, category, text, 2, save_changes);
        }

        public static void LogWarning(string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(login, category, text, 1, save_changes);
        }

        public static void LogWarning(CMSDB db, string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(db, login, category, text, 1, save_changes);
        }

        public static void LogInfo(string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(login, category, text, 0, save_changes);
        }

        public static void LogInfo(CMSDB db, string login, string category, string text, bool save_changes = true)
        {
            CreateLogEntry(db, login, category, text, 0, save_changes);
        }


        public static ClientInfo GetUserAgent(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            if (request.Headers.ContainsKey("User-Agent"))
            {
                var user_agent = request.Headers["User-Agent"];
                string ua_string = Convert.ToString(user_agent[0]);
                var ua_parser = Parser.GetDefault();
                ClientInfo c = ua_parser.Parse(ua_string);
                return c;
            }
            else return null;
        }
    }

}
