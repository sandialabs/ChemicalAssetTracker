using CMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services
{
    public interface IAccountHelper
    {
        void EnsureDatabaseExists();
        void EnsureRoleExists(string name);
        void AddUserToRole(ApplicationUser user, string role);
        void AddUserToRole(string username, string role);
        (bool, string) CreateUser(UserInfo user);
        UserInfo EnsureUserExists(string username, string password, string email, int home_location_id, params string[] roles);
        Task<List<UserInfo>> GetUsersAsync();
        List<UserInfo> GetUsers();
        List<UserInfo> GetUsers(int[] location_ids);
        Task<(bool, string)> ChangePasswordAsync(string username, string new_password);
        (bool, string) ChangePassword(string username, string new_password);
        Task<(bool, string)> UpdateUserAsync(UserInfo user_info, bool allow_new_users);
        (bool, string) UpdateUser(UserInfo user_info, bool allow_new_users);
        (bool, string) UpdateUsers(List<UserInfo> user_info, bool allow_new_users);
        (bool, string) DeleteUser(string username);
        List<string> GetUserRoles(string username);
        Task<UserInfo> GetUserAsync(string login);
        UserInfo GetUser(string login);
    }
}


