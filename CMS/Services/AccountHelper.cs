using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataModel;

namespace CMS.Services
{
    public class AccountHelper : IAccountHelper
    {
        private readonly UserManager<ApplicationUser> m_user_manager;
        private readonly RoleManager<IdentityRole> m_role_manager;
        private readonly ApplicationDbContext m_db_context;

        public string ErrorMessage { get; set; }

        public AccountHelper(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            m_db_context = context;
            m_user_manager = userManager;
            m_role_manager = roleManager;
        }

        public void EnsureDatabaseExists()
        {
            m_db_context.Database.EnsureCreated();
        }

        public void EnsureRoleExists(string name)
        {
            bool rc = m_role_manager.RoleExistsAsync(name).Result;
            if (rc == false)
            {
                IdentityRole role = new IdentityRole() { Name = name, NormalizedName = name.ToLower() };
                var foo = m_role_manager.CreateAsync(role).Result;
            }
        }

        public void EnsureUserInRole(ApplicationUser user, string role)
        {
            EnsureRoleExists(role);
            if (!m_user_manager.IsInRoleAsync(user, role).Result)
            {
                var rc = m_user_manager.AddToRoleAsync(user, role).Result;
            }
        }

        public void AddUserToRole(ApplicationUser user, string role)
        {
            EnsureRoleExists(role);
            var rc = m_user_manager.AddToRoleAsync(user, role).Result;
        }

        public void AddUserToRole(string username, string role)
        {
            ApplicationUser user = m_user_manager.FindByNameAsync(username).Result;
            if (user != null)
            {
                AddUserToRole(user, role);
            }
        }

        public List<string> GetUserRoles(string username)
        {
            if (username != null)
            {
                ApplicationUser user = m_user_manager.FindByNameAsync(username).Result;
                List<string> roles = (System.Collections.Generic.List<string>)m_user_manager.GetRolesAsync(user).Result;
                return roles;
            }
            else return new List<string>();
        }

        public (bool, string) CreateUser(UserInfo user)
        {
            ApplicationUser existing = m_user_manager.FindByNameAsync(user.UserName).Result;
            if (existing == null)
            {
                ApplicationUser new_user = new ApplicationUser()
                {
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    MiddleName = user.MiddleName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    HomeLocationID = user.HomeLocationID,
                    Position = user.Position,
                    Workplace = user.Workplace
                };
                var rc = m_user_manager.CreateAsync(new_user, user.Password).Result;
                if (rc.Succeeded)
                {
                    foreach (var role in user.Roles)
                    {
                        EnsureUserInRole(new_user, role);
                    }
                    return (true, $"Created user {user.UserName}");
                }
                else return (false, IdentityErrors(rc));
            }
            else return (false, $"User {user.UserName} already exists");
        }

        

        public UserInfo EnsureUserExists(string username, string password, string email, int home_location_id, params string[] roles)
        {
            ApplicationUser user = m_user_manager.FindByNameAsync(username).Result;
            if (user == null)
            {
                user = new ApplicationUser() { UserName = username, Email = email, HomeLocationID = home_location_id };
                var rc = m_user_manager.CreateAsync(user, password).Result;
            }
            foreach (string role in roles)
            {
                EnsureUserInRole(user, role);
            }
            return new UserInfo(user);
        }

