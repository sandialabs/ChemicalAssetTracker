using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class UserRoles
    {
        public bool IsRoot { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsManager { get; set; }
        public bool IsAuditor { get; set; }
        public bool IsEditor { get; set; }
        public bool IsViewer { get; set; }

        public bool CanViewInventory { get { return (IsAdmin || IsManager || IsEditor || IsViewer); } }
        public bool CanSearch { get { return (IsAdmin || IsManager || IsEditor || IsViewer); } }
        public bool CanAudit { get { return (IsAdmin || IsManager || IsAuditor); } }
        public bool CanViewReports { get { return (IsAdmin || IsManager || IsEditor || IsViewer); } }
        public bool CanManageUsers { get { return (IsAdmin || IsManager); } }
        public bool CanManageLocations { get { return (IsAdmin || IsManager); } }
        public bool CanEditSettings { get { return (IsAdmin || IsManager); } }

        public UserRoles()
        {
            // everything will default to false
        }

        public UserRoles(string rolenames)
        {
            Assign(rolenames);
        }

        public UserRoles(List<string> roles)
        {
            Assign(roles);
        }

        public void Assign(List<string> roles)
        {
            IsRoot = roles.Contains("root");
            IsAdmin = roles.Contains("root") ||  roles.Contains("admin");
            IsManager = roles.Contains("manage");
            IsAuditor = roles.Contains("audit");
            IsEditor = roles.Contains("edit");
            IsViewer = roles.Contains("view");
        }

        public void Assign(string rolestr)
        {
            string[] roles = rolestr.Split(',');
            for (int i = 0; i < roles.Length; i++) roles[i] = roles[i].Trim();
            Assign(new List<string>(roles));
        }

        public bool Intersects(UserRoles other)
        {
            if (IsRoot && other.IsRoot) return true;
            if (IsAdmin && other.IsAdmin) return true;
            if (IsManager && other.IsManager) return true;
            if (IsAuditor && other.IsAuditor) return true;
            if (IsEditor && other.IsEditor) return true;
            if (IsViewer && other.IsViewer) return true;
            return false;
        }
    }
}
