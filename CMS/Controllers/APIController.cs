using Common;
using DataModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using CMS.Services;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace CMS.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    [Authorize]
    public class APIController : Controller
    {
        private IAccountHelper m_account_helper;
        private IHostingEnvironment m_hosting_environment;

        public APIController(IAccountHelper account_helper, IHostingEnvironment env)
        {
            m_account_helper = account_helper;
            m_hosting_environment = env;
        }

        //#############################################################
        //
        // Inventory
        //
        //#############################################################


        ///----------------------------------------------------------------
        ///
        /// Function:       Inventory
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET: api/inventory
        /// Fetch all inventory items at the given location or below.
        /// User visibility constraints are enforced
        /// </summary>
        ///
        /// <param name="root_id">0 or the LocationID of the location of interest</param>
        /// <returns>an AjaxResult object</returns>
        /// 
        /// <remarks>
        /// If root_id is 0, the user's home directory is used
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpGet("inventory/{root_id:int}")]
        public AjaxResult Inventory(int root_id)
        {
            AjaxResult result = new AjaxResult("APIController.Inventory");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    (UserInfo user_info, StorageLocation home_location) = GetVisibleLocation(root_id, db);
                    // read inventory, up to MaxInventoryRows
                    if (home_location != null)
                    {
                        List<InventoryItem> inventory = db.GetInventory(home_location.LocationID);
                        string message = "Success";
                        if (inventory.Count == CMSDB.MaxInventoryRows) message = $"The total number of inventory items returned was limited to {CMSDB.MaxInventoryRows}";
                        result["Inventory"] = inventory;
                        db.InitializeLocationNames(home_location);
                        result.Set("HomeLocation", home_location)
                              .Set("ChildLocations", db.Locations.Where(x => x.ParentID == home_location.LocationID).ToArray())
                              .Set("Inventory", inventory)
                              .Succeed(message);
                    }
                    else
                    {
                        result.Fail("Unable to find requested storage location");
                    }

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
        /// Function:       LocalInventory
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET: api/localinventory
        /// Fetch all inventory items at the given location.
        /// User visibility constraints are enforced
        /// </summary>
        ///
        /// <param name="root_id">0 or the LocationID of the location of interest</param>
        /// <returns>an AjaxResult object</returns>
        /// 
        /// <remarks>
        /// If root_id is 0, the user's home directory is used
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpGet("localinventory/{root_id:int}")]
        public AjaxResult LocalInventory(int root_id)
        {
            AjaxResult result = new AjaxResult("APIController.Inventory");
            try
            {
                using (CMSDB db = new CMSDB())
                {

                    // get all the locations that a user can see
                    List<InventoryItem> inventory = new List<InventoryItem>();
                    (UserInfo user_info, StorageLocation home_location) = GetVisibleLocation(root_id, db);
                    if (home_location != null)
                    {
                        inventory = db.GetLocalInventory(home_location.LocationID);
                        result["Inventory"] = inventory;
                        result.Set("HomeLocation", home_location)
                              .Set("Inventory", inventory)
                              .Succeed();
                    }
                    else
                    {
                        result.Fail("Unable to find requested storage location");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        [HttpPost("inventorysearch")]
        public AjaxResult InventorySearch([FromBody] InventorySearchSettings settings)
        {
            AjaxResult result = new AjaxResult("APIController.InventorySearch");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string username = User.Identity.Name;
                    UserInfo user_info = m_account_helper.GetUser(username);
                    if (!db.Locations.Subsumes(user_info.HomeLocationID, settings.RootID))
                    {
                        throw new Exception($"Location access denied for location #{settings.RootID}");
                    }
                    List<InventoryItem> inventory;
                    inventory = db.SearchInventory(User.Identity.Name, settings.RootID, settings);
                    // the call to SearchINventory will have created a cached subtree
                    LocationSubtree subtree = db.GetCachedLocations(username, settings.RootID);
                    string message = "Success";
                    if (inventory.Count == CMSDB.MaxInventoryRows) message = $"The total number of inventory items returned was limited to {CMSDB.MaxInventoryRows}";
                    settings.IsInitialQuery = false;
                    result.Set("Inventory", inventory)
                          .Set("HomeLocation", subtree.Locations[0])
                          .Set("ChildLocations", subtree.GetChildren(subtree.Root))
                          .Set("SearchSettings", settings)
                          .Succeed(message);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpPost("inventorysearchnoauth")]
        public AjaxResult InventorySearchNoauth([FromBody] InventorySearchSettings settings)
        {
            AjaxResult result = new AjaxResult("APIController.InventorySearchNoauth");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string username = User.Identity.Name;
                    UserInfo user_info = m_account_helper.GetUser(username);
                    List<InventoryItem> inventory;
                    inventory = db.SearchInventoryNoauth(user_info.HomeLocationID, settings);
                    LocationSubtree subtree = db.GetCachedLocations(username, settings.RootID);
                    string message = "Success";
                    if (inventory.Count == CMSDB.MaxInventoryRows) message = $"The total number of inventory items returned was limited to {CMSDB.MaxInventoryRows}";
                    settings.IsInitialQuery = false;
                    result.Set("Inventory", inventory)
                          .Set("HomeLocation", subtree.Locations[0])
                          .Set("ChildLocations", subtree.GetChildren(subtree.Root))
                          .Set("SearchSettings", settings)
                          .Succeed(message);
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
        /// Function:       FetchInventoryItem
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../api/fetchitem/barcode
        /// </summary>
        ///
        /// <param name="barcode">request argument</param>
        /// <returns>an AjaxResult object</returns>
        ///
        /// <remark>
        /// The returned AjaxResult will contain the following fields
        /// in its Data property.
        ///     Item: the Inventory item
        ///     Owners: an array of all the Owners in the database
        ///     Groups: an array of all the group names in the database
        /// </remark>
        ///
        ///----------------------------------------------------------------
        [HttpGet("fetchitem/{barcode}")]
        public AjaxResult FetchInventoryItem(string barcode)
        {
            AjaxResult result = new AjaxResult("APIController.FetchInventoryItem");
            try
            {
                if (barcode == null) throw new Exception("No barcode specified");
                using (CMSDB db = new CMSDB())
                {
                    // semi-kludge: if the barcode includes '#', it has been replaced with '_HASH_'
                    string decoded_barcode = barcode.Replace("_HASH_", "#");
                    InventoryItem item = db.GetItemByBarcode(decoded_barcode);
                    if (item == null) result.Fail($"No inventory item with barcode \"{barcode}\" found in database.");
                    else
                    {
                        item.InitializeItemFlags(db);
                        item.ExpandLocation(db, false);
                        result.Succeed($"Found inventory item {barcode}", "Item", item);
                        result["Owners"] = db.Owners.OrderBy(x => x.Name).ToList();
                        //result["Locations"] = db.GetStorageLocationData();
                        result["Groups"] = db.StorageGroups.OrderBy(x => x.Name).ToList();
                    }
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
        /// Function:       FindBarcode
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../api/findbarcode/barcode
        /// </summary>
        ///
        /// <param name="barcode">request argument</param>
        /// <returns>an AjaxResult object</returns>
        ///
        /// <remark>
        /// This API is almost identical to FetchInventoryItem except it
        /// does not return Owners or Groups or initialize FullLocation
        /// or ShortLocation, since InventoryItem.Path is now availalble.
        /// It also returns an array of SDS files that match the item's
        /// CAS #.
        ///
        /// The returned AjaxResult will contain the following fields
        /// in its Data property.
        ///     Item: the Inventory item
        ///     SDSFiles: a list of SDS files for this CAS number
        /// </remark>
        ///
        ///----------------------------------------------------------------
        [HttpGet("findbarcode/{barcode}")]
        public AjaxResult FindBarcode(string barcode)
        {
            AjaxResult result = new AjaxResult("APIController.FindBarcode");
            try
            {
                if (barcode == null) throw new Exception("No barcode specified");
                using (CMSDB db = new CMSDB())
                {
                    // semi-kludge: if the barcode includes '#', it has been replaced with '_HASH_'
                    string decoded_barcode = barcode.Replace("_HASH_", "#");
                    InventoryItem item = db.GetItemByBarcode(decoded_barcode);
                    if (item == null) result.Fail($"Barcode \"{barcode}\" was not found or is not accessible.");
                    else
                    {
                        (_, StorageLocation home_location) = GetUserHomeLocation(db);
                        StorageLocation loc = db.Locations.Find(item.LocationID);
                        if (db.Locations.Subsumes(home_location, loc))
                        {
                            item.InitializeItemFlags(db);
                            string[] sds_files = GetSDSFiles(item.CASNumber);
                            result.Set("SDSFiles", sds_files)
                                .Set("Item", item)
                                .Set("Location", loc)
                                .Succeed($"Found inventory item {barcode}");
                        }
                        else result.Fail($"Barcode \"{barcode}\" was not found or is not accessible.");
                    }
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
        /// Function:       UpdateInventoryItem
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Add or update an inventory item in the database
        /// </summary>
        ///
        /// <param name="item">(POST) the item to update</param>
        /// <returns>an AjaxResult</returns>
        /// 
        /// <remarks>
        /// If item.InventoryID == 0, we interpret that as a new item.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpPost("update_item")]
        [Authorize(Roles = "admin,edit,manage")]
        public AjaxResult UpdateInventoryItem([FromBody] InventoryItem item)
        {
            // the updated item will have ItemFlags set correctly and the item's Flags property needs to be set
            AjaxResult result = new AjaxResult("APIController.UpdateInventoryItem");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    // initialize ItemFlags from Flags
                    item.ItemFlags.Reset();
                    item.InitializeItemFlags(item.Flags);
                    StorageLocation loc = db.FindLocation(item.LocationID);
                    if (loc != null)
                    {
                        StorageLocation site = db.GetSite(loc);
                        //item.SiteID = site.LocationID;
                    }
                    if (item.InventoryID == 0)
                    {
                        InventoryItem existing = db.InventoryItems.FirstOrDefault(x => x.Barcode == item.Barcode);
                        if (existing != null)
                        {
                            result.Fail($"An item with barcode \"{item.Barcode}\" already exists.");
                            db.LogError(User.Identity.Name, "inventory update", result.Message);
                        }
                        else
                        {
                            db.InventoryItems.Add(item);
                            item.ExpandLocation(db, true);
                            db.SaveChanges();
                            result.Set("UpdatedItem", item);
                            result.Succeed($"Inventory item #{item.InventoryID} ({item.Barcode}) successfully added.");
                            db.LogInfo(User.Identity.Name, "inventory update", result.Message);
                        }
                    }
                    else
                    {
                        InventoryItem existing = db.InventoryItems.Find(item.InventoryID);
                        if (existing == null) throw (new Exception($"UpdateInventoryItem: ID {item.InventoryID} does not exist"));
                        item.ExpandLocation(db, true);
                        db.Entry(existing).CurrentValues.SetValues(item);
                        db.SaveChanges();
                        item = ReadInventoryItem(item.InventoryID, db);
                        result.Set("UpdatedItem", item);
                        result.Succeed($"Inventory item #{item.InventoryID} ({item.Barcode}) successfully updated.");
                        db.LogInfo(User.Identity.Name, "inventory update", result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
                ControllerHelper.LogError(User.Identity.Name, "inventory update", $"Exception in UpdateInventoryItem: {result.Message}");
            }
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       DeleteItem
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP DELETE API: http://.../api/delete_item/id
        /// </summary>
        ///
        /// <param name="id">request argument</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpDelete]
        [Route("delete_item")]
        [Authorize(Roles = "admin,edit,manage")]
        public AjaxResult DeleteItem([FromQuery(Name = "id")] int id)
        {
            AjaxResult result = new AjaxResult("DatabaseController.DeleteItem");
            try
            {
                UserInfo user = m_account_helper.GetUser(User.Identity.Name);
                using (CMSDB db = new CMSDB())
                {
                    InventoryItem doomed = db.InventoryItems.Find(id);
                    if (doomed != null)
                    {
                        db.DeleteItem(doomed, RemovedItem.ERemovalReason.DELETED, User.Identity.Name, true);
                        result.Succeed($"Successfully deleted item {id}");
                    }
                    else
                    {
                        result.Fail($"Item {id} does not exist.");
                        db.LogError(User.Identity.Name, "inventory update", $"Delete operation failed: {result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
                ControllerHelper.LogError(User.Identity.Name, "inventory update", $"Exception in DeleteItem: {result.Message}");
            }
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       MoveItem
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// HTTP GET API: http://.../api/moveitem/itemid/locid
        /// </summary>
        ///
        /// <param name="itemid">InventoryID of the item to move</param>
        /// <param name="locid">LocationID of the location to receive the item</param>
        /// <returns>an AjaxResult object</returns>
        ///
        ///----------------------------------------------------------------
        [HttpGet("moveitem/{itemid}/{locid}")]
        [Authorize(Roles = "admin,edit,manage")]
        public AjaxResult MoveItem(int itemid, int locid)
        {
            AjaxResult result = new AjaxResult("DatabaseController.MoveItem");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    InventoryItem item = db.InventoryItems.Find(itemid);
                    if (item != null)
                    {
                        db.MoveItem(item, User.Identity.Name, locid, true);
                        result.Set("UpdatedItem", db.InventoryItems.Find(itemid));
                        result.Succeed($"Successfully moved {item.Barcode}");
                    }
                    else
                    {
                        result.Fail($"Item {itemid} does not exist.");
                        db.LogError(User.Identity.Name, "item move", $"Move operation failed: {result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
                ControllerHelper.LogError(User.Identity.Name, "item move", $"Exception in MoveItem: {result.Message}");
            }
            return result;
        }


        //#################################################################
        //
        // STOCK CHECK
        //
        // The client will have an instance of StockCheckData that he will
        // pass to each of these functions.  If the function succeeds, the
        // StockCheckData will be updated and returned in the AjaxResult.
        //
        // Fields in StockCheckData:
        //     BarCode - a barcode that the user has entered
        //     SelectedLocationID - the LocationID of the location the user has selected
        //     UnconfirmedInventory - the inventory items at the currently
        //         selected location that has not yet been confirmed
        //     ConfirmedInventory - the inventory items that have been confirmed
        //         at the currently selected location
        //
        //#################################################################

        [HttpPost("stockcheck")]
        public AjaxResult StockCheck([FromBody] StockCheckData stockcheckdata)
        {
            AjaxResult result = new AjaxResult("APIController.StockCheck");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    if (stockcheckdata.OpCode == "new")
                    {
                        db.Database.ExecuteSqlCommand("update InventoryItems set StockCheckPreviousLocation = null, StockCheckTime = null, StockCheckUser = null where StockCheckPreviousLocation is not null");
                        db.Database.ExecuteSqlCommand("delete from Orphans");
                        stockcheckdata.ConfirmedInventory = new List<InventoryItem>();
                        stockcheckdata.UnconfirmedInventory = new List<InventoryItem>();

                        result.Succeed("SUCCESS - stock check initialized", "StockCheckData", stockcheckdata);
                        string stock_check_date = CMSDB.StandardDate();
                        db.StoreSetting(CMSDB.StockCheckDateKey, stock_check_date);
                        result.Set("StockCheckDate", stock_check_date);

                    }
                    if (stockcheckdata.OpCode == "initloc")
                    {
                        GetStockCheckLocation(stockcheckdata, stockcheckdata.SelectedLocationID, db);
                        result.Succeed("SUCCESS - stock check location initialized", "StockCheckData", stockcheckdata);
                    }

                    if (stockcheckdata.OpCode == "find")
                    {
                        string barcode = stockcheckdata.Barcode.ToLower();
                        (bool rc, string errmsg) = CMSDB.IsBarcodeValid(stockcheckdata.Barcode);
                        if (!rc) return result.Fail(errmsg);

                        InventoryItem item = db.InventoryItems.FirstOrDefault(x => x.Barcode == stockcheckdata.Barcode);
                        if (item == null) return result.Fail($"No inventory item with barcode \"{barcode}\" was found");
                        if (item.LocationID == stockcheckdata.SelectedLocationID)
                        {
                            // we found the barcoded inventory item at at the selected location
                            InventoryItem found_item = stockcheckdata.UnconfirmedInventory.FirstOrDefault(x => x.InventoryID == item.InventoryID);
                            if (found_item != null)
                            {
                                // it has not been confirmed yet
                                stockcheckdata.UnconfirmedInventory.Remove(found_item);
                                stockcheckdata.ConfirmedInventory.Add(found_item);
                                item.SetStockCheckInformation(stockcheckdata.SelectedLocationID, User.Identity.Name, true);
                                db.SaveChanges();
                                found_item.StockCheckPreviousLocation = item.StockCheckPreviousLocation;
                                result.Succeed("SUCCESS - recorded in current location", "StockCheckData", stockcheckdata);
                            }
                            else
                            {
                                // this item is not in the unconfirmed list for the current location
                                // has it already been confirmed?
                                found_item = stockcheckdata.ConfirmedInventory.FirstOrDefault(x => x.InventoryID == item.InventoryID);
                                if (found_item != null)
                                {
                                    // yes - it has already been scanned in the current location
                                    item.SetStockCheckInformation(stockcheckdata.SelectedLocationID, User.Identity.Name, true);
                                    db.SaveChanges();
                                    result.Succeed("SUCCESS - previously recorded in current location", "StockCheckData", stockcheckdata);
                                }
                                else
                                {
                                    // this item has the right LocationID but is not in the confirmed or unconfirmed list
                                    throw new Exception($"Item \"{barcode}\" was not in either the confirmed or unconfirmed list");
                                }
                            }
                        }
                        else
                        {
                            // this item is not where it ought to be
                            InventoryItem found_item = stockcheckdata.ConfirmedInventory.FirstOrDefault(x => x.InventoryID == item.InventoryID);
                            if (found_item != null)
                            {
                                // this item has already been confirmed at the currently selected location
                                result.Succeed("SUCCESS - previously recorded in current location", "StockCheckData", stockcheckdata);
                            }
                            else
                            {
                                // record the items new location
                                int previous_location = item.LocationID;
                                stockcheckdata.UnconfirmedInventory.Remove(item);
                                stockcheckdata.ConfirmedInventory.Add(item);
                                item.SetStockCheckInformation(stockcheckdata.SelectedLocationID, User.Identity.Name, false);
                                //item.StockCheckPreviousLocation = previous_location;
                                db.SaveChanges();
                                result.Succeed("SUCCESS - recorded in new location", "StockCheckData", stockcheckdata);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        //#################################################################
        //
        // SETTINGS
        //
        //#################################################################

        [HttpGet("usersettings")]
        public AjaxResult UserSettings()
        {
            AjaxResult result = new AjaxResult("APIController.UserSettings");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    (UserInfo user_info, StorageLocation home_location) = GetUserHomeLocation(db);
                    Setting bookmark = db.GetUserSetting(User.Identity.Name, "bookmark");
                    if (bookmark != null)
                    {
                        InventorySearchSettings search_settings = JsonConvert.DeserializeObject<InventorySearchSettings>(bookmark.SettingValue);
                        result.Set("Bookmark", search_settings);
                        result.Set("BookmarkLocation", db.FindLocation(search_settings.RootID));
                    }
                    result.Set("HomeLocation", home_location).Set("RootLocation", home_location);
                    result.Succeed("Success");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("ownersandgroups")]
        public AjaxResult OwnersAndGroups()
        {
            AjaxResult result = new AjaxResult("APIController.OwnersAndGroups");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    var owners = db.Owners.OrderBy(x => x.Name).ToArray();
                    var groups = db.StorageGroups.OrderBy(x => x.Name).ToArray();
                    result.Set("owners", owners).Set("groups", groups).Succeed();
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("settings")]
        public AjaxResult Settings()
        {
            AjaxResult result = new AjaxResult("APIController.Settings");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    GetLocationsAndSites(result, db);
                    (UserInfo user_info, StorageLocation home_location) = GetUserHomeLocation(db);
                    //db.InitializeLocationNames(home_location);
                    result.Set("HomeLocation", home_location).Set("RootLocation", db.GetRootLocation());
                    result.Set("GlobalSettings", db.SystemSettings());
                    result.Succeed("Success");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("announcement")]
        public AjaxResult Announcement()
        {
            AjaxResult result = new AjaxResult("APIController.Announcement");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    result.Set("Announcement", db.GetStringSetting(CMSDB.AnnouncementKey, ""));
                    result.Succeed("Success");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        private string ReadBody()
        {
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();

                return body;
            }
        }


        [HttpPost("update_settings")]
        public AjaxResult UpdateSettings()
        {
            AjaxResult result = new AjaxResult("APIController.UpdateSettings");
            try
            {
                SettingsUpdate settings;
                string body_content = ReadBody();
                settings = Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsUpdate>(body_content);
                using (CMSDB db = new CMSDB())
                {
                    settings.SaveChanges(db);
                    // send back the updated list because client will need IDs of new items
                    GetLocationsAndSites(result, db);
                    result.Set("GlobalSettings", db.SystemSettings());
                    result.Succeed("Database successfully updated");
                    db.LogInfo(User.Identity.Name, "update", "Settings updated.");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpPost("setbookmark")]
        public AjaxResult SetBookmark([FromBody] InventorySearchSettings settings)
        {
            AjaxResult result = new AjaxResult("APIController.SetBookmark");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string value = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    db.StoreUserSetting(User.Identity.Name, "bookmark", value);
                    result.Succeed("Bookmark saved");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        //#################################################################
        //
        // LOCATIONS / SITES
        //
        //#################################################################

        [HttpPost("initializelocationtypes")]
        public AjaxResult InitializeLocationTypes([FromBody] Dictionary<string, LocationTypeInitializer> type_definitions)
        {
            AjaxResult result = new AjaxResult("APIController.InitializeLocationTypes");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    int added_count = 0;
                    int updated_count = 0;
                    int deleted_count = 0;
                    int existing_count = 0;

                    // find all the existing types
                    Dictionary<int, LocationType> existing = new Dictionary<int, LocationType>();
                    foreach (LocationType lt in db.LocationTypes)
                    {
                        existing.Add(lt.LocationTypeID, lt);
                        lt.IsDeleted = true;
                    }
                    // add/update submitted types
                    foreach ((string name, LocationTypeInitializer def) in type_definitions)
                    {
                        if (def.id > 0)
                        {
                            // this is an existing type
                            if (existing.ContainsKey(def.id))
                            {
                                var existing_type = existing[def.id];
                                existing_type.Name = name;
                                existing_type.ValidChildren = String.Join(",", def.children);
                                existing_type.IsDeleted = false;
                                updated_count += 1;
                            }
                            else throw new Exception($"InitializeLocationTypes - LocationTypeID {def.id} not found");
                        }
                        else
                        {
                            // add new location type
                            LocationType newtype = new LocationType()
                            {
                                LocationTypeID = 0,
                                Name = name,
                                ValidChildren = String.Join(",", def.children)
                            };
                            db.LocationTypes.Add(newtype);
                            added_count += 1;
                        }
                    }
                    // delete any remaining existing types that are not in type_definitions
                    foreach ((int id, LocationType lt) in existing)
                    {
                        if (lt.IsDeleted)
                        {
                            bool in_use = (db.GetLocationCount(id) > 0);
                            if (!in_use)
                            {
                                db.LocationTypes.Remove(lt);
                                deleted_count += 1;
                            }
                            else existing_count += 1;  // there are some locations that still use this type
                        }
                    }
                    db.SaveChanges();
                    List<LocationType> updated_locations = db.LocationTypes.ToList();
                    result.Set("locationtypes", db.LocationTypes.ToList())
                          .Set("added", added_count)
                          .Set("deleted", deleted_count)
                          .Set("updated", updated_count)
                          .Set("existing", existing_count);
                    result.Succeed();
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("refreshlocations")]
        public AjaxResult RefreshLocations()
        {
            AjaxResult result = new AjaxResult("APIController.RefreshLocations");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    db.RefreshLocations();
                    result.Succeed();
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        [HttpGet("getlocations")]
        public AjaxResult GetLocations()
        {
            AjaxResult result = new AjaxResult("APIController.GetLocations");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    result.Succeed("SUCCESS", "locations", db.Locations.AllLocations);
                    result.Set("levels", db.LocationLevelNames.OrderBy(x => x.LocationLevel).ToArray());
                    result.Set("locationtypes", db.LocationTypes.ToArray());
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("getlocationtypes")]
        public AjaxResult GetLocationTypes()
        {
            AjaxResult result = new AjaxResult("APIController.GetLocationTypes");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    //result.Set("levels", db.LocationLevelNames.OrderBy(x => x.LocationLevel).ToArray());
                    LocationType[] ltypes = db.LocationTypes.ToArray();
                    result.Set("locationtypes", ltypes);
                    result.Succeed("SUCCESS");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        [HttpGet("getuserlocations/{login}")]
        public AjaxResult GetUserLocations(string login)
        {
            AjaxResult result = new AjaxResult("APIController.GetUserLocations");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    StorageLocation root = db.GetRootLocation();
                    (UserInfo user_info, LocationSubtree subtree) = GetUserSubtree(0, db);
                    List<StorageLocation> locations = subtree.Locations;
                    result.Set("root_location", root);
                    result.Set("locations", locations);
                    result.Set("login", login);
                    result.Succeed("SUCCESS");
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
        /// Function:       GetUserLocationSubtree
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the location subtree that a given user can access
        /// </summary>
        ///
        /// <param name="login">user login name</param>
        /// <param name="rootid">user's home location id</param>
        /// <param name="depth">how many levels to return</param>
        /// <returns>an AjaxResult with user subtree and location level names</returns>
        /// 
        /// <remarks>
        /// If login == "*", we will use the logged in user
        /// If rootid < 1, we will look up the user's home location id.
        /// If depth == 0, we will use 4 - the user's home location level,
        ///                which will go down to the room level
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        [HttpGet("getuserlocationsubtree/{login}/{rootid}/{depth}")]
        [Authorize]
        public AjaxResult GetUserLocationSubtree(string login, int rootid, int depth)
        {
            AjaxResult result = new AjaxResult("APIController.GetUserLocationSubtree");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    if (login == "*")
                    {
                        login = User.Identity.Name;
                    }
                    UserInfo user_info = m_account_helper.GetUser(login);
                    if (rootid < 1)
                    {
                        rootid = user_info.HomeLocationID;
                    }
                    if (db.Locations.Subsumes(user_info.HomeLocationID, rootid))
                    {
                        StorageLocation loc = db.FindLocation(rootid);
                        if (depth == 0)
                        {
                            depth = 4 - loc.LocationLevel;
                        }
                        LocationSubtree subtree = db.Locations.GetSubtree(loc, depth);
                        var locations = subtree.Locations;
                        result.Succeed("Success", "subtree", locations);
                        result.Set("locationtypes", db.LocationTypes.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        [HttpPost("addlocations")]
        public AjaxResult AddLocations([FromBody] AddLocationRequest request)
        {
            AjaxResult result = new AjaxResult("APIController.AddLocations");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    string[] child_names = request.ChildLocationNames.Split(';');
                    int add_count = 0;
                    foreach (string name in child_names)
                    {
                        string trimmed = name.Trim();
                        if (trimmed.Length > 0)
                        {
                            db.AddLocation(trimmed, request.ParentID, true);
                            add_count += 1;
                        }
                    }
                    db.SaveChanges();
                    db.RefreshLocations();
                    List<StorageLocation> locations = db.GetStorageLocations(false);
                    result.Set("locations", locations);
                    string msg = (add_count == 1 ? "One new location successfully added to the database." : $"{add_count} new locations successfully added to the database");
                    result.Succeed(msg);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpPost("updatelocation")]
        public AjaxResult UpdateLocation([FromBody] StorageLocation location)
        {
            AjaxResult result = new AjaxResult("APIController.UpdateLocation");
            try
            {
                string msg = "SUCCESS";
                using (CMSDB db = new CMSDB())
                {
                    if (location.LocationID == 0)
                    {
                        if (location.ParentID < 1) throw new Exception("UpdateLocation - invalid  ParentID");
                        db.AddLocation(location, true);
                        msg = $"Location successfully added to the database.";
                        db.LogInfo(User.Identity.Name, "update", $"Location \"{db.GetLocationName(location.LocationID)}\" ({location.LocationID}) added", false);
                    }
                    else
                    {
                        db.UpdateLocationName(location.LocationID, location.Name);
                        msg = "Location name successfully changed";
                        db.LogInfo(User.Identity.Name, "update", $"Location #{location.LocationID} name changed to \"{location.Name}\"", false);
                    }
                    db.SaveChanges();
                    List<StorageLocation> locations = db.GetStorageLocations(false);
                    result.Set("locations", locations);
                    result.Succeed(msg);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpDelete("deletelocation/{location_id:int}")]
        public AjaxResult DeleteLocation(int location_id)
        {
            AjaxResult result = new AjaxResult("APIController.DeleteLocation");
            try
            {
                //string msg = "SUCCESS";
                using (CMSDB db = new CMSDB())
                {
                    string full_location = db.Locations[location_id].FullLocation;
                    db.DeleteLocation(location_id);
                    List<StorageLocation> locations = db.GetStorageLocations(false);
                    result.Set("locations", locations);
                    result.Succeed($"Location \"{full_location}\" was successfully removed from the database");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        //#################################################################
        //
        // Reports
        //
        //#################################################################

        [HttpGet("getinventory")]
        public AjaxResult GetInventory()
        {
            AjaxResult result = new AjaxResult("APIController.GetInventory");
            try
            {
                List<InventoryData> inventory = new List<InventoryData>();
                using (CMSDB db = new CMSDB())
                {
                    foreach (var item in db.InventoryItems.Include(x => x.Location).Include(x => x.Group).Include(x => x.Owner))
                    {
                        db.InitializeLocationNames(item.Location);
                        InventoryData data = new InventoryData(item);
                        inventory.Add(data);
                    }
                }
                result.Succeed("SUCCESS", "Inventory", inventory);
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        //#################################################################
        //
        // MISC
        //
        //#################################################################



        [HttpGet("gethazardtables")]
        public AjaxResult GetHazardTable()
        {
            AjaxResult result = new AjaxResult("APIController.GetHazardTable");
            try
            {
                Dictionary<string, HazardTable> hazards = new Dictionary<string, HazardTable>();  // CASNumber -> table
                using (CMSDB db = new CMSDB())
                {
                    foreach (var coc in db.ChemicalsOfConcern)
                    {
                        HazardTable hazard = null;
                        if (hazards.ContainsKey(coc.CASNumber)) hazard = hazards[coc.CASNumber];
                        else
                        {
                            hazard = new HazardTable(coc.CASNumber);
                            hazards[coc.CASNumber] = hazard;
                        }
                        hazard.SetValues(coc);
                    }
                    
                    foreach (CASData casdata in db.CASDataItems)
                    {
                        HazardTable hazard = null;
                        if (hazards.ContainsKey(casdata.CASNumber)) hazard = hazards[casdata.CASNumber];
                        else
                        {
                            hazard = new HazardTable(casdata.CASNumber);
                            hazards[casdata.CASNumber] = hazard;
                        }
                        hazard.SetValues(casdata);
                    }

                    result.Set("Hazards", hazards);
                    result.Succeed("SUCCESS");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("gethazardinfo/{casnum}")]
        public AjaxResult GetHazardInfo(string casnum)
        {
            AjaxResult result = new AjaxResult("APIController.GetHazardInfo");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    if (casnum == "all") result.Set("hazardcodes", db.HazardCodes.ToArray());
                    else result.Set("hazardcodes", db.HazardCodes.Where(x => x.CASNumber == casnum).ToArray());
                    List<int> disposal_ids = db.CASDisposalProcedures.Where(x => x.CASNumber == casnum).Select(x => x.DisposalProcedureID).ToList();
                    result.Set("disposal", db.DisposalProcedures.Where(x => disposal_ids.Contains(x.DisposalProcedureID)).ToArray());
                    result.Succeed();
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("getcoc")]
        public AjaxResult GetCOC()
        {
            AjaxResult result = new AjaxResult("APIController.GetCOC");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    result.Set("ChemicalsOfConcern", db.COC);
                    result.Succeed("SUCCESS");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpGet("sdsfiles/{casnumber}")]
        public AjaxResult SDSFiles(string casnumber)
        {
            AjaxResult result = new AjaxResult("APIController.GetCOC");
            try
            {
                string[] sdsfiles = GetSDSFiles(casnumber);
                result.Set("SDSFiles", sdsfiles).Succeed();
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        [HttpGet("seed")]
        [AllowAnonymous]
        public AjaxResult Seed()
        {
            AjaxResult result = new AjaxResult("APIController.Seed");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    bool rc = db.EnsureSeeded();
                    if (rc) result.Succeed("The database was seeded successfully");
                    else result.Succeed("The database did not need to be seeded");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [AllowAnonymous]
        [HttpGet("seedusers/{rootpswd}")]
        public AjaxResult SeedUsers(string rootpswd)
        {
            AjaxResult result = new AjaxResult("APIController.Seed");
            var userinfo = m_account_helper.GetUser("root");
            if (userinfo != null)
            {
                result.Succeed("Default users and roles have already been created.");
                return result;
            }
            try
            {
                int home_location_id = 0;
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
                m_account_helper.EnsureUserExists("root", rootpswd, "root@yoursite.gov", home_location_id, "root", "admin");
                result.Succeed("Default users and roles have been created.");
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }



        [HttpGet("logentries")]
        [Authorize(Roles = "admin,manage")]
        public AjaxResult LogEntries()
        {
            AjaxResult result = new AjaxResult("APIController.LogEntries");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    var entries = db.LogEntries.ToList();
                    result.Succeed("SUCCESS", "entries", entries);
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }


        [HttpGet("attachment/{name}/{username}")]
        public AjaxResult GetAttachment(string name, string username)
        {
            AjaxResult result = new AjaxResult("APIController.GetAttachment");
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    Attachment[] attachments = db.Attachments.Where(x => x.Login == username && x.Name == name).ToArray();
                    if (attachments.Length > 0)
                    {
                        string attachment_dir = System.IO.Path.Combine(m_hosting_environment.WebRootPath, "attachments");
                        System.IO.Directory.CreateDirectory(attachment_dir);
                        string filename = $"{username}_{name}.jpg";
                        string filepath = System.IO.Path.Combine(attachment_dir, filename);
                        System.IO.File.WriteAllBytes(filepath, attachments[0].Data);
                        result.Set("filename", filename);
                        result.Succeed();
                    }
                    else result.Fail($"No attachment exists for user {username} with name {name}");
                }
            }
            catch (Exception ex)
            {
                result.Fail(ex);
            }
            return result;
        }

        [HttpPut("upload")]
        [AllowAnonymous]
        public AjaxResult Upload(List<IFormFile> files, string owner, string tag, string description)
        {
            AjaxResult result = new AjaxResult("APIController.Upload");
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
                result.Fail(ex);
            }
            return result;
        }




        //#################################################################
        //
        // PRIVATE UTILITY FUNCTIONS
        //
        //#################################################################

        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateUserLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Make sure a location id is valid for the current user
        /// </summary>
        ///
        /// <param name="root_id">requested root location id - 0 for home location</param>
        /// <param name="user_info">the UserInfo data for the current user</param>
        /// <param name="db">an open database connection</param>
        /// <returns>the requested location id or the user home location id</returns>
        ///
        ///----------------------------------------------------------------

        private int ValidateUserLocation(int root_id, UserInfo user_info, CMSDB db)
        {
            int home_location_id = user_info.HomeLocationID;
            if (root_id > 0 && root_id != home_location_id)
            {
                if (!db.Locations.Subsumes(home_location_id, root_id))
                {
                    throw new Exception("Location access not permitted");
                }
            }
            else root_id = home_location_id;
            return root_id;
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateUserLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Make sure a location id is valid for the current user
        /// </summary>
        ///
        /// <param name="root_id">requested root location id - 0 for home location</param>
        /// <param name="db">an open database connection</param>
        /// <returns>the requested location id or the user home location id</returns>
        ///
        ///----------------------------------------------------------------
        private int ValidateUserLocation(int root_id, CMSDB db)
        {
            UserInfo user_info = m_account_helper.GetUser(User.Identity.Name);
            return ValidateUserLocation(root_id, user_info, db);
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetUserSubtree
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a LocationSubtree, ensuring that it is viewable by the current user
        /// </summary>
        ///
        /// <param name="root_id">if > 0, the subtree rooted at this location is returened</param>
        /// <param name="db">open database connection</param>
        /// <param name="depth">maximum depth of subtree</param>
        /// <returns>a tuble with UserInfo and a LocationSubtree</returns>
        /// 
        /// <remarks>
        /// If root_id < 1 the subtree rooted at the user's home 
        /// location is returned.
        /// If the user does not have access to the given location, an
        /// exception is thrown.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        private (UserInfo, LocationSubtree) GetUserSubtree(int root_id, CMSDB db, int depth = 99)
        {
            UserInfo user_info = m_account_helper.GetUser(User.Identity.Name);
            root_id = ValidateUserLocation(root_id, user_info, db);
            return (user_info, db.GetSubtree(root_id, depth));
        }

        private (UserInfo, StorageLocation) GetVisibleLocation(int root_id, CMSDB db)
        {
            UserInfo user_info = m_account_helper.GetUser(User.Identity.Name);
            root_id = ValidateUserLocation(root_id, user_info, db);
            StorageLocation loc = db.FindLocation(root_id);
            if (loc == null) throw new Exception($"Location #{root_id} does not exist");
            return (user_info, loc);
        }

        private (UserInfo, StorageLocation) GetUserHomeLocation(CMSDB db)
        {
            return GetVisibleLocation(0, db);
        }

        private InventoryItem ReadInventoryItem(string barcode, CMSDB db)
        {
            string decoded_barcode = barcode.Replace("_HASH_", "#");
            InventoryItem item = db.GetItemByBarcode(decoded_barcode);
            if (item != null)
            {
                item.InitializeItemFlags(db);
                item.ExpandLocation(db, false);
            }
            return item;
        }

        private InventoryItem ReadInventoryItem(int item_id, CMSDB db)
        {
            InventoryItem item = db.GetItem(item_id);
            if (item != null)
            {
                item.InitializeItemFlags(db);
                item.ExpandLocation(db, false);
            }
            return item;
        }




        private string[] GetSDSFiles(string casnumber)
        {
            string sds_path = System.IO.Path.Combine(m_hosting_environment.WebRootPath, "SDS");
            string filter = (casnumber == "*" ? "*.pdf" : $"{casnumber}*.pdf");
            string[] sds_paths = System.IO.Directory.GetFiles(sds_path, filter);
            string[] sds_files = sds_paths.Select(x => System.IO.Path.GetFileName(x)).ToArray();
            return sds_files;
        }




        private void GetLocationsAndSites(AjaxResult result, CMSDB db)
        {
            UserInfo userinfo = m_account_helper.GetUser(User.Identity.Name);

            // get all the storage locations that user can view
            List<StorageLocation> locations = db.Locations.GetStorageLocations();

            SettingsUpdate settings = new SettingsUpdate();
            settings.Owners = db.Owners.OrderBy(x => x.Name).ToList();
            settings.Locations = locations;
            settings.Groups = db.StorageGroups.OrderBy(x => x.Name).ToList();
            result["Settings"] = settings;
            result["StockCheckDate"] = db.GetStringSetting(CMSDB.StockCheckDateKey, "");
        }

        private void GetStockCheckLocation(StockCheckData data, int selected_location_id, CMSDB db)
        {
            List<InventoryItem> location_items = db.InventoryItems.Where(x => x.LocationID == selected_location_id).ToList();
            data.UnconfirmedInventory = new List<InventoryItem>();
            data.ConfirmedInventory = new List<InventoryItem>();
            foreach (InventoryItem item in location_items)
            {
                // if an item has a value for StockCheckLocationID, it has been confirmed
                if (item.StockCheckPreviousLocation.HasValue) data.ConfirmedInventory.Add(item);
                else data.UnconfirmedInventory.Add(item);
            }
        }

    }

    public class SettingsUpdate
    {
        public List<Owner> Owners { get; set; }
        public List<StorageLocation> Locations { get; set; }
        public List<StorageLocation> Sites { get; set; }
        public List<StorageGroup> Groups { get; set; }
        public List<Setting> GlobalSettings { get; set; }
        public StorageLocation CurrentSite { get; set; }
        public string Country { get; set; }
        public bool ChangeCurrentSite { get; set; }

        public void SaveChanges(CMSDB db)
        {
            if (Owners != null) UpdateOwners(db);
            //UpdateLocations(db);  // this is not handled by the location manager page
            if (Groups != null) UpdateGroups(db);
            if (GlobalSettings != null) UpdateGlobalSettings(db);
            //db.StoreSetting(CMSDB.InstitutionKey, Country);
            // db.StoreSetting(CMSDB.CurrentSiteKey, CurrentSite);  // don't change for everybody
            db.SaveChanges();
        }

        private void UpdateOwners(CMSDB db)
        {
            foreach (var owner in Owners.Where(x => x.IsChanged || x.IsDeleted))
            {
                var existing = db.Owners.Find(owner.OwnerID);
                if (existing != null)
                {
                    if (owner.IsDeleted) db.Owners.Remove(existing);
                    else existing.Name = owner.Name;
                }
                else if (!owner.IsDeleted)     // make sure this wasn't added then deleted
                {
                    db.Owners.Add(owner);
                }
            }
        }

        private void UpdateGroups(CMSDB db)
        {
            foreach (StorageGroup group in Groups.Where(x => x.IsChanged || x.IsDeleted))
            {
                StorageGroup existing = db.StorageGroups.Find(group.GroupID);
                if (existing != null)
                {
                    if (group.IsDeleted) db.StorageGroups.Remove(existing);
                    else existing.Name = group.Name;
                }
                else if (!group.IsDeleted)     // make sure this wasn't added then deleted
                {
                    db.StorageGroups.Add(group);
                }
            }
        }

        private void UpdateGlobalSettings(CMSDB db)
        {
            foreach (var setting in GlobalSettings.Where(x => x.IsChanged))
            {
                var existing = db.Settings.FirstOrDefault(x => x.SettingKey == setting.SettingKey);
                if (existing != null) existing.SettingValue = setting.SettingValue;
            }
        }
    }

    public class AddLocationRequest
    {
        public int ParentID { get; set; }
        public string ChildLocationNames { get; set; }
    }


    public class StockCheckData
    {
        public string OpCode { get; set; }
        public string Barcode { get; set; }
        public int SelectedLocationID { get; set; }
        // these two lists represent the inventory in the currently selected location
        public List<InventoryItem> UnconfirmedInventory { get; set; }
        public List<InventoryItem> ConfirmedInventory { get; set; }
        public List<InventoryItem> MovedInventory { get; set; }

        public StockCheckData()
        {
            UnconfirmedInventory = new List<InventoryItem>();
            ConfirmedInventory = new List<InventoryItem>();
            MovedInventory = new List<InventoryItem>();
        }
    }

    public class InventoryData
    {
        public string Barcode { get; set; }
        public string ChemicalName { get; set; }
        public string CASNumber { get; set; }
        public string ShortLocation { get; set; }
        public string FullLocation { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }

        public InventoryData() { }
        public InventoryData(InventoryItem item)
        {
            Barcode = item.Barcode;
            ChemicalName = item.ChemicalName;
            CASNumber = item.CASNumber;
            ShortLocation = item.Location.ShortLocation;
            FullLocation = item.Location.FullLocation;
            Owner = item.Owner == null ? "" : item.Owner.Name;
            Group = item.Group == null ? "" : item.Group.Name;
        }

    }



    public class DatabaseQueryRequest
    {
        public int DatabaseQueryID { get; set; }
        public List<TypedQueryParameter> Parameters { get; set; }
        public int MaxRows { get; set; } = 100;
    }

    public class ReportColumn
    {
        public string field { get; set; }       // column name
        public string header { get; set; }
        public string align { get; set; }
        public int width { get; set; }          // pixels
        public bool sortable { get; set; }
        public bool filter { get; set; }
    }

    public class ReportConfiguration
    {
        public string height { get; set; }
        public bool filter { get; set; }
        public List<ReportColumn> columns { get; set; }
    }

    public class UploadMetadata
    {
        public string Source { get; set; }
        public string Username { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
    }

    public class LocationTypeInitializer
    {
        public int id { get; set; }
        public string[] children { get; set; }
    }

}