        public async Task<List<UserInfo>> GetUsersAsync()
        {
            List<UserInfo> result = new List<UserInfo>();

            foreach (ApplicationUser user in m_user_manager.Users.OrderBy(x => x.UserName))
            {
                UserInfo info = new UserInfo(user);
                var roles = await m_user_manager.GetRolesAsync(user);
                foreach (string role in roles) info.Roles.Add(role);
                result.Add(info);
            }
            return result;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetUserAsync
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get all users whose HomeLocationID is in an array of LocationIDs
        /// </summary>
        ///
        /// <param name="location_ids">array if LocationID values</param>
        /// <returns>a Task that returns a list of UserInfo</returns>
        ///
        ///----------------------------------------------------------------
        public async Task<List<UserInfo>> GetUsersAsync(int[] location_ids)
        {
            List<UserInfo> result = new List<UserInfo>();

            foreach (ApplicationUser user in m_user_manager.Users.Where(x => location_ids.Contains(x.HomeLocationID)).OrderBy(x => x.UserName))
            {
                UserInfo info = new UserInfo(user);
                var roles = await m_user_manager.GetRolesAsync(user);
                foreach (string role in roles) info.Roles.Add(role);
                result.Add(info);
            }
            return result;
        }

        public async Task<UserInfo> GetUserAsync(string login)
        {
            UserInfo result = null;

            ApplicationUser user = m_user_manager.Users.FirstOrDefault(x => x.UserName == login);
            if (user != null)
            {
                result = new UserInfo(user);
                var roles = await m_user_manager.GetRolesAsync(user);
                foreach (string role in roles) result.Roles.Add(role);
            }
            return result;
        }

        public UserInfo GetUser(string login)
        {
            return GetUserAsync(login).Result;
        }

        public async Task<(bool, string)> UpdateUserAsync(UserInfo user_info, bool allow_new_users = false)
        {

            ApplicationUser user = await m_user_manager.FindByNameAsync(user_info.UserName);
            if (user != null)
            {
                List<string> current_roles = m_user_manager.GetRolesAsync(user).Result.ToList();
                int current_home_id = user.HomeLocationID;
                user.Email = user_info.Email;
                user.FirstName = user_info.FirstName;
                user.LastName = user_info.LastName;
                user.MiddleName = user_info.MiddleName;
                user.Position = user_info.Position;
                user.Workplace = user_info.Workplace;
                user.PhoneNumber = user_info.PhoneNumber;
                user.HomeLocationID = user_info.HomeLocationID;
                var rc = await m_user_manager.UpdateAsync(user);
                if (rc.Succeeded)
                {
                    // if user_info.Roles == null, make no changes to roles
                    if (user_info.Roles != null)
                    {
                        List<string> added = new List<string>();
                        foreach (string role in user_info.Roles)
                        {
                            if (!current_roles.Contains(role))
                            {
                                added.Add(role);
                            }
                        }
                        if (added.Count > 0) await m_user_manager.AddToRolesAsync(user, added);
                        List<string> doomed = new List<string>();
                        foreach (string role in current_roles)
                        {
                            if (!user_info.Roles.Contains(role))
                            {
                                doomed.Add(role);
                            }
                        }
                        if (doomed.Count > 0) await m_user_manager.RemoveFromRolesAsync(user, doomed);
                    }                    
                }
                return (rc.Succeeded, IdentityErrors(rc));
            }
            else return (false, "User not found");
        }

        public (bool, string) UpdateUser(UserInfo user_info, bool allow_new_users = false)
        {
            return UpdateUserAsync(user_info, allow_new_users).Result;
        }

        public (bool, string) UpdateUsers(List<UserInfo> user_info, bool allow_new_users = false)
        {
            bool success = true;
            List<string> errors = new List<string>();
            foreach (UserInfo user in user_info)
            {
                (bool rc, string err) = UpdateUser(user, allow_new_users);
                success = success && rc;
                if (!rc) errors.Add(err);
            }
            if (success) return (success, null);
            else return (false, String.Join('\n', errors));
        }


        public List<UserInfo> GetUsers()
        {
            return GetUsersAsync().Result;
        }

        public List<UserInfo> GetUsers(int[] location_ids)
        {
            return GetUsersAsync(location_ids).Result;
        }

        public (bool, string) DeleteUser(string username)
        {
            ApplicationUser user = m_user_manager.FindByNameAsync(username).Result;
            if (user != null)
            {
                IdentityResult rc = m_user_manager.DeleteAsync(user).Result;
                if (rc.Succeeded) return (true, "");
                else
                {
                    List<string> error_messages = new List<string>();
                    foreach (var err in rc.Errors)
                    {
                        error_messages.Add(err.Description);
                    }
                    ErrorMessage = String.Join("\n", error_messages);
                    return (false, ErrorMessage);
                }
            }
            else
            {
                ErrorMessage = $"User {username} does not exist";
                return (false, ErrorMessage);
            }
        }


        public async Task<(bool, string)> ChangePasswordAsync(string username, string newpassword)
        {
            ApplicationUser user = await m_user_manager.FindByNameAsync(username);
            IdentityResult rc = await m_user_manager.RemovePasswordAsync(user);
            if (rc.Succeeded)
            {
                rc = await m_user_manager.AddPasswordAsync(user, newpassword);
                if (rc.Succeeded) return (true, null);
                else return (false, IdentityErrors(rc));
            }
            else return (false, IdentityErrors(rc));
        }

        public (bool, string) ChangePassword(string username, string newpassword)
        {
            return ChangePasswordAsync(username, newpassword).Result;
        }

        public static List<string> IdentityErrorMessages(IdentityResult rc)
        {
            List<string> result = new List<string>();
            foreach (var err in rc.Errors)
            {
                result.Add(err.Description);
            }
            return result;
        }

        public static string IdentityErrors(IdentityResult rc, string separator = "\n")
        {
            List<string> error_messages = IdentityErrorMessages(rc);
            if (error_messages.Count > 0) return null;
            else return String.Join(separator, error_messages);
        }

    }

    public class UserInfo
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Roles { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Position { get; set; }
        public string Workplace { get; set; }
        public string PhoneNumber { get; set; }
        public int HomeLocationID { get; set; }
        public string HomeLocation { get; set; }
        public bool IsChanged { get; set; }
        public int PictureAttachmentID { get; set; }

        public UserInfo()
        {
            Roles = new List<string>();
        }

        public UserInfo(ApplicationUser user)
        {
            Roles = new List<string>();
            UserName = user.UserName;
            Email = user.Email;
            LastName = user.LastName;
            FirstName = user.FirstName;
            MiddleName = user.MiddleName;
            Position = user.Position;
            Workplace = user.Workplace;
            PhoneNumber = user.PhoneNumber;
            HomeLocationID = user.HomeLocationID;
        }

        public void GetHomeLocationNames(CMSDB db)
        {
            if (HomeLocationID > 0)
            {
                HomeLocation = db.GetFullLocationName(HomeLocationID);
            }
        }
    }

}
