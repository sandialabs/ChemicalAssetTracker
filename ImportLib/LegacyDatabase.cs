using System;
using System.Collections.Generic;
using DatabaseClasses;
using DataModel;
using System.Linq;
using System.Data.Common;

namespace ImportLib
{
    public enum EChangeStatus { IS_NEW, IS_CHANGED, IS_UNCHANGED };

    public class LegacyDatabase : SQLiteDatabase
    {
        private Dictionary<int, CASData> m_casid_map = new Dictionary<int, CASData>();
        private Dictionary<string, CASData> m_casnumber_map = new Dictionary<string, CASData>();
        private Dictionary<int, StorageLocation> m_storage_locations = new Dictionary<int, StorageLocation>();
        private Dictionary<int, StorageGroup> m_storage_groups = new Dictionary<int, StorageGroup>();
        private Dictionary<int, Login> m_logins = new Dictionary<int, Login>();
        private Dictionary<int, Owner> m_owners = new Dictionary<int, Owner>();

        // used for activity log
        public string CurrentUser { get; set; }
        public static string CurrentFilename { get; private set; }

        public LegacyDatabase(string filename)
            : base(filename)
        {
            CurrentUser = "n/a";
            CurrentFilename = filename;
        }

        public void Refresh()
        {
            RefreshCASData();
            RefreshStorageLocations();
            RefreshStorageGroups();
            RefreshLogins();
            RefreshOwners();
        }

        public static LegacyDatabase OpenCurrent()
        {
            if (CurrentFilename == null) return null;
            else return new LegacyDatabase(CurrentFilename);
        }



        //#################################################################
        //
        // CAS Data
        //
        //#################################################################
        public void RefreshCASData()
        {
            m_casid_map.Clear();
            m_casnumber_map.Clear();
            Console.WriteLine("Refreshing CASData");
            DatabaseResult rc = ExecuteQuery("SELECT * FROM CASData", (reader) =>
            {
                CASData data = new CASData(reader);
                if (data.CWCFlag != ' ')
                {
                    Console.WriteLine($"{data.CASNumber}.CECFlag = {data.CWCFlag}");
                }
                if (m_casnumber_map.ContainsKey(data.CASNumber))
                {
                    var prev = m_casnumber_map[data.CASNumber];
                    Console.WriteLine($"Duplicate CASNumber: {data.CASNumber}");
                    prev.Show();
                    data.Show();
                    Console.WriteLine(" ");
                }
                else
                {
                    m_casid_map[data.CASID] = data;
                    m_casnumber_map[data.CASNumber] = data;
                }
                return true;
            });
            if (rc.Result) Console.WriteLine($"    Read {m_casnumber_map.Count} CASData records");
            else Console.WriteLine($"    Query failed: {rc.ErrorMessage}");
        }

        public List<CASData> FetchCASData()
        {
            List<CASData> result = m_casnumber_map.Values.OrderBy(x => x.CASNumber).ToList();
            return result;
        }

        public bool Store(CASData casdata)
        {
            bool result = casdata.UpdateDatabase(this).Succeeded;
            if (result) CASData.Store(casdata);
            return result;
        }

        public CASData FindCASData(int id)
        {
            if (m_casid_map.ContainsKey(id)) return m_casid_map[id];
            else return null;
        }


        //#################################################################
        //
        // Storage Locations
        //
        //#################################################################

        public void RefreshStorageLocations()
        {
            m_storage_locations.Clear();
            Console.WriteLine("Refreshing storage locations");
            DatabaseResult rc = ExecuteQuery("SELECT * FROM StorageLocations ORDER BY Name", (reader) =>
            {
                int id = DBHelper.GetIntColumn(reader, "StorageLocationID") ?? 0;
                string name = DBHelper.GetStringColumn(reader, "Name");
                StorageLocation loc = new StorageLocation(name);
                loc.StorageLocationID = id;
                loc.ChangeStatus = EChangeStatus.IS_UNCHANGED;
                m_storage_locations.Add(id, loc);
                return true;
            });
            if (rc.Result) Console.WriteLine($"    Read {m_storage_locations.Count} storage locations");
            else Console.WriteLine($"    Query failed: {this.LastError}");
        }

        public List<StorageLocation> FetchStorageLocations()
        {
            return (m_storage_locations.Values.OrderBy(x => x.Name).ToList());
        }

        public StorageLocation FetchStorageLocation(int location_id)
        {
            StorageLocation result = null;
            this.ExecuteQuery(String.Format("SELECT * FROM StorageLocations WHERE StorageLocationID = {0}", location_id), (reader) =>
            {
                string name = DBHelper.GetStringColumn(reader, "Name");
                result = new StorageLocation(name);
                result.StorageLocationID = location_id;
                return false;
            });
            return result;
        }

        public bool DeleteStorageLocation(int location_id)
        {
            DatabaseResult rc = ExecuteNonQuery($"DELETE FROM StorageLocations WHERE StorageLocationID = {location_id}", null);
            RefreshStorageLocations();
            StorageLocation.ReadStorageLocations(this);
            return (rc.RowsRead == 1);
        }

