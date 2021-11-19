using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMS.Models;
using CMS.Services;
using Common;
using DataModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers
{

    [Authorize(Roles = "admin,manage")]
    public class AdminController : Controller
    {
        // injected member variables
        private readonly UserManager<ApplicationUser> m_user_manager;
        private readonly IAccountHelper m_account_helper;
        private IHostingEnvironment m_hosting_environment;

        private static string s_temp_upload_directory = null;

        public AdminController(
            UserManager<ApplicationUser> user_manager,
            IAccountHelper account_helper,
            IHostingEnvironment hosting_environment
            )
        {
            m_user_manager = user_manager;
            m_account_helper = account_helper;
            m_hosting_environment = hosting_environment;
            if (s_temp_upload_directory == null)
            {
                using (CMSDB db = new CMSDB())
                {
                    s_temp_upload_directory = db.GetSetting(CMSDB.TempDirKey).SettingValue;
                }
            }
        }

        public IActionResult Index()
        {
            var user = m_account_helper.GetUser(User.Identity.Name);
            UserRoles myroles = new UserRoles(user.Roles);
            InitializeViewData();
            List<AdminTool> tools = AdminTool.EnabledTools(myroles);
            ViewData["Tools"] = tools;
            return View();
        }


        public IActionResult APITest()
        {
            InitializeViewData();
            return View();
        }

        public IActionResult Owners()
        {
            InitializeViewData();
            return View();
        }

        public IActionResult Groups()
        {
            InitializeViewData();
            return View();
        }


        //public IActionResult Index()
        //{
        //    ControllerHelper.InitializeViewData(ViewData, User.Identity.Name);
        //    return View();
        //}

        public IActionResult UserManagement()
        {
            InitializeViewData();
            return View();
        }

        public IActionResult UserManagement2()
        {
            InitializeViewData();
            return View();
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Settings
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Admin, manager page to edit system settings
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [Authorize(Roles = "root")]
        public IActionResult Settings()
        {
            InitializeViewData();
            return View();
        }


        [AllowAnonymous]
        [HttpGet("Admin/SeedUsers/{rootpswd}")]
        public IActionResult SeedUsers(string rootpswd)
        {
            InitializeViewData();
            int home_location_id = 1;
            using (CMSDB db = new CMSDB())
            {
                StorageLocation root = db.GetRootLocation();
                if (root != null) home_location_id = root.LocationID;
            }
            m_account_helper.EnsureRoleExists("root");
            m_account_helper.EnsureRoleExists("admin");
            m_account_helper.EnsureRoleExists("edit");
            m_account_helper.EnsureRoleExists("view");
            m_account_helper.EnsureRoleExists("manage");
            m_account_helper.EnsureRoleExists("audit");
            m_account_helper.EnsureUserExists("root", rootpswd, "wrhumph@sandia.gov", home_location_id, "root", "admin");
            ViewData["Message"] = "User roles and user root have been created.";
            return View();
        }

        [AllowAnonymous]
        public IActionResult SeedStudents()
        {
            InitializeViewData();
            using (CMSDB db = new CMSDB())
            {
                List<StorageLocation> universities = db.Locations.FindByLevel(1);
                List<string> logins = new List<string>();
                string[] names = { "admin", "manager" };
                string[] roles = { "admin", "manage" };
                int locnum = 0;
                for (int i = 1;  i <= 30;  i++)
                {
                    StorageLocation location = universities[locnum];
                    locnum = (locnum + 1) % universities.Count;
                    for (int j = 0; j < names.Length;  j++)
                    {
                        string name = names[j];
                        string role = roles[j];
                        int locid = location.LocationID;
                        string login = String.Format("{0}{1:000}", name, i);
                        m_account_helper.EnsureUserExists(login, login, $"{login}@{location.Name}.edu", locid, role);
                        logins.Add($"{login}:{location.Name}");
                    }
                }
                ViewData["Message"] = $"Created {logins.Count} accounts";
                ViewData["Logins"] = logins;
            }
            return View();
        }


        [HttpGet]
        public IActionResult EditUserLocations()
        {
            InitializeViewData();
            return View();
        }

        [HttpGet]
        public IActionResult LocationPermissions()
        {
            InitializeViewData();
            return View();
        }

        [HttpGet]
        public IActionResult LocationManagement()
        {
            InitializeViewData();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult ViewLogs()
        {
            InitializeViewData();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditLocations()
        {
            InitializeViewData();
            return View();
        }



        [HttpGet]
        public IActionResult ImportDatabase()
        {
            InitializeViewData();
            return View();
        }

        public IActionResult ImportSDSFiles()
        {
            InitializeViewData();
            return View();
        }

        public IActionResult Stats()
        {
            InitializeViewData();

            return View();

        }

        [Authorize(Roles = "root")]
        public IActionResult Config()
        {
            string error_message = "Database has not been initialized.  Please contact your system administrator.";
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    List<LocationType> location_types = db.LocationTypes.ToList();
                    int location_count = db.GetLocationCount();
                    ViewData["location_count"] = location_count;
                    ViewData["location_type_count"] = location_types.Count;
                    ViewData["institution"] = db.GetStringSetting("Institution", "Institution");
                    List<UserInfo> users = m_account_helper.GetUsers();
                    error_message = "";
                }
                InitializeViewData();
            }
            catch
            {
            }
            ViewData["status"] = error_message;
            return View();

        }







        //#################################################################
        //
        // APIs
        //
        //#################################################################

        ///----------------------------------------------------------------
        ///
        /// Function:       AddUsers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP POST API: http://.../Admin/AddUsers
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost]
        public AjaxResult AddUsers([FromBody] List<UserInfo> users)
        {
            AjaxResult result = new AjaxResult("DatabaseController.AddUsers");
            InitializeViewData();
            List<string> errors = new List<string>();
            UserInfo current_user = null;
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string mylogin = User.Identity.Name;
                    UserInfo this_user = m_account_helper.GetUser(mylogin);
                    int this_user_location_level = db.Locations[this_user.HomeLocationID].LocationLevel;
                    bool this_user_is_root = this_user.Roles.Contains("root");

                    foreach (UserInfo user in users)
                    {
                        current_user = user;
                        int user_location_level = db.Locations[user.HomeLocationID].LocationLevel;
                        if (this_user_is_root  ||  this_user_location_level < user_location_level)
                        {
                            (bool rc, string msg) = m_account_helper.CreateUser(user);
                            if (rc) 
                            {
                                ControllerHelper.LogInfo(User.Identity.Name, "authentication", $"User {user.UserName} added");
                            }
                            else {
                                errors.Add(msg);
                                ControllerHelper.LogError(User.Identity.Name, "authentication", msg);
                            }
                        }
                        else
                        {
                            string msg = $"Unable to create user {user.UserName} - not below your location.";
                            errors.Add(msg);
                            ControllerHelper.LogError(User.Identity.Name, "authentication", msg);
                        }
                    }
                }
                if (errors.Count > 0) result.Fail(String.Join(" ", errors));
                else result.Succeed($"Users successfully added");
            }
            catch (Exception ex)
            {
                if (current_user != null) ControllerHelper.LogError(User.Identity.Name, "authentication", $"Attempt to add new user {current_user.UserName} failed. {result.Message}");
                else ControllerHelper.LogError(User.Identity.Name, "authentication", $"Attempt to add a new user failed. {result.Message}");
                result.Fail(ex);
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       SaveUsers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP POST API: http://.../Admin/SaveUsers
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost]
        public AjaxResult SaveUsers([FromBody] List<UserInfo> users)
        {
            InitializeViewData();
            AjaxResult result = new AjaxResult("DatabaseController.SaveUsers");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string mylogin = User.Identity.Name;
                    UserInfo this_user = m_account_helper.GetUser(mylogin);
                    int this_user_location_level = db.Locations[this_user.HomeLocationID].LocationLevel;
                    bool this_user_is_root = this_user.Roles.Contains("root");

                    foreach (var user in users)
                    {
                        int user_location_level = db.Locations[user.HomeLocationID].LocationLevel;
                        if (this_user_is_root || this_user_location_level < user_location_level)
                        {
                            m_account_helper.UpdateUser(user, true);
                            result.Succeed("User successfully updated");
                        }
                        else throw new Exception($"Unable to create/modify user ${user.UserName} - not in location below current user.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        [HttpDelete]
        public AjaxResult DeleteUser()
        {
            InitializeViewData();
            string id = Request.Query["id"];
            AjaxResult result = new AjaxResult("AdminController.DeleteUser");
            if (User.Identity.Name == id) throw new Exception("Cannot delete the current user.");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string mylogin = User.Identity.Name;
                    UserInfo this_user = m_account_helper.GetUser(mylogin);
                    int this_user_location_level = db.Locations[this_user.HomeLocationID].LocationLevel;
                    bool this_user_is_root = this_user.Roles.Contains("root");

                    UserInfo doomed = m_account_helper.GetUser(id);
                    int doomed_location_level = db.Locations[doomed.HomeLocationID].LocationLevel;

                    if (this_user_is_root || this_user_location_level < doomed_location_level)
                    {
                        (bool rc, string err_msg) = m_account_helper.DeleteUser(id);
                        if (rc) {
                            string msg = $"User {id} deleted.";
                            result.Succeed(msg);
                            ControllerHelper.LogInfo(User.Identity.Name, "authentication", msg);
                        }
                        else {
                            result.Fail(err_msg);
                            ControllerHelper.LogError(User.Identity.Name, "authentication", err_msg);
                        }
                    }
                    else {
                        result.Fail("You cannot delete a user at your own location or higher");
                        ControllerHelper.LogError(User.Identity.Name, "authentication", $"Invalid attempt to delete user {id}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }

            return result;
        }

        [HttpPost]
        public AjaxResult ChangePassword([FromBody] UserInfo userinfo)
        {
            InitializeViewData();
            AjaxResult result = new AjaxResult("AdminController.DeleteUser");
            {
                try
                {
                    (bool rc, string err_msg) = m_account_helper.ChangePassword(userinfo.UserName, userinfo.Password);
                    if (rc)
                    {
                        result.Succeed($"The password for user {userinfo.UserName} has been changed.");
                        ControllerHelper.LogInfo(User.Identity.Name, "authentication", $"The password for user {userinfo.UserName} has been changed.");
                    }
                    else
                    {
                        result.Fail(err_msg);
                        ControllerHelper.LogError(User.Identity.Name, "authentication", $"An attempt to change password for {userinfo.UserName} failed.");
                    }
                }
                catch (Exception ex)
                {
                    result.Fail(ex);
                }
            }

            return result;
        }




        ///----------------------------------------------------------------
        ///
        /// Function:       GetUsers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../Admin/GetUsers
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet]
        public AjaxResult GetUsers()
        {
            InitializeViewData();
            AjaxResult result = new AjaxResult("DatabaseController.GetUsers");
            try
            {
                List<UserInfo> users = m_account_helper.GetUsers();
                result.Succeed($"{users.Count} users successfully read", "Users", users);
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       MyUsers
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get all the users in the user's HomeLocation subtree
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet]
        public AjaxResult MyUsers()
        {
            InitializeViewData();
            AjaxResult result = new AjaxResult("AdminController.MyUsers");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string mylogin = User.Identity.Name;
                    UserInfo this_user = m_account_helper.GetUser(mylogin);
                    int my_location_id = this_user.HomeLocationID;
                    StorageLocation my_location = db.Locations[my_location_id];
                    LocationSubtree subtree = db.GetSubtree(this_user.HomeLocationID, 99);
                    List<StorageLocation> my_locations = subtree.Locations.OrderBy(x => x.LocationLevel).ToList();
                    if (!this_user.Roles.Contains("root")) my_locations.Remove(my_location);
                    int[] my_location_ids = my_locations.Select(x => x.LocationID).ToArray();
                    List<UserInfo> users = m_account_helper.GetUsers(my_location_ids).OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList();
                    users.ForEach(x =>
                    {
                        x.GetHomeLocationNames(db);
                        x.PictureAttachmentID = db.GetAttachmentID(x.UserName, "photo");
                    });
                    result.Set("ThisUser", this_user);
                    result.Succeed($"{users.Count} users successfully read", "Users", users);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       UploadAttachment
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP PUT: admin/uploadattachment - upload an user attachment
        /// </summary>
        ///
        /// <param name="files">a list with informaton on the file to upload</param>
        /// <param name="owner">the login of the user associated with the upload</param>
        /// <param name="tag">the type of attachment ("photo" or "other")</param>
        /// <param name="description">a description string for the attachment</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPut("admin/uploadattachment")]
        public AjaxResult UploadAttachment(List<IFormFile> files, string owner, string tag, string description)
        {
            AjaxResult result = new AjaxResult("AdminController.UploadAttachment");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    if (owner == null || tag == null) throw new Exception("No value for owner and/or tag");
                    UserInfo user = m_account_helper.GetUser(owner);
                    if (user == null) throw new Exception($"Unknown user: {owner}");
                    if (files.Count == 0) result.Fail("No files in request.");
                    if (files.Count > 1) result.Fail("Too many files in upload");
                    if (files.Count == 1)
                    {
                        IFormFile file = files[0];
                        string tempfilepath = Path.GetTempFileName();

                        using (FileStream stream = System.IO.File.Create(tempfilepath))
                        {
                            file.CopyTo(stream);
                        }
                        byte[] data = System.IO.File.ReadAllBytes(tempfilepath);
                        db.SaveAttachment(User.Identity.Name, owner, tag, description ?? "", data);
                        result.Succeed("SUCCESS");
                    }
                    else
                    {
                        result.Fail("No files in request.");
                    }
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    if (ex is MySql.Data.MySqlClient.MySqlException) break;
                }
                result.Fail(ex.Message);
            }
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ImportDatabase
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP PUT: /admin/validateimportdatabase - validate an import database or Excel file
        /// </summary>
        ///
        /// <param name="files">a list with informaton on the file to upload</param>
        /// <param name="tag">a string containing the parent LocationID</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------

        [HttpPut("admin/validateimportdatabase")]
        public AjaxResult ValidateImportDatabase(List<IFormFile> files, string tag)
        {
            AjaxResult result = new AjaxResult("AdminController.ValidateImportDatabase");
            string tempfilepath = null;
            string import_filename;
            try
            {
                if (files.Count == 0) result.Fail("No files in request.");
                if (files.Count > 1) result.Fail("Too many files in request");
                if (files.Count == 1)
                {
                    int location_id = 0;
                    if (!Int32.TryParse(tag, out location_id))
                    {
                        result.Fail($"LocationID is not an int: \"{tag}\"");
                    }
                    else
                    {
                        IFormFile formFile = files[0];

                        import_filename = formFile.FileName;
                        tempfilepath = CreateTempFilePath(import_filename);
                        result.Set("tempfilepath", tempfilepath);
                        using (FileStream stream = System.IO.File.Create(tempfilepath))
                        {
                            formFile.CopyTo(stream);
                        }

                        ImportLib.Importer importer = new ImportLib.Importer((msg) => { Console.WriteLine(msg); });
                        ImportLib.DatabaseValidationResult validation_result = importer.ValidateImport(tempfilepath, location_id);
                        validation_result.TempFilePath = tempfilepath;
                        result.Set("validationresult", validation_result);
                        if (validation_result.Success)
                        {
                            string msg = $"Validation of import database \"{import_filename}\" completed successfully.";
                            result.Succeed(msg);
                        }
                        else
                        {
                            string msg = $"Validation of import database \"{import_filename}\" failed.";
                            result.Fail(msg);
                        }
                    }
                }
                DeleteTempFiles();
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ImportDatabase
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP PUT: /admin/uploadattachment - upload an user attachment
        /// </summary>
        ///
        /// <param name="files">a list with informaton on the file to upload</param>
        /// <param name="tag">a string containing the parent LocationID</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------

        [HttpPut("admin/importdatabase")]
        public AjaxResult ImportDatabase(List<IFormFile> files, string tag)
        {
            AjaxResult result = new AjaxResult("AdminController.ImportDatabase");
            string tempfilepath = null;
            string import_filename = null;
            try
            {
                if (files.Count == 0) result.Fail("No files in request.");
                if (files.Count > 1) result.Fail("Too many files in request");
                if (files.Count == 1)
                {
                    int location_id = 0;
                    if (!Int32.TryParse(tag, out location_id))
                    {
                        result.Fail($"LocationID is not an int: \"{tag}\"");
                    }
                    else
                    {
                        IFormFile formFile = files[0];

                        import_filename = formFile.FileName;
                        tempfilepath = CreateTempFilePath(import_filename);
                        using (FileStream stream = System.IO.File.Create(tempfilepath))
                        {
                            formFile.CopyTo(stream);
                        }

                        ImportLib.Importer importer = new ImportLib.Importer((msg) => { Console.WriteLine(msg); });
                        string login = User.Identity.Name;

                        var import_result = importer.Import(tempfilepath, location_id, User.Identity.Name);
                        import_result.TempFilePath = tempfilepath;

                        if (import_result.Success)
                        {
                            string msg = $"Successfully imported {import_result.ItemsImported} items from CMS database \"{import_filename}\" into location {import_result.TargetLocation}";
                            CMSDB.LogInfoMessage(User.Identity.Name, "Import", msg);
                            result.Set("messages", import_result.Messages);
                            result.Succeed(msg);
                        }
                        else
                        {
                            string msg = $"Import failed from CMS database \"{import_filename}\" into location {import_result.TargetLocation}";
                            CMSDB.LogWarningMessage(User.Identity.Name, "Import", msg);
                            result.Succeed(msg);
                        }
                    }
                }
                DeleteTempFiles();
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ImportDatabase
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP PUT: /admin/importdatabase - import a CMS database or Excel file
        /// </summary>
        ///
        /// <param name="validation_result">a DatabaseValidataionResult from a previous validation</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------

        [HttpPost("admin/importdatabase")]
        public AjaxResult ImportDatabase([FromBody] ImportLib.DatabaseValidationResult validation_result)
        {
            AjaxResult result = new AjaxResult("AdminController.ImportDatabase");
            string import_filename = validation_result.TempFilePath;
            try
            {
                int location_id = validation_result.TargetLocationID;
                ImportLib.Importer importer = new ImportLib.Importer((msg) => { Console.WriteLine(msg); });
                string login = User.Identity.Name;

                var import_result = importer.Import(import_filename, location_id, User.Identity.Name);
                if (import_result.Success)
                {
                    string msg = $"Successfully imported {import_result.ItemsImported} items from CMS database \"{import_filename}\" into location {import_result.TargetLocation}";
                    result.Succeed(msg);
                    result.Set("messages", import_result.Messages);
                    CMSDB.LogInfoMessage(User.Identity.Name, "Import", msg);
                }
                else
                {
                    string msg = $"Import opertation failed: " + result.Message;
                    result.Fail(msg);
                    CMSDB.LogWarningMessage(User.Identity.Name, "Import", msg);
                }
                // clean up after ourselve
                DeleteTempFiles();
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       UploadSDS
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP PUT: admin/uploadsds - upload an user attachment
        /// </summary>
        ///
        /// <param name="files">a list with informaton on the file to upload</param>
        /// <param name="tag">the CAS number</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPut("admin/uploadsds")]
        public AjaxResult UploadSDS(List<IFormFile> files, string tag)
        {
            AjaxResult result = new AjaxResult("AdminController.UploadSDS");
            try
            {
                string sds_directory = System.IO.Path.Combine(m_hosting_environment.WebRootPath, "SDS");
                foreach (IFormFile file in files)
                {
                    string filename = tag + ".pdf";
                    string prefix = tag;
                    string suffix = ".pdf";
                    string filepath = System.IO.Path.Combine(sds_directory, filename);
                    int tries = 0;
                    while (tries < 100  && System.IO.File.Exists(filepath))
                    {
                        tries += 1;
                        filename = $"{prefix}.{tries:00}{suffix}";
                        filepath = System.IO.Path.Combine(sds_directory, filename);
                    }
                    if (tries == 100) throw new Exception("Too many SDS files");

                    using (FileStream stream = System.IO.File.Create(filepath))
                    {
                        file.CopyTo(stream);
                    }
                    result.Succeed($"SDS file saved as {filename}");
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    if (ex is MySql.Data.MySqlClient.MySqlException) break;
                }
                result.Fail(ex.Message);
            }
            return result;
        }





        public IActionResult Status(OperationStatus status)
        {
            InitializeViewData();
            Console.WriteLine("*** In Status");
            ViewData["Operation"] = status.Operation;
            ViewData["Message"] = status.Message;
            ViewData["ReturnURL"] = status.ReturnUrl;
            ViewData["Type"] = status.Type;

            return View();
        }

        public IActionResult Reports()
        {
            InitializeViewData();
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetUserHomeLocations
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../Admin/GetUserHomeLocations
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet]
        public AjaxResult GetUserHomeLocations()
        {
            InitializeViewData();
            AjaxResult result = new AjaxResult("DatabaseController.GetUserHomeLocations");
            try
            {
                Dictionary<int, StorageLocation> home_locations = new Dictionary<int, StorageLocation>();
                // get a list of all the users in the system
                List<UserInfo> users = m_account_helper.GetUsers();
                using (CMSDB db = new CMSDB())
                {
                    // get all the storage locations that are the parent of some user's HomeLocation
                    users.ForEach(user =>
                    {
                        AddToHomeLocations(user.HomeLocationID, home_locations, db);
                    });
                }
                List<StorageLocation> locations = home_locations.Values.OrderBy(x => x.LocationLevel).ThenBy(x => x.Name).ToList();
                UserLocationTree tree = new UserLocationTree(locations[0]);
                foreach (var loc in locations)
                {
                    if (loc.ParentID > 0) tree.Add(loc);
                }
                foreach (var u in users) tree.Add(u);
                result.Set("Users", users);
                result.Set("HomeLocations", locations);
                result.Succeed("SUCCESS", "UserHomeLocation", tree);
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        private void AddToHomeLocations(int location_id, Dictionary<int, StorageLocation> home_locations, CMSDB db)
        {
            if (!home_locations.ContainsKey(location_id))
            {
                StorageLocation location = db.Locations[location_id];
                home_locations[location_id] = location;
                if (location.ParentID > 0)
                {
                    AddToHomeLocations(location.ParentID, home_locations, db);
                }
            }
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetSDSGiles
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../Admin/GetSDSFiles
        /// </summary>
        ///
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet]
        public AjaxResult GetSDSFiles()
        {
            AjaxResult result = new AjaxResult("AdminController.GetSDSFiles");
            try
            {
                string sds_directory = System.IO.Path.Combine(m_hosting_environment.WebRootPath, "SDS");
                string[] files = System.IO.Directory.GetFiles(sds_directory);
                List<SDSFile> sdsfiles = new List<SDSFile>();
                foreach (string filepath in files)
                {
                    SDSFile sds = new SDSFile(filepath);
                    if (sds.IsValid) sdsfiles.Add(sds);
                }
                List<SDSFile> sorted = sdsfiles.OrderBy(x => x.FileName).ToList();

                result.Succeed("SUCCESS", "files", sorted);
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        private void InitializeViewData()
        {
            ControllerHelper.InitializeViewData(ViewData, User.Identity.Name, m_account_helper);
        }

        private string CreateTempFilePath(string filename)
        {
            string tempdir = s_temp_upload_directory;  // Path.GetTempPath();
            string temp_filename = "cat_" + filename;
            string result = Path.Combine(tempdir, temp_filename);
            return result;
        }

        private void DeleteTempFiles()
        {
            string tempdir = s_temp_upload_directory; // Path.GetTempPath();
            DateTime now = DateTime.Now;
            foreach (string file in Directory.EnumerateFiles(tempdir, "cat_*"))
            {
                var creation_time = System.IO.File.GetCreationTimeUtc(file);
                if ((now - creation_time).TotalMinutes > 60)
                {
                    System.IO.File.Delete(file);
                }
            }
        }


        private void Banner(params string[] lines)
        {
            string divider = new String('#', 72);
            Console.WriteLine(" ");
            Console.WriteLine(divider);
            Console.WriteLine("#");
            foreach (string line in lines)
            {
                Console.WriteLine("# " + line);
            }
            Console.WriteLine("#");
            Console.WriteLine(divider);
            Console.WriteLine(" ");
        }
    }

    public class UserLocationTree
    {
        public int ID { get; set; }
        public bool IsLeaf { get; set; }
        public bool IsInner { get { return !IsLeaf; } }
        public string Name { get; set; }    // will be the user's login if this is a leaf, or else the location name
        public StorageLocation Location { get; set; }
        public UserInfo UserData { get; set; }
        public List<UserLocationTree> Children { get; set; }

        public UserLocationTree(StorageLocation location)
        {
            ID = location.LocationID;
            IsLeaf = false;
            Name = location.Name;
            Location = location;
            Children = new List<UserLocationTree>();
        }

        public UserLocationTree(UserInfo userinfo, int id)
        {
            ID = id;
            IsLeaf = true;
            Name = userinfo.UserName;
            UserData = userinfo;
            Children = new List<UserLocationTree>();
        }

        public void AddChild(UserLocationTree child)
        {
            Children.Add(child);
        }

        public UserLocationTree Find(int location_id)
        {
            if (IsInner)
            {
                if (ID == location_id) return this;
                foreach (UserLocationTree child in Children)
                {
                    var found = child.Find(location_id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        // this method only works if called in order of LocationLevel
        public bool Add(StorageLocation location)
        {
            int parent_id = location.ParentID;
            if (!IsLeaf && ID == parent_id)
            {
                AddChild(new UserLocationTree(location));
                return true;
            }
            else if (Children != null)
            {
                foreach (UserLocationTree subtree in Children)
                {
                    if (subtree.Add(location)) return true;
                }
            }
            return false;
        }

        public bool Add(UserInfo user)
        {
            int parent_id = user.HomeLocationID;
            if (!IsLeaf && ID == parent_id)
            {
                AddChild(new UserLocationTree(user, 0));
                return true;
            }
            else if (Children != null)
            {
                foreach (UserLocationTree subtree in Children)
                {
                    if (subtree.Add(user)) return true;
                }
            }
            return false;
        }
    }

    public class OperationStatus
    {
        public string Operation { get; set; }
        public string Message { get; set; }
        public string ReturnUrl { get; set; }
        public string Type { get; set; }
    }

    public class AdminTool
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public string Description { get; set; }
        public string Roles { get; set; }

        public AdminTool(string name, string url, string description)
        {
            Name = name;
            URL = url;
            Description = description;
        }

        static public List<AdminTool> EnabledTools(UserRoles myroles)
        {
            List<AdminTool> tools = new List<AdminTool>();

            if (myroles.IsRoot)
            {
                tools.Add(new AdminTool("System Settings", "/Admin/Settings", "Manage system settings"));
                tools.Add(new AdminTool("Report Management", "/Admin/Reports", "Create/Delete/Edit reports"));
            }
            if (myroles.IsAdmin)
            {
                tools.Add(new AdminTool("Owners", "/Admin/Owners", "Create, delete, and update owners"));
                tools.Add(new AdminTool("Groups", "/Admin/Groups", "Manage storage groups"));
                //tools.Add(new AdminTool("System Settings", "/Admin/Settings", "Manage system settings"));
                tools.Add(new AdminTool("Users", "/Admin/UserManagement", "Manage users"));
                tools.Add(new AdminTool("Locations", "/Admin/LocationManagement", "Manage locations and user access to locations"));
                tools.Add(new AdminTool("Import", "/Admin/ImportDatabase", "Import CMS database"));
                //tools.Add(new AdminTool("Report Management", "/Admin/Reports", "Create/Delete/Edit reports"));
                tools.Add(new AdminTool("SDS File Upload", "/Admin/ImportSDSFiles", "Upload new SDS files."));
                tools.Add(new AdminTool("Performance", "/Admin/Stats", "Server performance statistics"));
                tools.Add(new AdminTool("Logs", "/Admin/ViewLogs", "View system logs"));
            }
            else if (myroles.IsManager)
            {
                tools.Add(new AdminTool("Owners", "/Admin/Owners", "Create, delete, and update owners"));
                tools.Add(new AdminTool("Groups", "/Admin/Groups", "Manage storage groups"));
                tools.Add(new AdminTool("Users", "/Admin/UserManagement", "Manage users"));
                tools.Add(new AdminTool("Import", "/Admin/ImportDatabase", "Import CMS database"));
                tools.Add(new AdminTool("SDS File Upload", "/Admin/ImportSDSFiles", "Upload new SDS files."));
            }

            return tools;
        }
    }

    public class SDSFile
    {
        public string FileName { get; set; }
        public string CASNumber { get; set; }
        public string Suffix { get; set; }
        public bool IsValid { get { return (CASNumber != null); } }

        private static Regex s_regex = new Regex(@"^(\d+-\d+-\d+)(\D?.*).pdf$", RegexOptions.IgnoreCase);

        public SDSFile()
        {

        }

        public SDSFile(string filepath)
        {
            FileName = System.IO.Path.GetFileName(filepath);
            Match m = s_regex.Match(FileName);
            if (m.Success)
            {
                CASNumber = m.Groups[1].Value;
                Suffix = m.Groups[2].Value;
            }
        }

    }
}

