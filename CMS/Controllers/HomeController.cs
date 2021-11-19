using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CMS.Models;
using Microsoft.AspNetCore.Authorization;
using CMS.Services;
using UAParser;
using DataModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace CMS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IAccountHelper m_account_helper;
        private IHostingEnvironment m_hosting_environment;

        public HomeController(IAccountHelper account_helper, IHostingEnvironment hosting_environment)
        {
            m_account_helper = account_helper;
            m_hosting_environment = hosting_environment;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Index
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Default page - this is the page of activity icons
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Index()
        {
            //var c = ControllerHelper.GetUserAgent(Request);
            //Console.WriteLine(c.UA.Family);     // => "Mobile Safari"
            //Console.WriteLine(c.UA.Major);      // => "5"
            //Console.WriteLine(c.UA.Minor);      // => "1"
            //Console.WriteLine(c.OS.Family);     // => "iOS"
            //Console.WriteLine(c.OS.Major);      // => "5"
            //Console.WriteLine(c.OS.Minor);      // => "1"
            //Console.WriteLine(c.Device.Family);
            try
            {
                UserInfo user_info = m_account_helper.GetUser(User.Identity.Name);
                using (CMSDB db = new CMSDB())
                {
                    int location_count = db.GetLocationCount();
                    int location_type_count = db.LocationTypes.Count();
                    if (location_count == 1 && location_type_count == 1)
                    {
                        if (user_info.Roles.Contains("root"))
                        {
                            return RedirectToAction("Config", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("NotInitialized");
                        }
                    }
                }
                InitializeViewData();
                if (String.IsNullOrEmpty(HttpContext.Session.GetString("SHOW_ANNOUNCEMENT")))
                {
                    HttpContext.Session.SetString("SHOW_ANNOUNCEMENT", "TRUE");
                    ViewData["SHOW_ANNOUNCEMENT"] = "TRUE";
                }
                else
                {
                    HttpContext.Session.SetString("SHOW_ANNOUNCEMENT", "FALSE");
                    ViewData["SHOW_ANNOUNCEMENT"] = "FALSE";
                }
            }
            catch (Exception)
            {

            }
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Inventory
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// This main inventory page
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Inventory()
        {
            InitializeViewData();
            return View();
        }




        ///----------------------------------------------------------------
        ///
        /// Function:       InventoryDetail
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Show detailed inventory item information
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [HttpGet("Home/InventoryDetail/{barcode}")]
        public IActionResult InventoryDetail(string barcode)
        {
            InitializeViewData();
            ViewData["Barcode"] = barcode;
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Search
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Main inventory management page
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Search()
        {
            InitializeViewData();
            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       SiteSearch
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Search full site
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult SiteSearch()
        {
            InitializeViewData();
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       StockCheck
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Audit process
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [Authorize(Roles = "admin,audit")]
        public IActionResult StockCheck()
        {
            InitializeViewData();
            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       StockCheck2
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Audit process
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [Authorize(Roles = "admin,audit")]
        public IActionResult StockCheck2()
        {
            InitializeViewData();
            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       StockCheck3
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Audit process
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [Authorize(Roles = "admin,audit")]
        public IActionResult StockCheck3()
        {
            InitializeViewData();
            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Reports
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Report generation page
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Reports()
        {
            InitializeViewData();
            return RedirectToAction("UnderConstruction");
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       About
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// About page
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        [AllowAnonymous]
        public IActionResult About()
        {
            InitializeViewData();
            BuildInformation info = new BuildInformation();
            var c = ControllerHelper.GetUserAgent(Request);

            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string migration_id = "n/a";
                    Setting announcement_setting = db.GetSetting(CMSDB.AnnouncementKey);
                    if (announcement_setting != null)
                    {
                        ViewData["Announcement"] = announcement_setting.SettingValue;
                    }
                    else ViewData["Announcement"] = "No current announcements.";
                    db.RunQuery("select max(MigrationId) as Migration from __EFMigrationsHistory", (reader) =>
                    {
                        migration_id = reader.GetString(0);
                        return false;
                    });
                    ViewData["Migration"] = migration_id;
                }
                string commit_file = System.IO.Path.Combine(m_hosting_environment.ContentRootPath, "wwwroot", "assets", "gitcommit.txt");
                string git_commit = "";
                if (System.IO.File.Exists(commit_file))
                {
                    git_commit = System.IO.File.ReadAllText(commit_file).Trim();
                }
                ViewData["GITCommit"] = git_commit;
                ViewData["ProductName"] = info.ProductName;
                ViewData["Version"] = info.VersionString;
                ViewData["BuildDate"] = String.Format("{0:MMMM dd, yyyy  hh:mm:ss tt}", info.BuildDate);

                ViewData["UAFamily"] = c.UA.Family;
                ViewData["OSFamily"] = c.OS.Family;
                ViewData["DeviceFamily"] = c.Device.Family;
            }
            catch(Exception /*ex*/)
            {

            }

            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Contact
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Contact information page
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       UnderConstruction
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Page to display for incomplete functionality
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult UnderConstruction()
        {
            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       NotInitialized
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Page to display Database Not Initialized
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult NotInitialized()
        {
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Error
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Error page
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult Error()
        {
            InitializeViewData();
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       MaterialTest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Test page for Material Design layout
        /// </summary>
        ///
        ///----------------------------------------------------------------
        [Authorize(Roles = "admin")]
        public IActionResult MaterialTest()
        {
            InitializeViewData();
            return View();
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       Test
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Test page for new functionality
        /// </summary>
        ///
        ///----------------------------------------------------------------
        //[Authorize(Roles = "admin")]
        public IActionResult Test()
        {
            InitializeViewData();
            BuildInformation info = new BuildInformation();

            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       TreeviewTest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Test page for location TreeView
        /// </summary>
        ///
        ///----------------------------------------------------------------
        //[Authorize(Roles = "admin")]
        public IActionResult TreeviewTest()
        {
            InitializeViewData();
            BuildInformation info = new BuildInformation();

            return View();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       BarcodeTest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Test page for barcoding
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult BarcodeTest()
        {
            InitializeViewData();
            BuildInformation info = new BuildInformation();

            return View();
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       UploadTest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Test page for file uploading
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public IActionResult UploadTest()
        {
            InitializeViewData();

            return View();
        }


        [AllowAnonymous]
        public IActionResult Experimental()
        {
            InitializeViewData();
            BuildInformation info = new BuildInformation();

            return View();
        }




        //#################################################################
        //
        // PRIVATE UTILITY METHODS
        //
        //#################################################################

        private void InitializeViewData()
        {
            ControllerHelper.InitializeViewData(ViewData, User.Identity.Name, m_account_helper);
        }
    }
}
