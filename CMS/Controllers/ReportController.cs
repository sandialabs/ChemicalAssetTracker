using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CMS.Models;
using CMS.Services;
using Microsoft.EntityFrameworkCore;
using Common;
using DataModel;

namespace CMS.Controllers
{
    [Route("Report")]
    [Authorize]
    public class ReportController : Controller
    {
        IAccountHelper m_account_helper;

        public ReportController(IAccountHelper account_helper)
        {
            m_account_helper = account_helper;
        }

        [Route("Index")]
        public IActionResult Index()
        {
            ControllerHelper.InitializeViewData(ViewData, User.Identity.Name, m_account_helper);
            return View();
        }
        
        //#################################################################
        //
        // APIs
        //
        //#################################################################

        ///----------------------------------------------------------------
        ///
        /// Function:       GetReportData
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Generate data for a report
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost]
        public AjaxResult GetReportData([FromBody] OldReportRequest report_request)
        {
            UserRoles permissions = ControllerHelper.InitializeViewData(ViewData, User.Identity.Name, m_account_helper);
            AjaxResult result = new AjaxResult("ReportController.GetReportData");
            {
                try
                {
                    using (var db = new CMSDB())
                    {
                        switch (report_request.ReportID)
                        {
                            case "activity-log":
                                result.Fail("Not implemented");
                                break;
                            case "inventory-by-location":
                                result.Succeed("SUCCESS", "QueryResult", InventoryReportData(report_request, db));
                                break;
                            case "inventory-by-chemical":
                                result.Succeed("SUCCESS", "QueryResult", InventoryReportData(report_request, db));
                                break;
                            case "stock-check":
                                if (permissions.CanAudit) result.Succeed("SUCCESS", "QueryResult", StockCheckReportData(db));
                                else result.Fail("You do not have permission to view this report");
                                break;
                            case "users":
                                if (permissions.CanManageUsers) result.Succeed("SUCCESS", "QueryResult", GetUserData());
                                else result.Fail("You do not have permission to view this report");
                                break;
                            default:
                                result.Fail($"Invalid report id: {report_request.ReportID}");
                                break;
                        }
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
        /// Function:       GetReports
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET: http://.../Report/getreports
        /// </summary>
        ///
        /// <returns>
        /// an AjaxResult object
        /// {
        ///     ...
        ///     Data: {
        ///         reports: [ 
        ///             { ReportID: 0, 
        ///               ReportName: "", 
        ///               Description: "", 
        ///               DatabaseQueryID: 0,
        ///               Roles: "admin,manage",
        ///               Widgets: "location, fromdate, todate, rowcount",
        ///               ColumnDefinitions: "JSON data"
        ///             }
        ///         ]
        ///     }
        /// }
        ///</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet("getreports")]
        [Authorize(Roles = "admin,manage,edit,view")]
        public AjaxResult GetReports()
        {
            AjaxResult result = new AjaxResult("ReportController.GetReports");
            try
            {
                // we need the user's roles to determine which reports he can run
                UserRoles user_roles = ControllerHelper.GetUserRoles(User.Identity.Name, m_account_helper);
                using (CMSDB db = new CMSDB())
                {
                    List<ReportDefinition> allreports = db.GetReportDefinitions();
                    // PH: this doesn't work
                    // db.ChangeTracker.LazyLoadingEnabled = false;

                    // fix for EF doing lazy loading when reports are serialized
                    List<ReportData> reports = new List<ReportData>();
                    foreach (var rpt in allreports.Where(x => x.CanRun(user_roles))) {
                        reports.Add(new ReportData(rpt));
                    }
                    if (reports.Count > 0) result.Set("reports", reports).Succeed();
                    else result.Fail("There are no reports to show.  Please check with your system administrator.");
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
        /// Function:       GetQueries
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET: http://.../Report/getreports
        /// </summary>
        ///
        /// <returns>
        /// an AjaxResult object
        /// {
        ///     ...
        ///     Data: {
        ///         queries: [ 
        ///             { DatabaseQueryID: 0, 
        ///               Name: "", 
        ///               QueryText: "" 
        ///             }
        ///         ]
        ///     }
        /// }
        ///</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet("getqueries")]
        [Authorize(Roles = "admin")]
        public AjaxResult GetQueries()
        {
            AjaxResult result = new AjaxResult("ReportController.GetQueries");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    var queries = db.DatabaseQueries.ToArray();
                    result.Set("queries", queries)
                          .Succeed();
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
        /// Function:       RunQuery
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Execute a predefined query
        /// </summary>
        ///
        /// <param name="query_request">a DatabaseQueryRequest object</param>
        /// <returns>AjaxResult</returns>
        /// 
        /// <remarks>
        /// The postdata will look like this (in the JavaScript code):
        /// 
        /// var postdata = {
        ///     "QueryName": "test",
        ///     "Parameters": [ 
        ///         { 
        ///             "DatabaseQueryID": 0, 
        ///             "Parameters": [
        ///                 { DataType: 'string', Name: 'chemical', Value: '%magnesium%' },
        ///                 . . .
        ///             ], 
        ///             "MaxRows": 0 
        ///         }
        ///     ],
        ///     "MaxRows": 10
        /// }
        /// 
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpPost("runquery")]
        public AjaxResult RunQuery([FromBody] DatabaseQueryRequest query_request)
        {
            AjaxResult result = new AjaxResult("APIController.ExecuteQuery");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    DatabaseQuery query = db.DatabaseQueries.FirstOrDefault(x => x.DatabaseQueryID == query_request.DatabaseQueryID);
                    if (query != null)
                    {
                        QueryProcessor qp = new QueryProcessor(query, query_request.MaxRows);
                        var query_result = qp.RunQuery(query_request.Parameters);
                        if (result != null)
                        {
                            result.Set("RowsRead", query_result.RowsRead);
                            result.Set("Rows", query_result.Rows);
                            result.Succeed("SUCCESS");
                        }
                        else result.Fail("Query failed.");
                    }
                    else result.Fail($"Query {query_request.DatabaseQueryID} not found");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpPost("savequery")]
        [Authorize(Roles = "admin")]
        public AjaxResult SaveQuery([FromBody] DatabaseQuery query)
        {
            AjaxResult result = new AjaxResult("ReportController.SaveQuery");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    if (query.DatabaseQueryID == 0)
                    {
                        // new query
                        db.DatabaseQueries.Add(query);
                        db.SaveChanges();
                        result.Set("query", query);
                        result.Succeed();
                    }
                    else
                    {
                        // update to existing query
                        DatabaseQuery existing = db.DatabaseQueries.Find(query.DatabaseQueryID);
                        existing.Name = query.Name;
                        existing.QueryText = query.QueryText;
                        db.SaveChanges();
                        result.Set("query", existing);
                        result.Succeed();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        [HttpPost("savereport")]
        [Authorize(Roles = "admin")]
        public AjaxResult SaveReport([FromBody] ReportDefinition report)
        {
            AjaxResult result = new AjaxResult("ReportController.SaveReport");
            try
            {
                string username = User.Identity.Name;
                using (CMSDB db = new CMSDB())
                {
                    if (report.ReportID == 0)
                    {
                        // new report
                        db.ReportDefinitions.Add(report);
                        db.SaveChanges();
                        db.LogInfo(username, "reports", $"Added ReportDefinition {report.ReportName} ({report.ReportID})", true);  
                        result.Set("report", report);
                        result.Succeed();
                    }
                    else
                    {
                        // updating existing report
                        ReportDefinition existing = db.ReportDefinitions.Find(report.ReportID);
                        existing.ReportName = report.ReportName;
                        existing.Roles = report.Roles;
                        existing.Description = report.Description;
                        existing.DatabaseQueryID = report.DatabaseQueryID;
                        existing.Widgets = report.Widgets;
                        existing.ColumnDefinitions = report.ColumnDefinitions;
                        db.LogInfo(username, "reports", $"Updated ReportDefinition {report.ReportName} ({report.ReportID})", true);  // will SaveChanges
                        result.Set("report", existing);
                        result.Succeed();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }




        [HttpPost("runreport")]
        [Authorize(Roles = "admin,manage,edit,view")]
        public AjaxResult RunReport([FromBody] ReportRequest report_request)
        {
            AjaxResult result = new AjaxResult("ReportController.RunReport");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    ReportDefinition def = db.GetReportDefinitions().FirstOrDefault(x => x.ReportID == report_request.ReportID);
                    if (def == null) result.Fail("Report not found");
                    else
                    {
                        DatabaseQuery query = db.DatabaseQueries.FirstOrDefault(x => x.DatabaseQueryID == def.DatabaseQueryID);
                        if (query != null)
                        {
                            QueryProcessor qp = new QueryProcessor(query, report_request.MaxRows);
                            if (!String.IsNullOrEmpty(def.WhereClause)) qp.WhereClause = def.WhereClause;
                            var query_result = qp.RunQuery(report_request.Parameters);
                            result.Set("Result", query_result.Result);
                            result.Set("RowsRead", query_result.RowsRead);
                            result.Set("Rows", query_result.Rows);
                            result.Succeed("SUCCESS");
                        }
                        else result.Fail($"Query {def.DatabaseQueryID} not found in database: ");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        private QueryResult GetUserData()
        {
            List<UserInfo> users = m_account_helper.GetUsers();
            QueryResult result = new QueryResult();
            result.AddColumn("UserName", EQueryDataType.STRING, 0);
            result.AddColumn("Email", EQueryDataType.STRING, 0);
            result.AddColumn("Roles", EQueryDataType.STRING, 0);

            foreach (UserInfo user in users)
            {
                result.AddRow(new ColumnData(user.UserName),
                              new ColumnData(user.Email),
                              new ColumnData(String.Join(", ", user.Roles))
                );
            }
            return result;
        }

        private QueryResult InventoryReportData(OldReportRequest report_request, CMSDB db)
        {
            bool by_chemical = (report_request.ReportID == "inventory-by-chemical");
            bool by_location = (report_request.ReportID == "inventory-by-location");
            QueryResult result = new QueryResult();
            result.AddColumn("LocationID", EQueryDataType.INT, 0);
            result.AddColumn("Chemical", EQueryDataType.STRING, 0);
            result.AddColumn("Location", EQueryDataType.STRING, 0);
            result.AddColumn("Barcode", EQueryDataType.STRING, 0);
            result.AddColumn("CASNumber", EQueryDataType.STRING, 0);
            result.AddColumn("Owner", EQueryDataType.STRING, 0);
            result.AddColumn("ContainerSize", EQueryDataType.FLOAT, 0);
            result.AddColumn("Remaining", EQueryDataType.FLOAT, 0);

            var query = db.InventoryItems.Include(x => x.Location).Include(x => x.Owner).OrderBy(x => x.ChemicalName);
            var rows = query.ToList();
            foreach (var row in rows)
            {
                if (row.Location != null)
                {
                    db.InitializeLocationNames(row.Location);
                    row.FullLocation = row.Location.FullLocation;
                }
                else row.FullLocation = "";
            }
            if (by_location) rows = rows.OrderBy(x => x.FullLocation).ToList();
            foreach (var item in rows)
            {
                result.AddRow(
                    new ColumnData(item.InventoryID),
                    new ColumnData(item.ChemicalName ?? ""),
                    new ColumnData(item.FullLocation),
                    new ColumnData(item.Barcode),
                    new ColumnData(item.CASNumber),
                    new ColumnData(item.Owner?.Name ?? ""),
                    new ColumnData(item.ContainerSize),
                    new ColumnData(item.RemainingQuantity)
                );
            }
            // TODO: add QueryResult.GroupBy(int column_id);
            return result;
        }

        private QueryResult StockCheckReportData(CMSDB db)
        {
            QueryResult result = new QueryResult();
            result.AddColumn("Location", EQueryDataType.STRING, 0);
            result.AddColumn("Barcode", EQueryDataType.STRING, 0);
            result.AddColumn("Status", EQueryDataType.STRING, 0);
            result.AddColumn("StockCheckLocation", EQueryDataType.STRING, 0);
            result.AddColumn("CASNumber", EQueryDataType.STRING, 0);
            result.AddColumn("Chemical", EQueryDataType.STRING, 0);

            db.GetStockCheckData(result);
            return result;
        }


    }


    public class OldReportRequest
    {
        public string ReportID { get; set; }
        public string UserName { get; set; }
        public string Location { get; set; }
        public string Owner { get; set; }
    }

    public class ReportResponsedData
    {
        public string ReportID { get; set; }
        public string Title { get; set; }
    }

    //#########################################################################
    //
    // ReportData - ReportDefinition without the query
    //
    // This is only needed because EF does lazy loading when a ReportDefinition
    // is serialized.
    //
    //#########################################################################
    public class ReportData
    {
        public int ReportID { get; set; }
        public string ReportName { get; set; }
        public string Description { get; set; }
        public int DatabaseQueryID { get; set; }
        public string Roles { get; set; }
        public string Widgets { get; set; }
        public string ColumnDefinitions { get; set; }     // JSON to configure Vue Grid

        public ReportData(ReportDefinition definition)
        {
            ReportID = definition.ReportID;
            ReportName = definition.ReportName;
            Description = definition.Description;
            DatabaseQueryID = definition.DatabaseQueryID;
            Roles = definition.Roles;
            Widgets = definition.Widgets;
            ColumnDefinitions = definition.ColumnDefinitions;
        }
    }

    //#########################################################################
    //
    // ReportRequest - request data that is passed to GetReports
    //
    //#########################################################################
    public class ReportRequest
    {
        public int ReportID { get; set; }
        public List<TypedQueryParameter> Parameters { get; set; }
        public int MaxRows { get; set; } = 100;
        public int GridHeight { get; set; } = 1000;
    }

}