        public bool Store(StorageLocation location)
        {
            bool is_new = (location.StorageLocationID == 0);
            bool result = location.UpdateDatabase(this).Succeeded;
            if (is_new) m_storage_locations.Add(location.StorageLocationID, location);
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       AddStorageLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Add a storage location if it does not already exist
        /// </summary>
        ///
        /// <param name="name">location name</param>
        /// <param name="refresh">true if all the locations should be refreshed</param>
        /// <returns>the identity of the added row, 0 if it already exists, or -1 if there was an error</returns>
        ///
        ///----------------------------------------------------------------
        public int AddStorageLocation(string name, bool refresh)
        {
            int result = -1;
            if (StorageLocation.FindByName(name) == null)
            {
                List<DatabaseQueryParameter> parameters = new List<DatabaseQueryParameter>();
                parameters.Add(new DatabaseQueryParameter("name", name));
                DatabaseResult rc = this.ExecuteNonQuery("INSERT INTO StorageLocations (Name) VALUES(:name)", parameters, null);
                if (rc.RowsRead == 1)
                {
                    result = (int)this.GetGlobalIdentity(null);
                    if (refresh) StorageLocation.ReadStorageLocations(this);
                    this.LogActivity("Inserted storage location \"{0}\"", name);
                }
            }
            else
            {
                result = 0;
            }
            return result;
        }

        public StorageLocation FindStorageLocation(int id)
        {
            if (m_storage_locations.ContainsKey(id)) return m_storage_locations[id];
            else return null;
        }




        //#################################################################
        //
        // Storage Groups
        //
        //#################################################################

        public void RefreshStorageGroups()
        {
            m_storage_groups.Clear();
            Console.WriteLine("Refreshing storage groups");
            DatabaseResult rc = ExecuteQuery("SELECT * FROM StorageGroups ORDER BY Name", (reader) =>
            {
                int id = DBHelper.GetIntColumn(reader, "StorageGroupID") ?? 0;
                string name = DBHelper.GetStringColumn(reader, "Name");
                StorageGroup group = new StorageGroup(name, id);
                m_storage_groups.Add(id, group);
                return true;
            });
            if (rc.Result) Console.WriteLine($"    Read {m_storage_groups.Count} storage groups");
            else Console.WriteLine($"    Query failed: {this.LastError}");
        }

        public List<StorageGroup> FetchStorageGroups()
        {
            return (m_storage_groups.Values.OrderBy(x => x.Name).ToList());
        }

        public StorageGroup FetchStorageGroup(int group_id)
        {
            StorageGroup result = null;
            this.ExecuteQuery(String.Format("SELECT * FROM StorageGroups WHERE StorageGroupID = {0}", group_id), (reader) =>
            {
                string name = DBHelper.GetStringColumn(reader, "Name");
                result = new StorageGroup(name, group_id);
                return false;
            });
            return result;
        }

        public bool DeleteStorageGroup(int storage_group_id)
        {
            DatabaseResult rc = ExecuteNonQuery($"DELETE FROM StorageGroups WHERE StorageGroupID = {storage_group_id}", null);
            RefreshStorageGroups();
            StorageGroup.ReadStorageGroups(this);
            return (rc.RowsRead == 1);
        }

        public bool Store(StorageGroup group)
        {
            bool is_new = (group.StorageGroupID == 0);
            bool result = group.UpdateDatabase(this).Succeeded;
            if (is_new) m_storage_groups.Add(group.StorageGroupID, group);
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       AddStorageGroup
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Add a storage group if it does not already exist
        /// </summary>
        ///
        /// <param name="name">group name</param>
        /// <param name="refresh">true to read in all the groups</param>
        /// <returns>the identity of the added row, 0 if it already exists, or -1 if there was an error</returns>
        ///
        ///----------------------------------------------------------------
        public int AddStorageGroup(string name, bool refresh)
        {
            int result = -1;
            if (StorageGroup.FindByName(name) == null)
            {
                List<DatabaseQueryParameter> parameters = new List<DatabaseQueryParameter>();
                parameters.Add(new DatabaseQueryParameter("name", name));
                DatabaseResult rc = this.ExecuteNonQuery("INSERT INTO StorageGroups (Name) VALUES(:name)", parameters, null);
                if (rc.RowsRead == 1)
                {
                    result = (int)GetGlobalIdentity(null);
                    if (refresh) StorageGroup.ReadStorageGroups(this);
                    this.LogActivity("Inserted storage group \"{0}\"", name);
                }
            }
            else
            {
                result = 0;
            }
            return result;
        }


        public StorageGroup FindStorageGroup(int id)
        {
            if (m_storage_groups.ContainsKey(id)) return m_storage_groups[id];
            else return null;
        }



        //#################################################################
        //
        // Logins
        //
        //#################################################################

        public void RefreshLogins()
        {
            m_logins.Clear();
            ExecuteQuery("SELECT * FROM Logins", (reader) =>
            {
                Login login = new Login(reader);
                m_logins.Add(login.LoginID, login);
                return true;
            });
        }


        public List<Login> FetchLogins()
        {
            return m_logins.Values.OrderBy(x => x.LoginName).ToList();
        }

        public Login FindByLoginName(string name)
        {
            Login result = m_logins.Values.FirstOrDefault(x => x.LoginName == name);
            return result;
        }

        public bool Exists(string name)
        {
            return (FindByLoginName(name) != null);
        }

        public bool DeleteLogin(int login_id)
        {
            DatabaseResult rc = ExecuteNonQuery($"DELETE FROM Logins WHERE LoginID = {login_id}", null, null);
            if (rc.RowsRead == 1)
            {
                RefreshLogins();
                Login.ReadLogins(this);
                return true;
            }
            else return false;
        }



        //#################################################################
        //
        // Owners
        //
        //#################################################################

        public void RefreshOwners()
        {
            m_owners.Clear();
            Owner.ReadOwners(this);
            foreach (var owner in Owner.All())
            {
                m_owners.Add(owner.OwnerID, owner);
            }
        }

        public bool Store(Owner owner)
        {
            bool result = true;
            if (owner.OwnerID == 0)
            {
                InsertStmt stmt = new InsertStmt("Owners");
                stmt.AddPlaceholder("name", "Name", owner.Name);
                int rc = stmt.Execute(this);
                result = (rc == 1);
                if (result)
                {
                    owner.OwnerID = (int)this.GetGlobalIdentity(null);
                    m_owners.Add(owner.OwnerID, owner);
                }
            }
            else
            {
                UpdateStmt stmt = new UpdateStmt("Owners", "OwnerID", owner.OwnerID);
                stmt.AddPlaceholder("name", "Name", owner.Name);
                result = (stmt.Execute(this) == 1);
            }
            return result;
        }

        public List<Owner> FetchOwners()
        {
            return m_owners.Values.OrderBy(x => x.Name).ToList();
        }


        public Owner FindById(int id)
        {
            Owner result = null;
            if (m_owners.TryGetValue(id, out result)) return result;
            else return null;
        }

        public Owner FindByName(string name)
        {
            Owner result = m_owners.Values.FirstOrDefault(x => x.Name == name);
            return result;
        }

        public bool DeleteOwner(int owner_id)
        {
            DatabaseResult rc = ExecuteNonQuery($"DELETE FROM Owners WHERE OwnerID = {owner_id}", null);
            RefreshOwners();
            return (rc.RowsRead == 1);
        }

        public static bool OwnerExists(string name)
        {
            return (Owner.FindByName(name) != null);
        }

        public static Owner FindOwner(int owner_id)
        {
            return Owner.FindById(owner_id);
        }



        //#################################################################
        //
        // Inventory Items
        //
        //#################################################################

        public List<InventoryItem> FetchInventory()
        {
            List<InventoryItem> result = new List<InventoryItem>();
            string sql = @"select I.*, L.Name as Location, G.Name as StorageGroupName, O.Name as Owner 
                           from InventoryItems I
                           left outer join StorageLocations L on I.LocationID = L.StorageLocationID
                           left outer join StorageGroups G on I.StorageGroupID = G.StorageGroupID
                           left outer join Owners O on I.OwnerID = O.OwnerID";
            DatabaseResult rc = ExecuteQuery(sql, (reader) =>
            {
                InventoryItem item = new InventoryItem();
                item.InventoryID = DBHelper.GetIntColumn(reader, "InventoryID") ?? 0;
                item.Barcode = DBHelper.GetStringColumn(reader, "Barcode");
                item.LocationID = DBHelper.GetIntColumn(reader, "LocationID") ?? 0;
                if (item.LocationID > 0) item.Location = DBHelper.GetStringColumn(reader, "Location");
                item.OwnerID = DBHelper.GetIntColumn(reader, "OwnerID") ?? 0;
                if (item.OwnerID > 0) item.Owner = DBHelper.GetStringColumn(reader, "Owner");
                //DateIn = DBHelper.GetStringColumn(reader, "DateIn");
                item.DateIn = DBHelper.GetDateTimeString(reader, "DateIn");
                //ExpirationDate = DBHelper.GetStringColumn(reader, "ExpirationDate");
                item.ExpirationDate = DBHelper.GetDateTimeString(reader, "ExpirationDate");
                item.ChemicalName = DBHelper.GetStringColumn(reader, "ChemicalName");
                item.CASNumber = DBHelper.GetStringColumn(reader, "CASNumber");
                item.StorageGroupID = DBHelper.GetIntColumn(reader, "StorageGroupID") ?? 0;
                if (item.StorageGroupID > 0) item.StorageGroupName = DBHelper.GetStringColumn(reader, "StorageGroupName");
                item.ContainerSize = DBHelper.GetDoubleColumn(reader, "ContainerSize");
                item.RemainingQuantity = DBHelper.GetDoubleColumn(reader, "RemainingQuantity");
                item.Units = DBHelper.GetStringColumn(reader, "Units");
                item.State = DBHelper.GetStringColumn(reader, "State");
                if (!String.IsNullOrWhiteSpace(item.CASNumber)) item.MSDS = String.Format("msds_{0}.pdf", item.CASNumber.ToLower());
                else item.MSDS = "";
                item.Notes = DBHelper.GetStringColumn(reader, "Notes");

                string flags = DBHelper.GetStringColumn(reader, "Flags", false);
                //System.Diagnostics.Trace.WriteLine(Barcode + ": \"" + Flags + "\"");
                while (flags.Length < 13) flags += ' ';
                if (flags.Length > 13) flags = flags.Substring(0, 13);
                item.Flags = flags;
                item.UpdateFlagValues();

                item.ChangeStatus = EChangeStatus.IS_UNCHANGED;
                result.Add(item);
                return true;
            });
            if (rc.Result == false) Console.WriteLine($"Unable to read inventory items: {this.LastError}");
            return result;
        }

        public InventoryItem FindByBarcode(string barcode)
        {
            InventoryItem result = null;
            ExecuteQuery("SELECT * FROM InventoryItems WHERE Barcode = '" + barcode + "'", (reader) =>
            {
                result = new InventoryItem(reader);
                return false;
            });
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       FindInventoryItems
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find inventory items that match a WHERE expression
        /// </summary>
        ///
        /// <param name="where_expr">a where expression</param>
        /// <returns>a possibly empty list if InventoryIDs</returns>
        ///
        ///----------------------------------------------------------------
        private List<int> FindInventoryItems(string where_expr)
        {
            List<int> result = new List<int>();
            ExecuteQuery($"SELECT InventoryID FROM InventoryItems WHERE {where_expr}", (reader) =>
            {
                int id = Convert.ToInt32(reader[0]);
                result.Add(id);
                return true;
            });
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindInventoryByOwner
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find inventory items that refer to the given owner id
        /// </summary>
        ///
        /// <param name="owner_id">the OwnerID to look for</param>
        /// <returns>a possibly empty list if InventoryIDs</returns>
        ///
        ///----------------------------------------------------------------
        public List<int> FindInventoryByOwner(int owner_id)
        {
            return FindInventoryItems($"OwnerID = {owner_id}");
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindInventoryByLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find inventory items that refer to the given location
        /// </summary>
        ///
        /// <param name="location_id">the LocationID to look for</param>
        /// <returns>a possibly empty list if InventoryIDs</returns>
        ///
        ///----------------------------------------------------------------
        public List<int> FindInventoryByLocation(int location_id)
        {
            return FindInventoryItems($"LocationID = {location_id}");
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindInventoryByGroup
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find inventory items that refer to the given storage group
        /// </summary>
        ///
        /// <param name="group_id">the GroupID to look for</param>
        /// <returns>a possibly empty list if InventoryIDs</returns>
        ///
        ///----------------------------------------------------------------
        public List<int> FindInventoryByGroup(int group_id)
        {
            return FindInventoryItems($"StorageGroupID = {group_id}");
        }



        public bool Store(InventoryItem item)
        {
            bool result = item.UpdateDatabase(this).Succeeded;
            return result;
        }

        public bool Delete(InventoryItem item)
        {
            string sql = String.Format("DELETE FROM InventoryItems WHERE InventoryID = {0}", item.InventoryID);
            DatabaseResult rc = ExecuteNonQuery(sql, null, null);
            return (rc.RowsRead == 1);
        }

        public static String FormatDate(DateTime date)
        {
            return String.Format("{0:yyyy-MM-dd HH:mm:ss}", date);
        }

        public void ResetStockCheck()
        {
            string last_check = GetSetting("current_stock_check", null);
            if (last_check != null) SetSetting("last_stock_check", last_check);
            SetSetting("current_stock_check", "");
            this.ExecuteNonQuery("UPDATE InventoryItems SET InventoryStatus = 0, LastInventoryDate = NULL, StockCheckLocation = ''", null);
            this.ClearOrphans();
        }

        //public void SetInventoryStatus(string barcode, int status)
        //{
        //    string sql = String.Format("UPDATE InventoryItems SET InventoryStatus = {0} WHERE Barcode = '{1}'", status, barcode);
        //    Execute(sql, null, null);
        //}


        //#################################################################
        //
        // STOCK CHECK / ORPHANS
        //
        //#################################################################

        public List<OrphanInventoryItem> FetchOrphans()
        {
            List<OrphanInventoryItem> result = new List<OrphanInventoryItem>();
            string sql = "select * from Orphans O where not exists (select * from InventoryItems II where II.Barcode = O.Barcode)";
            ExecuteQuery(sql, (reader) =>
            {
                OrphanInventoryItem orphan = new OrphanInventoryItem(reader);
                result.Add(orphan);
                return true;
            });
            return result;
        }

        public void DeleteOrphan(string barcode)
        {
            string sql = String.Format("DELETE FROM Orphans WHERE Barcode = '{0}'", barcode);
            ExecuteNonQuery(sql, null, null);
        }

        public void ClearOrphans()
        {
            ExecuteNonQuery("DELETE FROM Orphans", null, null);
        }

        public OrphanInventoryItem CreateOrphan(string barcode, string expected_location)
        {
            OrphanInventoryItem result = new OrphanInventoryItem
            {
                Barcode = barcode,
                ExpectedLocation = expected_location,
                InventoryDate = DateTime.Now
            };
            result.Store(this);
            return result;
        }

        public static bool IsBarcodeValid(string barcode, out string error_message)
        {
            if (barcode.Contains("'"))
            {
                error_message = "Quote marks are not valid in bar codes.";
                return false;
            }
            error_message = "";
            return true;
        }

        //#################################################################
        //
        // Settings
        //
        //#################################################################

        public String GetSetting(String name, String default_value)
        {
            String result = default_value;
            ExecuteQuery(String.Format("SELECT * FROM Settings WHERE SettingKey = '{0}'", name), (reader) =>
            {
                result = reader["SettingValue"].ToString().Trim();
                return false;
            });
            return result;
        }

        public int GetIntSetting(String name, int default_value)
        {
            int result = default_value;
            String valstr = GetSetting(name, null);
            if (valstr != null)
            {
                int temp;
                if (Int32.TryParse(valstr, out temp)) result = temp;
            }
            return result;
        }


        public void SetSetting(String key, String value)
        {
            List<DatabaseQueryParameter> parameters = new List<DatabaseQueryParameter>();
            parameters.Add(new DatabaseQueryParameter("VALUE", value));
            if (RowExists($"SELECT * FROM Settings WHERE SettingKey = '{key}'"))
            {
                ExecuteNonQuery(String.Format("UPDATE Settings SET SettingValue = @VALUE WHERE SettingKey = '{0}'", key), parameters, null);
            }
            else
            {
                ExecuteNonQuery(String.Format("INSERT INTO Settings (SettingKey, SettingValue) VALUES('{0}', @VALUE)", key), parameters, null);
            }
        }



        //#################################################################
        //
        // Activity Log
        //
        //#################################################################

        public bool LogActivity(string fmt, params object[] args)
        {
            string description = String.Format(fmt, args);
            InsertStmt stmt = new InsertStmt("ActivityLog");
            stmt.AddField("LogDate", FormatDate(DateTime.Now));
            stmt.AddPlaceholder("user", "User", CurrentUser);
            stmt.AddPlaceholder("description", "Description", description);
            int rc = stmt.Execute(this);
            return (rc == 1);
        }
    }

    public abstract class CIMSData
    {
        public EChangeStatus ChangeStatus { get; set; }
        public bool IsModified { get { return ChangeStatus != EChangeStatus.IS_UNCHANGED; } }
        public string ErrorMessage { get; set; }

        public CIMSData()
        {
            ChangeStatus = EChangeStatus.IS_NEW;
        }

        public DbResult UpdateDatabase(LegacyDatabase db)
        {
            DbResult rc;
            try
            {
                rc = DoUpdateDatabase(db);
                if (rc.Succeeded) ChangeStatus = EChangeStatus.IS_UNCHANGED;
                else rc.ErrorMessage = db.LastError;
            }
            catch (Exception ex)
            {
                rc = new DbResult();
                while (ex.InnerException != null) ex = ex.InnerException;
                ErrorMessage = ex.Message;
                rc.ErrorMessage = ErrorMessage;
            }
            return rc;
        }

        public abstract bool IsEqualTo(CIMSData other);

        protected abstract DbResult DoUpdateDatabase(LegacyDatabase db);
    }

    public class DbResult
    {
        public bool Succeeded { get; set; }
        public int RowsUpdated { get; set; }
        public string ErrorMessage { get; set; }
    }



    public class CASData : CIMSData
    {
        public int CASID { get; set; }
        public string CASNumber { get; set; }
        public string ChemicalName { get; set; }
        public char CWCFlag { get; set; }
        public char TheftFlag { get; set; }
        public char CarcinogenFlag { get; set; }


        public CASData() { }

        public CASData(DbDataReader reader)
        {
            CASID = Convert.ToInt32(reader["CASID"]);
            CASNumber = reader["CASNumber"].ToString().Trim();
            if (reader["ChemicalName"] != DBNull.Value) ChemicalName = reader["ChemicalName"].ToString().Trim();
            if (reader["CWCFlag"] != DBNull.Value) CWCFlag = reader["CWCFlag"].ToString()[0];
            if (reader["TheftFlag"] != DBNull.Value) TheftFlag = reader["TheftFlag"].ToString()[0];
            if (reader["CarcinogenFlag"] != DBNull.Value) CarcinogenFlag = reader["CarcinogenFlag"].ToString()[0];
        }

        public void Show()
        {
            Console.WriteLine($"CASID: {this.CASID}");
            Console.WriteLine($"    CASNumber:    {this.CASNumber}");
            Console.WriteLine($"    ChemicalName: {this.ChemicalName}");
            Console.WriteLine($"    CWCFlag:      {this.CWCFlag}");
            Console.WriteLine($"    TheftFlag:    {this.TheftFlag}");
            Console.WriteLine($"    CarcFlag:     {this.CarcinogenFlag}");
        }

        public override bool IsEqualTo(CIMSData obj)
        {
            CASData other = obj as CASData;
            return (other != null && this.CASID == other.CASID && this.CASNumber == other.CASNumber && this.ChemicalName == other.ChemicalName);
        }

        protected override DbResult DoUpdateDatabase(LegacyDatabase db)
        {
            DbResult result = new DbResult();
            List<DatabaseQueryParameter> query_parameters = new List<DatabaseQueryParameter>();
            query_parameters.Add(new DatabaseQueryParameter("name", ChemicalName));
            string sql;
            if (CASID == 0)
            {
                sql = String.Format("INSERT INTO CASData (CASNumber, ChemicalName, CWCFlag, TheftFlag, CarcinogenFlag) VALUES ('{0}', :name, '{1}', '{2}', '{3}')",
                                    CASNumber, CWCFlag, TheftFlag, CarcinogenFlag);
            }
            else
            {
                sql = String.Format("UPDATE CASData SET CASNumbewr = '{0}', ChemicalName = :name, CWCFlag = '{1}', TheftFlag = '{2}', CarcinogenFag = '{3}' WHERE CASID = {4}",
                      CASNumber, CWCFlag, TheftFlag, CarcinogenFlag, CASID);
            }
            int rc = db.ExecuteNonQuery(sql, query_parameters, null).RowsRead;
            result.Succeeded = (rc == 1);
            result.RowsUpdated = rc;
            if (rc == 1)
            {
                if (CASID == 0) CASID = (int)db.GetGlobalIdentity(null);
            }
            return result;
        }

        public static List<CASData> ReadCASData(LegacyDatabase db)
        {
            List<CASData> result = new List<CASData>();
            s_casid_map.Clear();
            s_casnumber_map.Clear();
            db.ExecuteQuery("SELECT * FROM CASData", (reader) =>
            {
                CASData data = new CASData(reader);
                s_casid_map[data.CASID] = data;
                s_casnumber_map[data.CASNumber] = data;
                result.Add(data);
                return true;
            });
            return result;
        }

        public static CASData Find(string casnumber)
        {
            CASData result = null;
            if (casnumber != null)
            {
                if (s_casnumber_map.TryGetValue(casnumber.Trim(), out result)) return result;
            }
            return null;
        }

        public static CASData Find(int casid)
        {
            CASData result = null;
            if (s_casid_map.TryGetValue(casid, out result)) return result;
            return null;
        }

        public static char FindCWCFlag(string casnumber)
        {
            CASData data = Find(casnumber);
            if (data != null) return data.CWCFlag;
            else return ' ';
        }

        public static char FindTheftFlag(string casnumber)
        {
            CASData data = Find(casnumber);
            if (data != null) return data.TheftFlag;
            else return ' ';
        }

        public static char FindCarcinogenFlag(string casnumber)
        {
            CASData data = Find(casnumber);
            if (data != null) return data.CarcinogenFlag;
            else return ' ';
        }

        public static void Store(CASData data)
        {
            if (data.CASID > 0) s_casid_map[data.CASID] = data;
            s_casnumber_map[data.CASNumber] = data;
        }

        public static void Store(int casid, string casnumber, string chemical_name, char cwc, char theft, char carcinogen)
        {
            CASData data = CASData.Find(casnumber);
            if (data == null)
            {
                data = new CASData();
            }
            if (casid > 0) data.CASID = casid;
            data.CASNumber = casnumber;
            data.ChemicalName = chemical_name;
            data.CWCFlag = cwc;
            data.TheftFlag = theft;
            data.CarcinogenFlag = carcinogen;
            CASData.Store(data);
        }


        private static Dictionary<int, CASData> s_casid_map = new Dictionary<int, CASData>();
        private static Dictionary<string, CASData> s_casnumber_map = new Dictionary<string, CASData>();

    }


    public class StorageLocation : CIMSData
    {
        public int StorageLocationID { get; set; }
        public string Name { get; set; }

        public static List<StorageLocation> Locations { get { return s_storage_locations.Values.OrderBy(x => x.Name).ToList(); } }
        private static Dictionary<int, StorageLocation> s_storage_locations = new Dictionary<int, StorageLocation>();

        public StorageLocation(string name = null)
        {
            Name = name;
        }

        public override bool IsEqualTo(CIMSData obj)
        {
            StorageLocation other = obj as StorageLocation;
            return (other != null && this.StorageLocationID == other.StorageLocationID && this.Name == other.Name);
        }

        public static StorageLocation FindByName(string name)
        {
            return Locations.FirstOrDefault(x => x.Name == name);
        }

        public static StorageLocation Add(string name, LegacyDatabase db)
        {
            StorageLocation result = StorageLocation.FindByName(name);
            if (result == null)
            {
                InsertStmt stmt = new InsertStmt("StorageLocations");
                stmt.AddPlaceholder("name", "Name", name);
                int rc = stmt.Execute(db);
                if (rc == 1)
                {
                    StorageLocation.ReadStorageLocations(db);
                    return StorageLocation.FindByName(name);
                }
            }
            return result;
        }

        protected override DbResult DoUpdateDatabase(LegacyDatabase db)
        {
            DbResult result = new DbResult();
            List<DatabaseQueryParameter> parameters = new List<DatabaseQueryParameter>();
            parameters.Add(new DatabaseQueryParameter("name", Name));
            string sql = "INSERT INTO StorageLocations (Name) VALUES (:name)";
            if (StorageLocationID > 0) sql = String.Format("UPDATE StorageLocations SET Name = :name WHERE StorageLocationID = {0}", StorageLocationID);
            int rc = db.ExecuteNonQuery(sql, parameters, null).RowsRead;
            result.Succeeded = (rc == 1);
            result.RowsUpdated = rc;
            if (rc == 1)
            {
                if (StorageLocationID == 0) StorageLocationID = (int)db.GetGlobalIdentity(null);
            }
            return result;
        }

        public static void ReadStorageLocations(LegacyDatabase db)
        {
            s_storage_locations = new Dictionary<int, StorageLocation>();
            db.ExecuteQuery("SELECT * FROM StorageLocations", (reader) =>
            {
                int id = DBHelper.GetIntColumn(reader, "StorageLocationID") ?? 0;
                string name = DBHelper.GetStringColumn(reader, "Name");
                //if (id == 0) throw new Exception("Invalid storage location: ID is zero");
                //if (String.IsNullOrEmpty(name)) throw new Exception("Invalid storage location: Name is empty");
                StorageLocation loc = new StorageLocation(name);
                loc.StorageLocationID = id;
                s_storage_locations.Add(id, loc);
                loc.ChangeStatus = EChangeStatus.IS_UNCHANGED;
                return true;
            });
        }

        public static void ReadStorageLocations()
        {
            LegacyDatabase db = LegacyDatabase.OpenCurrent();
            if (db.IsOpen)
            {
                ReadStorageLocations(db);
            }
        }

        public static List<StorageLocation> All()
        {
            List<StorageLocation> result = s_storage_locations.OrderBy(x => x.Value.Name).Select(x => x.Value).ToList();
            return result;
        }

        public static StorageLocation Get(int id)
        {
            StorageLocation result = null;
            if (s_storage_locations.Count == 0) ReadStorageLocations();
            if (s_storage_locations.TryGetValue(id, out result)) return result;
            else return null;
        }

        public static int Find(string name)
        {
            int result = 0;
            name = name.ToLower().Trim();
            if (s_storage_locations.Count == 0) ReadStorageLocations();
            var loc = s_storage_locations.Values.FirstOrDefault(x => x.Name.ToLower() == name);
            if (loc != null) result = loc.StorageLocationID;
            return result;
        }
    }

    public class StorageGroup : CIMSData
    {
        public int StorageGroupID { get; set; }
        public string Name { get; set; }

        private static Dictionary<int, StorageGroup> s_storage_groups = new Dictionary<int, StorageGroup>();

        public StorageGroup(string name = null, int storage_group_id = 0)
        {
            Name = name;
            StorageGroupID = storage_group_id;
        }

        public override bool IsEqualTo(CIMSData obj)
        {
            StorageGroup other = obj as StorageGroup;
            return (other != null && this.StorageGroupID == other.StorageGroupID && this.Name == other.Name);
        }

        public static StorageGroup FindByName(string name)
        {
            return s_storage_groups.Values.FirstOrDefault(x => x.Name == name);
        }

        public static StorageGroup Add(string name, LegacyDatabase db)
        {
            StorageGroup result = StorageGroup.FindByName(name);
            if (result == null)
            {
                InsertStmt stmt = new InsertStmt("StorageGroups");
                stmt.AddPlaceholder("name", "Name", name);
                int rc = stmt.Execute(db);
                if (rc == 1)
                {
                    StorageGroup.ReadStorageGroups(db);
                    return FindByName(name);
                }
            }
            return result;
        }

        protected override DbResult DoUpdateDatabase(LegacyDatabase db)
        {
            DbResult result = new DbResult();
            List<DatabaseQueryParameter> parameters = new List<DatabaseQueryParameter>();
            parameters.Add(new DatabaseQueryParameter("name", Name));
            string sql = "INSERT INTO StorageGroups (Name) VALUES (:name)";
            if (StorageGroupID > 0) sql = String.Format("UPDATE StorageGroups SET Name = :name WHERE StorageGroupID = {0}", StorageGroupID);
            int rc = db.ExecuteNonQuery(sql, parameters, null).RowsRead;
            result.Succeeded = (rc == 1);
            result.RowsUpdated = rc;
            if (rc == 1)
            {
                if (StorageGroupID == 0) StorageGroupID = (int)db.GetGlobalIdentity(null);
            }
            return result;
        }



        public static void ReadStorageGroups(LegacyDatabase db)
        {
            s_storage_groups = new Dictionary<int, StorageGroup>();
            db.ExecuteQuery("SELECT * FROM StorageGroups", (reader) =>
            {
                int id = DBHelper.GetIntColumn(reader, "StorageGroupID") ?? 0;
                string name = DBHelper.GetStringColumn(reader, "Name");
                //if (id == 0) throw new Exception("Invalid storage location: ID is zero");
                //if (String.IsNullOrEmpty(name)) throw new Exception("Invalid storage location: Name is empty");
                StorageGroup group = new StorageGroup(name);
                group.ChangeStatus = EChangeStatus.IS_UNCHANGED;
                group.StorageGroupID = id;
                s_storage_groups.Add(id, group);
                return true;
            });
        }

        public static void ReadStorageGroups()
        {
            LegacyDatabase db = LegacyDatabase.OpenCurrent();
            if (db.IsOpen)
            {
                ReadStorageGroups(db);
            }
        }


        public static List<StorageGroup> All()
        {
            List<StorageGroup> result = s_storage_groups.OrderBy(x => x.Value.Name).Select(x => x.Value).ToList();
            return result;
        }


        public static StorageGroup Get(int id)
        {
            StorageGroup result = null;
            if (s_storage_groups.Count == 0) ReadStorageGroups();
            if (s_storage_groups.TryGetValue(id, out result)) return result;
            else return null;
        }

        public static int Find(string name)
        {
            int result = 0;
            name = name.ToLower().Trim();
            if (s_storage_groups.Count == 0) ReadStorageGroups();
            var loc = s_storage_groups.Values.FirstOrDefault(x => x.Name.ToLower() == name);
            if (loc != null) result = loc.StorageGroupID;
            return result;
        }

    }

    public class Login
    {
        public int LoginID { get; set; }
        public string LoginName { get; set; }
        public string PasswordHash { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsOwner { get; set; }

        public const string ADMIN_ROLE = "admin";
        public const string MANAGER_ROLE = "manager";
        public const string AUDITOR_ROLE = "auditor";
        public const string VIEWER_ROLE = "viewer";

        public bool IsAdmin { get { return (Role.ToLower() == ADMIN_ROLE); } }
        public bool IsManager { get { return (Role.ToLower() == MANAGER_ROLE); } }
        public bool IsAuditor { get { return (Role.ToLower() == AUDITOR_ROLE); } }
        public bool IsViewer { get { return (Role.ToLower() == VIEWER_ROLE); } }

        public bool CanEditInventory { get { return !IsViewer; } }
        public bool CanStockCheck { get { return !IsViewer; } }

        // s_logins will never be null
        private static Dictionary<int, Login> s_logins = new Dictionary<int, Login>();

        public Login(string name = null)
        {
            LoginName = name;
        }

        public Login(DbDataReader reader)
        {
            LoginID = DBHelper.GetIntColumn(reader, "LoginID") ?? 0;
            LoginName = DBHelper.GetStringColumn(reader, "LoginName");
            Name = DBHelper.GetStringColumn(reader, "Name");
            Role = DBHelper.GetStringColumn(reader, "Role");
            PasswordHash = DBHelper.GetStringColumn(reader, "PasswordHash");
        }

        public bool Add(LegacyDatabase db)
        {
            if (this.LoginID != 0) throw (new Exception("Cannot add Login that already has a LoginID"));
            InsertStmt stmt = new InsertStmt("Logins");
            stmt.AddPlaceholder("login", "LoginName", LoginName);
            stmt.AddPlaceholder("pswd", "PasswordHash", PasswordHash);
            stmt.AddPlaceholder("name", "Name", Name);
            stmt.AddPlaceholder("role", "Role", Role);
            if (stmt.Execute(db) != 1) return false;
            else return true;
        }

        public bool Update(LegacyDatabase db)
        {
            if (this.LoginID == 0) throw (new Exception("Cannot update unsaved logins"));
            UpdateStmt stmt = new UpdateStmt("Logins", "LoginID", this.LoginID);
            stmt.AddPlaceholder("login", "LoginName", LoginName);
            stmt.AddPlaceholder("pswd", "PasswordHash", PasswordHash);
            stmt.AddPlaceholder("name", "Name", Name);
            stmt.AddPlaceholder("role", "Role", Role);
            if (stmt.Execute(db) != 1) return false;
            else return true;
        }

        public static void ReadLogins(LegacyDatabase db)
        {
            s_logins = new Dictionary<int, Login>();
            db.ExecuteQuery("SELECT * FROM Logins", (reader) =>
            {
                Login login = new Login(reader);
                s_logins.Add(login.LoginID, login);
                return true;
            });
        }

        public static List<Login> All()
        {
            return s_logins.Values.OrderBy(x => x.LoginName).ToList();
        }

        public static Login FindByLoginName(string name)
        {
            Login result = s_logins.Values.FirstOrDefault(x => x.LoginName == name);
            return result;
        }

        public static bool Exists(string name)
        {
            return (FindByLoginName(name) != null);
        }

        public static Login Get(int id)
        {
            Login result = null;
            if (s_logins.TryGetValue(id, out result)) return result;
            else return null;
        }
    }

    public class Owner
    {
        public int OwnerID { get; set; }
        public string Name { get; set; }
        public bool IsModified { get; set; }

        // s_logins will never be null
        private static Dictionary<int, Owner> s_owners = new Dictionary<int, Owner>();

        public Owner(string name = null)
        {
            Name = name;
            IsModified = false;
        }

        public Owner(DbDataReader reader)
        {
            OwnerID = DBHelper.GetIntColumn(reader, "OwnerID") ?? 0;
            Name = DBHelper.GetStringColumn(reader, "Name");
            IsModified = false;
        }


        public bool Add(LegacyDatabase db)
        {
            if (this.OwnerID != 0) throw (new Exception("Cannot add an Owner that already has an OwnerID"));
            InsertStmt stmt = new InsertStmt("Owners");
            stmt.AddPlaceholder("name", "Name", Name);
            if (stmt.Execute(db) != 1) return false;
            else
            {
                this.OwnerID = (int)db.GetGlobalIdentity(null);
                IsModified = false;
                return true;
            }
        }

        public DbResult Update(LegacyDatabase db)
        {
            DbResult result = new DbResult();
            UpdateStmt stmt = new UpdateStmt("Owners", "OwnerID", OwnerID);
            stmt.AddPlaceholder("name", "Name", Name);
            if (stmt.Execute(db) != 1)
            {
                result.ErrorMessage = "Update failed";
            }
            else
            {
                IsModified = false;
                result.Succeeded = true;
            }
            return result;
        }

        public static void ReadOwners(LegacyDatabase db)
        {
            s_owners = new Dictionary<int, Owner>();
            db.ExecuteQuery("SELECT * FROM Owners", (reader) =>
            {
                Owner owner = new Owner(reader);
                s_owners.Add(owner.OwnerID, owner);
                return true;
            });
        }

        public static List<Owner> All()
        {
            return s_owners.Values.OrderBy(x => x.Name).ToList();
        }


        public static Owner FindById(int id)
        {
            Owner result = null;
            if (s_owners.TryGetValue(id, out result)) return result;
            else return null;
        }

        public static Owner FindByName(string name)
        {
            Owner result = s_owners.Values.FirstOrDefault(x => x.Name == name);
            return result;
        }

        public static bool Exists(string name)
        {
            return (Owner.FindByName(name) != null);
        }

        public static Owner Add(string name, LegacyDatabase db)
        {
            Owner result = Owner.FindByName(name);
            if (result != null) throw (new Exception("Owner '" + name + "' already exists."));
            InsertStmt stmt = new InsertStmt("Owners");
            stmt.AddPlaceholder("name", "Name", name);
            int rc = stmt.Execute(db);
            Owner.ReadOwners(db);
            return FindByName(name);
        }
    }

    public class InventoryItem : CIMSData
    {
        public enum EFLAG
        {
            CWC = 0, THEFT, OTHERSECURITY, CARCINOGEN, HEALTHHAZARD, IRRITANT,
            ACUTETOXICITY, CORROSIVE, EXPLOSIVE, FLAMABLE, OXIDIZER, COMPRESSEDGAS, OTHERHAZARD
        }

        public const int INVENTORY_UNDEFINED = 0;
        public const int INVENTORY_FOUND = 1;
        public const int INVENTORY_MISSING = 2;
        public const int INVENTORY_MISPLACED = 3;

        private char[] m_flags = new char[13];

        public int InventoryID { get; set; }
        public string Barcode { get; set; }

        public int LocationID { get; set; }
        public string Location { get; set; }

        public int OwnerID { get; set; }
        public string Owner { get; set; }

        public int StorageGroupID { get; set; }
        public string StorageGroupName { get; set; }

        public string DateIn { get; set; }
        public string ExpirationDate { get; set; }
        public string ChemicalName { get; set; }
        public int CASID { get; set; }              // foreign key in CASData
        public string CASNumber { get; set; }       // comes from CASData
        public double? ContainerSize { get; set; }
        public double? RemainingQuantity { get; set; }
        public string Units { get; set; }
        public string State { get; set; }
        public int InventoryStatus { get; set; }
        public DateTime LastInventoryDate { get; set; }
        public string StockCheckLocation { get; set; }

        public string Flags { get { return new String(m_flags); } set { m_flags = value.ToCharArray(); } }

        public char CWC { get { return m_flags[(int)EFLAG.CWC]; } set { m_flags[(int)EFLAG.CWC] = value; } }

        public char Theft { get { return m_flags[(int)EFLAG.THEFT]; } set { m_flags[(int)EFLAG.THEFT] = value; } }

        public char OtherSecurity { get { return m_flags[(int)EFLAG.OTHERSECURITY]; } set { m_flags[(int)EFLAG.OTHERSECURITY] = value; } }

        public char Carcinogen { get { return m_flags[(int)EFLAG.CARCINOGEN]; } set { m_flags[(int)EFLAG.CARCINOGEN] = value; } }

        public char HealthHazard { get { return m_flags[(int)EFLAG.HEALTHHAZARD]; } set { m_flags[(int)EFLAG.HEALTHHAZARD] = value; } }

        public char Irritant { get { return m_flags[(int)EFLAG.IRRITANT]; } set { m_flags[(int)EFLAG.IRRITANT] = value; } }

        public char AcuteToxicity { get { return m_flags[(int)EFLAG.ACUTETOXICITY]; } set { m_flags[(int)EFLAG.ACUTETOXICITY] = value; } }

        public char Corrosive { get { return m_flags[(int)EFLAG.CORROSIVE]; } set { m_flags[(int)EFLAG.CORROSIVE] = value; } }

        public char Explosive { get { return m_flags[(int)EFLAG.EXPLOSIVE]; } set { m_flags[(int)EFLAG.EXPLOSIVE] = value; } }

        public char Flamable { get { return m_flags[(int)EFLAG.FLAMABLE]; } set { m_flags[(int)EFLAG.FLAMABLE] = value; } }

        public char Oxidizer { get { return m_flags[(int)EFLAG.OXIDIZER]; } set { m_flags[(int)EFLAG.OXIDIZER] = value; } }

        public char CompressedGas { get { return m_flags[(int)EFLAG.COMPRESSEDGAS]; } set { m_flags[(int)EFLAG.COMPRESSEDGAS] = value; } }

        public char OtherHazard { get { return m_flags[(int)EFLAG.OTHERHAZARD]; } set { m_flags[(int)EFLAG.OTHERHAZARD] = value; } }

        public string MSDS { get; set; }
        public string Notes { get; set; }

        // non-database fields
        public object HealthHazardTooltip { get; private set; }

        public InventoryItem()
        {
            CWC = ' ';
            Theft = ' ';
            OtherSecurity = ' ';
            Carcinogen = ' ';
            HealthHazard = ' ';
            Irritant = ' ';
            AcuteToxicity = ' ';
            Corrosive = ' ';
            Explosive = ' ';
            Flamable = ' ';
            Oxidizer = ' ';
            CompressedGas = ' ';
            OtherHazard = ' ';
            InventoryStatus = 0;
            LastInventoryDate = DateTime.MinValue;
            StockCheckLocation = "";
            m_flags = (new String(' ', 13)).ToCharArray();
        }

        public override bool IsEqualTo(CIMSData obj)
        {
            InventoryItem other = obj as InventoryItem;
            return (other != null && this.InventoryID == other.InventoryID && this.Barcode == other.Barcode && this.m_flags == other.m_flags && this.MSDS == other.MSDS && this.Notes == other.Notes && this.LocationID == other.LocationID && this.StorageGroupID == other.StorageGroupID && this.OwnerID == other.OwnerID);
        }



        public InventoryItem(DbDataReader reader)
        {
            InventoryID = DBHelper.GetIntColumn(reader, "InventoryID") ?? 0;
            Barcode = DBHelper.GetStringColumn(reader, "Barcode");
            LocationID = DBHelper.GetIntColumn(reader, "LocationID") ?? 0;
            StorageLocation location = StorageLocation.Get(LocationID);
            Location = (location == null ? null : location.Name);
            StockCheckLocation = DBHelper.GetStringColumn(reader, "StockCheckLocation");
            OwnerID = DBHelper.GetIntColumn(reader, "OwnerID") ?? 0;
            if (OwnerID > 0)
            {
                Owner owner = LegacyDatabase.FindOwner(OwnerID);
                if (owner != null) Owner = owner.Name;
            }
            //DateIn = DBHelper.GetStringColumn(reader, "DateIn");
            DateIn = DBHelper.GetDateTimeString(reader, "DateIn");
            //ExpirationDate = DBHelper.GetStringColumn(reader, "ExpirationDate");
            ExpirationDate = DBHelper.GetDateTimeString(reader, "ExpirationDate");
            ChemicalName = DBHelper.GetStringColumn(reader, "ChemicalName");
            CASNumber = DBHelper.GetStringColumn(reader, "CASNumber");
            StorageGroupID = DBHelper.GetIntColumn(reader, "StorageGroupID") ?? 0;
            StorageGroup group = StorageGroup.Get(StorageGroupID);
            StorageGroupName = (group == null ? null : group.Name);
            ContainerSize = DBHelper.GetDoubleColumn(reader, "ContainerSize");
            RemainingQuantity = DBHelper.GetDoubleColumn(reader, "RemainingQuantity");
            Units = DBHelper.GetStringColumn(reader, "Units");
            State = DBHelper.GetStringColumn(reader, "State");
            if (!String.IsNullOrWhiteSpace(CASNumber)) MSDS = String.Format("{0}.pdf", CASNumber.ToLower());
            else MSDS = "";
            InventoryStatus = DBHelper.GetIntColumn(reader, "InventoryStatus") ?? 0;
            LastInventoryDate = DBHelper.GetDateTimeColumn(reader, "LastInventoryDate") ?? DateTime.MinValue;
            Notes = DBHelper.GetStringColumn(reader, "Notes");

            Flags = DBHelper.GetStringColumn(reader, "Flags", false);
            //System.Diagnostics.Trace.WriteLine(Barcode + ": \"" + Flags + "\"");
            while (Flags.Length < 13) Flags += ' ';
            if (Flags.Length > 13) Flags = Flags.Substring(0, 13);
            m_flags = Flags.ToCharArray();
            UpdateFlagValues();

            ChangeStatus = EChangeStatus.IS_UNCHANGED;
            //System.Diagnostics.Trace.WriteLine(String.Format("{0} -> {1}", Barcode, DateIn));
        }

        public InventoryItem InitializeFrom(InventoryItem other)
        {
            if (other.Location != null) SetLocation(other.Location);
            OwnerID = other.OwnerID;
            Owner = other.Owner;
            ExpirationDate = other.ExpirationDate;
            ChemicalName = other.ChemicalName;
            CASNumber = other.CASNumber;
            StorageGroupID = other.StorageGroupID;
            StorageGroupName = other.StorageGroupName;
            ContainerSize = other.ContainerSize;
            RemainingQuantity = 0;
            Units = other.Units;
            State = other.State;
            MSDS = other.MSDS;
            CWC = other.CWC;
            Theft = other.Theft;
            OtherSecurity = other.OtherSecurity;
            Carcinogen = other.Carcinogen;
            HealthHazard = other.HealthHazard;
            Irritant = other.Irritant;
            AcuteToxicity = other.AcuteToxicity;
            Corrosive = other.Corrosive;
            Explosive = other.Explosive;
            Flamable = other.Flamable;
            Oxidizer = other.Oxidizer;
            CompressedGas = other.CompressedGas;
            OtherHazard = other.HealthHazard;
            InventoryStatus = InventoryItem.INVENTORY_UNDEFINED;
            LastInventoryDate = DateTime.MinValue;
            Flags = other.Flags;
            m_flags = other.m_flags;
            return this;
        }


        public static InventoryItem FindByBarcode(string barcode, LegacyDatabase db)
        {
            InventoryItem result = null;
            db.ExecuteQuery("SELECT * FROM InventoryItems WHERE Barcode = '" + barcode + "'", (reader) => {
                result = new InventoryItem(reader);
                return false;
            });
            return result;
        }


        public static bool Add(InventoryItem item, LegacyDatabase db)
        {
            bool result = item.DoUpdateDatabase(db).Succeeded;
            return result;
        }

        public bool Delete(LegacyDatabase db)
        {
            string sql = String.Format("DELETE FROM InventoryItems WHERE InventoryID = {0}", this.InventoryID);
            int rc = db.ExecuteNonQuery(sql, null, null).RowsRead;
            return (rc == 1);
        }

        public void SetLocation(string location)
        {
            StorageLocation loc = StorageLocation.FindByName(location);
            if (loc != null)
            {
                this.Location = loc.Name;
                this.LocationID = loc.StorageLocationID;
            }
        }

        public void SetLocationID()
        {
            StorageLocation loc = StorageLocation.FindByName(Location);
            if (loc != null)
            {
                this.Location = loc.Name;
                this.LocationID = loc.StorageLocationID;
            }
        }

        public void SetInventoryStatus(int status, LegacyDatabase db)
        {
            this.InventoryStatus = status;
            string sql = String.Format("UPDATE InventoryItems SET InventoryStatus = {0} WHERE InventoryID = '{1}'", status, this.InventoryID);
            DoUpdateDatabase(db);
        }

        protected override DbResult DoUpdateDatabase(LegacyDatabase db)
        {
            DbResult result = new DbResult();
            SQLStatement stmt;
            if (InventoryID == 0) stmt = new InsertStmt("InventoryItems");
            else stmt = new UpdateStmt("InventoryItems", "InventoryID", InventoryID);

            if (CASNumber == null) CASNumber = " ";
            stmt.AddField("CASNumber", CASNumber);
            stmt.AddField("Barcode", Barcode);
            stmt.AddPlaceholder("chem", "ChemicalName", ChemicalName);
            stmt.AddField("DateIn", DateIn);
            stmt.AddField("ExpirationDate", ExpirationDate);
            if (OwnerID > 0) stmt.AddField("OwnerID", OwnerID);
            if (LocationID > 0) stmt.AddField("LocationID", LocationID);
            if (StorageGroupID > 0) stmt.AddField("StorageGroupID", StorageGroupID);
            if (ContainerSize.HasValue) stmt.AddField("ContainerSize", ContainerSize.Value);
            if (RemainingQuantity.HasValue) stmt.AddField("RemainingQuantity", RemainingQuantity.Value);
            stmt.AddField("Units", Units);
            stmt.AddField("State", State);
            stmt.AddField("Flags", Flags);
            stmt.AddField("InventoryStatus", InventoryStatus);
            stmt.AddField("StockCheckLocation", StockCheckLocation);
            if (!String.IsNullOrWhiteSpace(Notes)) stmt.AddPlaceholder("notes", "Notes", Notes);
            int rc = stmt.Execute(db);
            result.Succeeded = (rc == 1);
            result.RowsUpdated = rc;
            if (rc == 1 && InventoryID == 0) InventoryID = (int)db.GetGlobalIdentity(null);
            return result;
        }

        public void UpdateFlagValues()
        {
            m_flags[(int)EFLAG.CWC] = CASData.FindCWCFlag(CASNumber);
            m_flags[(int)EFLAG.THEFT] = CASData.FindTheftFlag(CASNumber);
            m_flags[(int)EFLAG.CARCINOGEN] = CASData.FindCarcinogenFlag(CASNumber);
        }

        public void RebuildFlagString()
        {
            Flags = new string(m_flags);
        }

    }

    public class OrphanInventoryItem
    {
        public string Barcode { get; set; }
        public string ExpectedLocation { get; set; }
        public DateTime InventoryDate { get; set; }

        public OrphanInventoryItem() { }
        public OrphanInventoryItem(DbDataReader reader)
        {
            Barcode = DBHelper.GetStringColumn(reader, "Barcode");
            ExpectedLocation = DBHelper.GetStringColumn(reader, "ExpectedLocation");
            InventoryDate = DBHelper.GetDateTimeColumn(reader, "InventoryDate").Value;
        }

        public void Store(LegacyDatabase db)
        {
            string sql = String.Format("INSERT INTO Orphans (Barcode, ExpectedLocation, InventoryDate) VALUES ('{0}', '{1}', '{2}')", Barcode, ExpectedLocation, LegacyDatabase.FormatDate(InventoryDate));
            db.ExecuteNonQuery(sql, null, null);
        }
    }
}
