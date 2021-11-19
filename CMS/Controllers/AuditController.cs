using System;
using System.Collections.Generic;
using System.Linq;
using CMS.Services;
using Common;
using DataModel;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers
{
    [Produces("application/json")]
    [Route("audit")]
    public class AuditController : Controller
    {

        private IAccountHelper m_account_helper;

        public AuditController(IAccountHelper account_helper)
        {
            m_account_helper = account_helper;
        }

        //#############################################################
        //
        // Templates
        //
        //#############################################################


        ///----------------------------------------------------------------
        ///
        /// Function:       GetRequest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// An example GET request
        /// </summary>
        ///
        /// <param name="audit_id"> an InventoryAuditID</param>
        /// <returns>an InventoryAudit record if available</returns>
        /// 
        /// <remarks>
        /// Example request
        /// URL: https://localhost:44389/audit/10
        /// Response:
        /// {
        ///     "Source": "AuditController.GetRequest",
        ///     "Success": false,
        ///     "Message": "Not Found",
        ///     "TaskTime": "30.19 ms",
        ///     "Data": {}
        ///}
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        // 

        [HttpGet("getrequest/{audit_id:int}")]
        public AjaxResult GetRequest(int audit_id)
        {
            AjaxResult result = new AjaxResult("AuditController.GetRequest");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    InventoryAudit auditrec = db.InventoryAudits.FirstOrDefault(x => x.InventoryAuditID == audit_id);
                    if (auditrec == null) result.Fail("Not Found");
                    else result.Set("AuditRecord", auditrec).Succeed();
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
        /// Function:       PostRequest
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Example of a POST request handler
        /// </summary>
        ///
        /// <param name="auditreq">an AuditRequest object</param>
        /// <returns>an AjaxResult object</returns>
        /// 
        /// <remarks>
        /// Example request
        /// URL: https://localhost:44389/audit/postrequest 
        /// 
        /// postdata:
        /// {
        ///     "FromDate": "2020-02-16 10:00:00",
        ///     "AuditTime": "2020-02-16 11:00:00"
        /// }
        /// 
        /// response:
        /// {
        ///     "Source": "AuditController.PostRequest",
        ///     "Success": true,
        ///     "Message": "SUCCESS",
        ///     "TaskTime": "12608.08 ms",
        ///     "Data": {
        ///          "ReceivedFromDate": "2020-02-16T10:00:00",
        ///          "ReceivedToDate": "0001-01-01T00:00:00"
        ///      }
        /// }
        ///
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpPost("postrequest")]
        public AjaxResult PostRequest([FromBody] AuditRequest auditreq)
        {
            AjaxResult result = new AjaxResult("AuditController.PostRequest");
            try
            {
                result.Set("ReceivedFromDate", auditreq.FromDate)
                    .Set("ReceivedToDate", auditreq.ToDate)
                    .Succeed();
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetRecords
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Fetch InventoryAudit records
        /// </summary>
        ///
        /// <param name="auditreq">search settings</param>
        /// <returns>AjaxResult with results in Data.Rows</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost("getrecords")]
        public AjaxResult GetRecords([FromBody] AuditRequest auditreq)
        {
            AjaxResult result = new AjaxResult("AuditController.GetRecords");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    UserInfo user_info = m_account_helper.GetUser(User.Identity.Name);
                    int[] location_ids = null;
                    if (auditreq.LocationID > 0)
                    {
                        if (db.Locations.Subsumes(user_info.HomeLocationID, auditreq.LocationID))
                        {
                            location_ids = db.GetSubtree(auditreq.LocationID, 9999).Locations.Select(x => x.LocationID).ToArray();
                        }
                        else throw new Exception($"Location access denied for location #{auditreq.LocationID}");
                    }
                    var query = db.InventoryAudits.Where(x => x.InventoryAuditID > 0);

                    if (auditreq.FromDate.HasValue) query = query.Where(x => x.AuditTime >= auditreq.FromDate.Value);
                    if (auditreq.ToDate.HasValue) query = query.Where(x => x.AuditTime < auditreq.ToDate.Value);
                    if (string.IsNullOrEmpty(auditreq.Barcode) == false) query = query.Where(x => x.Item.Barcode == auditreq.Barcode);
                    if (location_ids != null) query = query.Where(x => location_ids.Contains(x.Item.LocationID));

                    List<InventoryAudit> audits = query.ToList();
                    audits.ForEach(a => a.Item = db.InventoryItems.First(i => i.InventoryID == a.InventoryID));
                    result.Set("Rows", audits).Succeed();
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
        /// Function:       RecordLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Record the current location of an InventoryItem
        /// </summary>
        ///
        /// <param name="auditreq"></param>
        /// <returns>AjaxResult</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost("recordlocation")]
        public AjaxResult RecordItemLocation([FromBody] AuditRequest auditreq)
        {
            AjaxResult result = new AjaxResult("AuditController.RecordItemLocation");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    PerformAudit(db, auditreq, result);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        private void PerformAudit(CMSDB db, AuditRequest request, AjaxResult result)
        {
            InventoryItem item = db.InventoryItems.FirstOrDefault(x => x.Barcode == request.Barcode);
            if (item == null) result.Fail($"Inventory item {request.Barcode} does not exist");
            else
            {
                string username = User.Identity.Name;

                if (User.Identity.Name == null) throw new Exception("You have been logged out due to inactivity.");

                InventoryAudit audit = new InventoryAudit
                {
                    AuditTime = DateTime.Now,
                    InventoryID = item.InventoryID,
                    PreviousLocationID = item.LocationID,
                    Barcode = request.Barcode,
                    LocationID = request.LocationID,
                    User = username
                };
                db.InventoryAudits.Add(audit);
                item.LastInventoryDate = DateTime.Now;
                db.LogInfo(username, "audit", $"Audit record added for {request.Barcode}.  Previous location {item.LocationID}.  Current location {request.LocationID}", true);  // will call SaveChanges

                // if auditreq.LocationID == 0, the item is missing 
                if (request.LocationID == 0)
                {
                    db.DeleteItem(item, RemovedItem.ERemovalReason.NOTFOUND, username, true);
                }
                else
                {
                    if (request.LocationID != item.LocationID)
                    {
                        item.InventoryStatusID = (int)EInventoryStatus.CONFIRMED_AT_NEW_LOCATION;
                        db.MoveItem(item, username, request.LocationID, true);
                    }
                }
                result.Succeed();
            }
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       RecordLocations
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Record the current location of a list of InventoryItems
        /// </summary>
        ///
        /// <param name="auditreqs"></param>
        /// <returns>AjaxResult</returns>
        ///
        ///----------------------------------------------------------------
        [HttpPost("recordlocations")]
        public AjaxResult RecordItemLocations([FromBody] List<AuditRequest> auditreqs)
        {
            AjaxResult result = new AjaxResult("AuditController.RecordItemLocations");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    foreach (AuditRequest request in auditreqs)
                    {
                        PerformAudit(db, request, result);
                    }
                    if (result.Success) db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }
    }
    public class AuditRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int LocationID { get; set; }
        public string Barcode { get; set; }
    }

    public class AuditRequests
    {
        public List<AuditRequest> Requests{ get; set; }

        public AuditRequests() => Requests = new List<AuditRequest>();
    }

    public class AuditDataModel
    {
        public DateTime AuditTime{ get; set; }
        public int LocationID{ get; set; }
        public string User{ get; set; }
    }

    public class ItemAndAuditModel
    {
        public InventoryItem Item { get; set; }
        public AuditDataModel Audit { get; set; }
    }
}