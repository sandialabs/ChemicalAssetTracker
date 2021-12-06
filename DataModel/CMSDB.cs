using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;


//#################################################################
//
// CMSDB - Entity Framework context for CMS database access
//
// Main Methods
//
//     Location Methods
//         GetUserSubtree - read all locations rooted at a given location
//         FindLocation - read a specific location
//     Inventory Methods
//         GetInventory - read inventory for a whole subtree
//         GetLocalInventory - read just inventory in a specific location
//
//#################################################################

namespace DataModel
{
    public class CMSDB : DbContext
    {
        public const string CurrentSiteKey = "CurrentSite";
        //public const string InstitutionKey = "Institution";
        public const string StockCheckDateKey = "StockCheckDate";
        public const string LocationSchemaKey = "System.Site.LocationSchema";
        public const string MaxInventoryRowsKey = "System.MaxInventoryRows";
        public const string AnnouncementKey = "System.Announcement";
        public const string TempDirKey = "System.TempDir";
        public const string SearchLevelKey = "System.SearchLevel";
        public const string CreateImportLocationsKey = "System.CreateImportLocations";

        public delegate bool QueryCallback(DbDataReader reader);

        private LocationSchema s_location_schema = null;

        public DbSet<Owner> Owners { get; set; }
        private DbSet<StorageLocation> StorageLocations { get; set; }
        public DbSet<LocationLevelName> LocationLevelNames { get; set; }
        public DbSet<StorageGroup> StorageGroups { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<RemovedItem> RemovedItems { get; set; }
        public DbSet<InventoryStatus> InventoryStatusNames { get; set; }
        public DbSet<CASData> CASDataItems { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        //public DbSet<UserLocationPermission> UserLocationPermissions { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<DatabaseQuery> DatabaseQueries { get; set; }
        public DbSet<GHSData> GHSClassifications { get; set; }
        public DbSet<ChemicalOfConcern> ChemicalsOfConcern { get; set; }
        public DbSet<ReportDefinition> ReportDefinitions { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<LocationType> LocationTypes { get; set; }

        public DbSet<HazardCode> HazardCodes { get; set; }

        public DbSet<DisposalProcedure> DisposalProcedures { get; set; }

        public DbSet<CASDisposalProcedure> CASDisposalProcedures { get; set; }

        public List<ReportDefinition> Reports { get; set; } = new List<ReportDefinition>();

        public DbSet<InventoryAudit> InventoryAudits { get; set; }

        public static ConnectionSettings ConnectionSettings;

        public static Dictionary<string, string> CountryToCountryCodeMap { get; private set; }
        public static Dictionary<string, string> CountryCodeToCountryMap { get; private set; }
        public static Dictionary<string, CASData> CASDataMap { get; private set; }
        public static int MaxInventoryRows = 1000;

        private static string s_connection_string;
        private static string s_database_hostname;
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Map CASNumber => true (those that are false are not included)
        /// </summary>
        private static Dictionary<string, bool> s_chemicals_of_concern;
        public Dictionary<string, bool> COC
        {
            get
            {
                if (s_chemicals_of_concern == null)
                {
                    s_chemicals_of_concern = new Dictionary<string, bool>();
                    foreach (ChemicalOfConcern coc in this.ChemicalsOfConcern)
                    {
                        s_chemicals_of_concern[coc.CASNumber] = coc.CWC || coc.CFATS || coc.EU || coc.AG || coc.WMD || coc.OTHER;
                    }
                }
                return s_chemicals_of_concern;
            }
        }

        public static UserSubtreeCache s_subtree_cache = new UserSubtreeCache();

        private LocationManager _location_manager;
        public LocationManager Locations
        {
            get
            {
                if (_location_manager == null) _location_manager = new LocationManager(this);
                return _location_manager;
            }
        }

        public delegate void ReadLocationDelegate(StorageLocation location);

        //-------------------------------------------------------------
        //
        // LOCATION STRINGS
        //
        // Location strings are made up of 5 parts:
        //    Institution
        //    Site
        //    Building
        //    Room
        //    Shelf
        //
        // Location strings can be fully specified or partially specified.
        // Partially specified locations assume a prespecified Institution and Site.
        // Partial location strings will have just Building, Room, and Shelf.
        // All parts of location string must be specified. If any of the
        // last two (Room and Shelf) are not not specified, you must use an 
        // asterisk for a placeholder.  If you use * for Room you must also use
        // * for Shelf
        // For example:
        //     Bldg823/211/X   is a valid partially spedified location
        //     823/211/*       is a valid partially specified location - somewhere in room 211
        //     Storage/*/*     is a valid partially specified location - somewhere in Storage
        //     Storage/*/X     is not valid
        //     Storage/*       is not valid
        //     NNSA/SNL/823/111/X is a valid fully specified location
        //     NNSA/SNL/823/111/* is a valid fully specified location
        //     NNSA/SNL/823/*/* is a valid fully specified location
        //
        // Note that you can have an item at Bldg/Room/X and another at
        // Bldg/Room/*, but these are treated as separate locations. No
        // wildcard matching is performed.
        //
        //-------------------------------------------------------------


        /// <summary>
        /// Total number of components in a fully-qualified location
        /// </summary>
        public const int LOCATION_LENGTH = 5;
        /// <summary>
        /// Number of components in the fixed part of a location (INSTITUTION and SITE)
        /// </summary>
        public const int LOCATION_PREFIX_LENGTH = 2;
        /// <summary>
        /// Number of components in the local part of a location string (BUILDING, ROOM, SHELF)
        /// </summary>
        public const int LOCATION_SUFFIX_LENGTH = 3;

        public static readonly string[] LEVEL_NAMES = { "Institution", "Site", "Building", "Room", "Shelf" };

        static CMSDB()
        {
            CountryCodeToCountryMap = new Dictionary<string, string>();
            CountryToCountryCodeMap = new Dictionary<string, string>();
            CASDataMap = new Dictionary<string, CASData>();

            foreach (string def in s_un_country_codes)
            {
                string[] parts = def.Split('|');
                CountryCodeToCountryMap[parts[0]] = parts[1];
                CountryToCountryCodeMap[parts[1]] = parts[0];
            }
        }



        public void RefreshLocations()
        {
            Locations.Initialize(this);
            s_subtree_cache.Clear();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       EnsureSeeded
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Make sure the database tables and procedures have been initialized
        /// </summary>
        ///
        /// <returns>true iff seeding was needed</returns>
        ///
        ///----------------------------------------------------------------
        public bool EnsureSeeded()
        {
            bool result = false;

            if (InventoryStatusNames.CountAsync().Result == 0)
            {
                Console.WriteLine("Initializing the InventoryStatusNames table");
                // UNKNOWN = 0, CONFIRMED, CONFIRMED_AT_NEW_LOCATION, NOT_FOUND
                string[] values = { "Unknown", "Confirmed", "Moved", "Not Found" };
                for (int i = 0; i < values.Length; i++)
                {
                    InventoryStatusNames.Add(new InventoryStatus { InventoryStatusID = i, Name = values[i] });
                }
                result = true;
            }
            if (LocationLevelNames.CountAsync().Result == 0)
            {
                Console.WriteLine("Initializing the LocationLevelNames table");
                LocationLevelNames.Add(new LocationLevelName(0, "Institution"));
                LocationLevelNames.Add(new LocationLevelName(1, "University"));
                LocationLevelNames.Add(new LocationLevelName(2, "Department"));
                LocationLevelNames.Add(new LocationLevelName(3, "Building"));
                LocationLevelNames.Add(new LocationLevelName(4, "Room"));
                LocationLevelNames.Add(new LocationLevelName(5, "Storage"));
                LocationLevelNames.Add(new LocationLevelName(6, "Shelf"));
            }
            if (!DbContextHelper.FunctionExists(this, "cms", "GetLocationPath"))
            {
                Console.WriteLine("Creating the GetLocationName function");
                Database.ExecuteSqlCommand(s_getlocationpath_func);
            }


            SaveChanges();

            return result;
        }

        public Dictionary<string, string> GetConnectionSettings()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] parts = s_connection_string.Split(';');
            foreach (string part in parts)
            {
                string[] nameval = part.Split('=');
                if (nameval.Length == 2) result.Add(nameval[0].ToLower(), nameval[1]);
            }
            return result;
        }

        public static string GetHostname()
        {
            return s_database_hostname;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetReportDefinitions
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get report definitions
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns>a List of ReportDefinition objects</returns>
        /// 
        /// <remarks>
        /// This method currently returns hard-coded definitions.  In 
        /// the future we need to read them from the database.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public List<ReportDefinition> GetReportDefinitions()
        {
            return ReportDefinitions.OrderBy(x => x.ReportName).ToList();
        }

        public void RunQuery(string sql, QueryCallback callback)
        {
            using (DbCommand command = this.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                //var parameter = new SqlParameter("@p1",...);
                //command.Parameters.Add(parameter);

                this.Database.OpenConnection();

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (callback(reader) == false) break;
                    }
                }
            }
        }



        protected void banner(params string[] lines)
        {
            string line1 = new string('#', 72);
            string line2 = "# ";
            Console.WriteLine(" ");
            Console.WriteLine(line1);
            Console.WriteLine(line2);
            foreach (string line in lines) Console.WriteLine("# " + line);
            Console.WriteLine(line2);
            Console.WriteLine(line1);
            Console.WriteLine(" ");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options_builder)
        {
            if (!options_builder.IsConfigured)
            {
                string connection_string = Configuration.GetConnectionString("CMSConnection");
                if (connection_string == null)
                {
                    string user = "cms";
                    string pswd = "cms";
                    string host = "localhost";
                    if (ConnectionSettings != null)
                    {
                        user = ConnectionSettings.Username;
                        pswd = ConnectionSettings.Password;
                        host = ConnectionSettings.Hostname;
                    }
                    connection_string = $"server={host};database=cms;user={user};password={pswd}";
                }
                //string safe_string = connection_string.Replace("password=cms", "password=*********");
                //string[] parts = safe_string.Split(";");
                //banner("Configuring connection string: ", "    " + parts[0], "    " + parts[1], "    " + parts[2], "    " + parts[3]);
                options_builder.UseMySql(connection_string);
                s_connection_string = connection_string;
                var settings = GetConnectionSettings();
                if (settings.ContainsKey("server")) s_database_hostname = settings["server"];
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StorageLocation>().HasIndex(x => x.ParentID);
            modelBuilder.Entity<InventoryStatus>().Property(x => x.InventoryStatusID).ValueGeneratedNever();
            modelBuilder.Entity<InventoryItem>().Property(x => x.MaterialType).HasDefaultValue(EMaterialType.CHEMICAL);
            modelBuilder.Entity<InventoryItem>().HasIndex(x => x.LocationID);
            modelBuilder.Entity<InventoryItem>().Property(x => x.IsOtherCOC).HasDefaultValue(false);
            modelBuilder.Entity<ChemicalOfConcern>().HasIndex(x => x.CASNumber);
            modelBuilder.Entity<CASData>().HasIndex(x => x.CASNumber);
            modelBuilder.Entity<HazardCode>().HasIndex(x => x.CASNumber);
            modelBuilder.Entity<HazardCode>().HasIndex(x => x.GHSCode);
            modelBuilder.Entity<CASDisposalProcedure>().HasIndex(x => x.CASNumber);

            //modelBuilder.Entity<GHSData>().HasIndex(x => x.CASNumber);
            // putting an index on a large text column causes mysql error 
            //modelBuilder.Entity<GHSData>().HasIndex(x => x.ChemicalName);
            modelBuilder.Entity<Attachment>().HasIndex(x => x.Login);
            base.OnModelCreating(modelBuilder);
        }



        //#################################################################
        //
        // Inventory Items
        //
        //#################################################################

        public Task<InventoryItem> GetItemByBarcodeAsync(string barcode)
        {
            return InventoryItems.FirstOrDefaultAsync(x => x.Barcode == barcode);
        }

        public InventoryItem GetItemByBarcode(string barcode)
        {
            InventoryItem result = null;
            if (!String.IsNullOrEmpty(barcode))
            {
                result = GetItemByBarcodeAsync(barcode).Result;
                if (result != null) result.InitializeItemFlags(this);
            }
            return result;
        }

        public InventoryItem GetItem(int item_id)
        {
            InventoryItem result = InventoryItems.Include(x => x.Group).Include(x => x.Owner).FirstOrDefault(x => x.InventoryID == item_id);
            if (result != null) result.InitializeItemFlags(this);
            return result;
        }


        public List<InventoryItem> GetInventory(int root_id)
        {
            MaxInventoryRows = GetIntSetting(MaxInventoryRowsKey, MaxInventoryRows);

            // get the location subtree rooted at root_id
            // each node in the subtree contains the user's permission for that node in "Access"
            StorageLocation root = Locations.FindLocation(root_id);
            var location_subtree = GetSubtree(root, 99);

            List<InventoryItem> result = InventoryItems.Include(x => x.Location)
                .Include(x => x.Group)
                .Include(x => x.Owner)
                .OrderBy(x => x.Barcode)
                .Take(MaxInventoryRows)
                .ToList();
            // initialize CAS flags (pictographs)
            result.ForEach(x => x.InitializeItemFlags(this).ExpandLocation(this, false));
            return result;
        }

        public List<InventoryItem> GetLocalInventory(int location_id, int maxrows = 0)
        {
            if (maxrows == 0) maxrows = MaxInventoryRows;

            List<InventoryItem> result;
            result = InventoryItems.Include(x => x.Location)
                .Include(x => x.Group)
                .Include(x => x.Owner)
                .Where(x => x.LocationID == location_id)
                .OrderBy(x => x.Barcode)
                .Take(maxrows)
                .ToList();
            result.ForEach(x => x.InitializeItemFlags(this).ExpandLocation(this, false));
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       SearchInventory
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Fetch all inventory items that match search criteria
        /// </summary>
        ///
        /// <param name="login_name">logged in user name</param>
        /// <param name="root_id">logged in user's root directory</param>
        /// <param name="settings">search criteria</param>
        /// <returns>list of inventory items</returns>
        ///
        /// <remarks>
        /// Note: the settings argument is updated go include paging information
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public List<InventoryItem> SearchInventory(string login_name, int root_id, InventorySearchSettings settings)
        {
            MaxInventoryRows = GetIntSetting(MaxInventoryRowsKey, MaxInventoryRows);
            if (settings.ItemsPerPage == 0) settings.ItemsPerPage = MaxInventoryRows;

            // see if this user subtree has been cached
            LocationSubtree subtree = GetCachedLocations(login_name, root_id);
            int[] valid_locations = subtree.Locations.OrderBy(x => x.LocationLevel).Select(x => x.LocationID).ToArray();

            // build the query
            var query = InventoryItems.Include(x => x.Location)
                .Include(x => x.Group)
                .Include(x => x.Owner)
                .Where(x => x.LocationID > 0);

            if (string.IsNullOrEmpty(settings.BarCode) == false)
                query = query.Where(x => x.Barcode.Contains(settings.BarCode));
            if (string.IsNullOrEmpty(settings.Chemical) == false)
                query = query.Where(x => x.ChemicalName.Contains(settings.Chemical));
            if (string.IsNullOrEmpty(settings.CASNumber) == false)
                query = query.Where(x => x.CASNumber.Contains(settings.CASNumber));
            if (string.IsNullOrEmpty(settings.Owner) == false)
                query = query.Where(x => x.Owner.Name.ToLower().Contains(settings.Owner));
            query = query.Where(x => valid_locations.Contains(x.LocationID));


            // if we don't know how many rows will match, find out now
            if (settings.IsInitialQuery) settings.ItemsMatched = query.Count();

            // fetch the next page of data from the query
            List<InventoryItem> result = query
                .OrderBy(x => x.Barcode)
                .Skip(settings.ResultOffset)
                .Take(settings.ItemsPerPage)
                .ToList();
            result.ForEach(x => x.InitializeItemFlags(this).ExpandLocation(this, false));
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       SearchInventoryNoauth
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Fetch all inventory items that match search criteria, ignoring 
        /// visibility restrictions
        /// </summary>
        ///
        /// <param name="root_id">logged in user's root directory</param>
        /// <param name="settings">search criteria</param>
        /// <returns>list of inventory items</returns>
        /// 
        /// <remarks>
        /// Note: the settings argument is updated go include paging information
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public List<InventoryItem> SearchInventoryNoauth(int root_id, InventorySearchSettings settings)
        {
            MaxInventoryRows = GetIntSetting(MaxInventoryRowsKey, MaxInventoryRows);
            if (settings.ItemsPerPage == 0) settings.ItemsPerPage = MaxInventoryRows;

            LocationSubtree subtree = GetSearchSubtree(root_id);
            int[] valid_location_ids = subtree.Locations.Select(x => x.LocationID).ToArray();
            // build the query
            var query = InventoryItems
                .Include(x => x.Group)
                .Include(x => x.Owner)
                .Where(x => valid_location_ids.Contains(x.LocationID));

            if (string.IsNullOrEmpty(settings.BarCode) == false)
                query = query.Where(x => x.Barcode.Contains(settings.BarCode));
            if (string.IsNullOrEmpty(settings.Chemical) == false)
                query = query.Where(x => x.ChemicalName.Contains(settings.Chemical));
            if (string.IsNullOrEmpty(settings.CASNumber) == false)
                query = query.Where(x => x.CASNumber.Contains(settings.CASNumber));
            if (string.IsNullOrEmpty(settings.Owner) == false)
                query = query.Where(x => x.Owner.Name.ToLower().Contains(settings.Owner));

            // if we don't know how many rows will match, find out now
            if (settings.IsInitialQuery) settings.ItemsMatched = query.Count();

            // fetch the next page of data from the query
            List<InventoryItem> result = query
                .OrderBy(x => x.Barcode)
                .Skip(settings.ResultOffset)
                .Take(settings.ItemsPerPage)
                .ToList();
            result.ForEach(x => x.InitializeItemFlags(this));
            return result;
        }




        public void DeleteItem(InventoryItem item, RemovedItem.ERemovalReason reason, string username, bool save_changes)
        {
            StorageLocation loc = this.FindLocation(item.LocationID);
            string location = (loc == null ? "unknown" : loc.Path);
            RemovedItem removed = new RemovedItem(item, reason);
            RemovedItems.Add(removed);
            InventoryItems.Remove(item);
            LogInfo(username, "delete", $"Deleted {item.Barcode} -  InventoryID: {item.InventoryID}, location: {location} ({item.LocationID}),  reason: {reason}", false);
            if (save_changes) SaveChanges();
        }

        public void MoveItem(InventoryItem item, string username, int new_location_id, bool save_changes)
        {
            int current_location_id = item.LocationID;
            StorageLocation fromloc = this.FindLocation(current_location_id);
            StorageLocation toloc = this.FindLocation(new_location_id);
            string location1 = (fromloc == null ? "unknown" : fromloc.Path);
            string location2 = (toloc == null ? "unknown" : toloc.Path);
            InventoryItem existing = InventoryItems.FirstOrDefault(x => x.InventoryID == item.InventoryID);
            if (existing != null)
            {
                LogInfo(username, "move", $"Moved ${item.Barcode} from {location1} ({current_location_id}) to {location2} ({new_location_id})", false);
                existing.LocationID = new_location_id;
                if (save_changes) SaveChanges();
            }
            else
            {
                LogError(username, "move", $"Unable to move {item.Barcode}: LocationID {item.LocationID} is not in the database", true);
            }
        }

        //#################################################################
        //
        // CASData
        //
        //#################################################################


        ///----------------------------------------------------------------
        ///
        /// Function:       GetCASData
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a CASData record corresponding to a given CAS number
        /// </summary>
        ///
        /// <param name="casnumber">the CAS number of interest</param>
        /// <returns>a CASData instance or null</returns>
        /// 
        /// <remarks>
        /// A static map of CAS numbers to CASData instances is used
        /// to reduce the number of database queries.  It is initialized
        /// on the first call to this function.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public CASData GetCASData(string casnumber)
        {
            CASData result = null;
            if (!String.IsNullOrEmpty(casnumber))
            {
                // populate the static map if necessary
                if (CASDataMap.Count == 0)
                {
                    lock (CASDataMap)
                    {
                        foreach (var item in this.CASDataItems)
                        {
                            if (!CASDataMap.ContainsKey(item.CASNumber))
                            {
                                CASDataMap.Add(item.CASNumber, item);
                            }
                        }
                    }
                }
                CASDataMap.TryGetValue(casnumber, out result);
            }
            return result;
        }




        //#################################################################
        //
        // StorageLocations
        //
        //#################################################################


        ///----------------------------------------------------------------
        ///
        /// Function:       GetCachedLocations
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a list of cached locations
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public LocationSubtree GetCachedLocations(string login_name, int root_id)
        {
            // see if this user subtree has been cached
            LocationSubtree subtree = s_subtree_cache.GetCachedSubtree(login_name, root_id);
            if (subtree == null)
            {
                // get the location subtree rooted at root_id
                // each node in the subtree contains the user's permission for that node in "Access"
                StorageLocation root = Locations.FindLocation(root_id);
                subtree = GetSubtree(root, 99);
                // get a list of LocationIDs in the subtree that the user can view
                s_subtree_cache.AddToCache(login_name, root_id, subtree);
            }
            return subtree;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetLocationCount
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Return the number of storage locations in the database
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns>an integer count</returns>
        ///
        ///----------------------------------------------------------------
        public int GetLocationCount(int location_type_id = 0)
        {
            if (location_type_id == 0) return StorageLocations.Count();
            else return StorageLocations.Where(x => x.LocationTypeID == location_type_id).Count();
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetLocationsOfType
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Return an array of storage locations with a given LocationTypeID
        /// </summary>
        ///
        /// <param name="location_type_id">the LocationTypeID value to look for</param>
        /// <returns>an array of StorageLocations</returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation[] GetLocationsOfType(int location_type_id)
        {
            return StorageLocations.Where(x => x.LocationTypeID == location_type_id).ToArray();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetUserLocations
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get all locations that a user can view
        /// </summary>
        ///
        /// <param name="login">user login name</param>
        /// <param name="root_id">user's home location</param>
        /// <returns>list of StorageLocations</returns>
        /// 
        /// <remarks>
        /// No permission checking is performed by this function.
        /// - per-location user permissions have been scrapped
        /// - view/edit distinction is not currently supported
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public List<StorageLocation> GetUserLocations(string login, int root_id)
        {
            LocationSubtree subtree = GetCachedLocations(login, root_id);
            return subtree.Locations.ToList();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetSearchSubtree
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the user's subtree for the search function
        /// </summary>
        ///
        /// <param name="location_id">LocationID for where to start looking</param>
        /// <param name="search_location_level">the LocationLevel of the root of the subtree</param>
        /// <returns>a LocationSubtree instance</returns>
        /// 
        /// <remarks>
        /// This routines uses the SearchLevel setting to determine the
        /// location level of a search subtree.  If the location passed in
        /// has a higher level, this method is called recursively to traverse
        /// up the location tree.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public LocationSubtree GetSearchSubtree(int location_id, int search_location_level = -1)
        {
            if (search_location_level < 0) search_location_level = GetIntSetting("SearchLevel", 1);
            StorageLocation loc = Locations[location_id];
            if (loc.ParentID > 0 && loc.LocationLevel > search_location_level)
            {
                return GetSearchSubtree(loc.ParentID);
            }
            else
            {
                return Locations.GetSubtree(location_id);
            }
        }

        public StorageLocation ReadLocation(int location_id)
        {
            var result = StorageLocations.Include(x => x.LocationType).FirstOrDefault(x => x.LocationID == location_id);
            return result;
        }

        public List<StorageLocation> ReadLocations()
        {
            List<StorageLocation> locations = StorageLocations.Include(x => x.LocationType).OrderBy(x => x.LocationID).ToList();
            return locations;
        }

        public void ReadLocations(ReadLocationDelegate callback)
        {
            List<StorageLocation> locations = StorageLocations.Include(x => x.LocationType).OrderBy(x => x.LocationLevel).ThenBy(x => x.Name).ToList();
            var whatever = locations.FirstOrDefault(x => x.Name == "Whatever");
            if (whatever != null)
            {
                List<StorageLocation> debug = locations.Where(x => x.LocationID > 8770).ToList();
                Console.WriteLine($"{debug.Count}");
            }
            foreach (var loc in locations)
            {
                callback(loc);
            }
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       UpdateLocationPath
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Update the Path field of a StorageLocation and its descendents
        /// </summary>
        ///
        /// <param name="location_id">LocationID of location to update</param>
        ///
        ///----------------------------------------------------------------
        public void UpdateLocationPath(int location_id)
        {
            LocationSubtree subtree = Locations.GetSubtree(location_id);
            List<string> locations_to_update = subtree.Locations.Select(x => Convert.ToString(x.LocationID)).ToList();
            string loclist = String.Join(", ", locations_to_update);
            string sql = $"update StorageLocations set Path = GetLocationPath(LocationID) where LocationID in ({loclist})";
            this.Database.ExecuteSqlCommand(sql);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       UpdateAllLocationPaths
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Update the Path field of every StorageLocation in the database
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public void UpdateAllLocationPaths(bool force = true)
        {
            if (force || StorageLocations.Include(x => x.LocationType).FirstOrDefault(x => x.LocationID == 1).Path == null)
            {
                TaskTimer t = new TaskTimer("timer");
                Database.ExecuteSqlCommand("update StorageLocations set Path = GetLocationPath(LocationID)");
                Locations.GetLocations();
            }
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       UpdateMissingLocationPaths
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Update the Path field of any StorageLocation that has no Path value
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public void UpdateMissingLocationPaths()
        {
            TaskTimer t = new TaskTimer("timer");
            Database.ExecuteSqlCommand("update StorageLocations set Path = GetLocationPath(LocationID) where Path is NULL");
            int[] missing_paths = StorageLocations.Include(x => x.LocationType).Where(x => x.Path == null).Select(x => x.LocationID).ToArray();
            if (missing_paths.Length > 0)
            {
                Console.WriteLine($"Have {missing_paths.Length} null Paths");
            }
            Locations.Refresh(this);
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       ParseLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Split a location path into its component parts
        /// </summary>
        ///
        /// <param name="location_string">the location path to parse</param>
        /// <returns>an array of location names</returns>
        ///
        ///----------------------------------------------------------------
        static public string[] ParseLocation(string location_string)
        {
            if (location_string.StartsWith('/')) location_string = location_string.Substring(1);
            string[] result = location_string.Split('/');
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetRootLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the top-level StorageLocation
        /// </summary>
        ///
        /// <param name=""></param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation GetRootLocation()
        {
            StorageLocation root_location = StorageLocations.FirstOrDefault(x => x.ParentID == 0);
            // create the root location if it hasn't yet been created
            //if (root_location == null)
            //{
            //    var setting = GetSetting(InstitutionKey);
            //    if (setting == null) throw new Exception("Institution name not initialized");
            //    root_location = new StorageLocation()
            //    {
            //        Name = setting.SettingValue,
            //        ParentID = 0,
            //        LocationLevel = 0
            //    };
            //    AddLocation(root_location);
            //    SaveChanges();
            //}
            return root_location;
        }

        public string NormalizeLocation(string location_string)
        {
            // get the institution 
            StorageLocation root_location = GetRootLocation();
            if (root_location == null)
            {
                throw new Exception("Institution name not initialized");
            }
            string institution_name = root_location.Name;

            // find all the site names
            List<string> site_names = Locations.Where(x => x.ParentID == root_location.LocationID).Select(x => x.Name).ToList();

            // break the location string into its parts
            List<string> parts = CMSDB.ParseLocation(location_string).ToList();
            // make sure the location starts with the institution
            if (parts[0] != institution_name) parts.Insert(0, institution_name);
            // make sure the location contains a site name
            if (!site_names.Contains(parts[1]))
            {
                // second component is not a site, use the currentlty selected site
                string current_site = GetStringSetting(CurrentSiteKey, null);
                if (current_site == null)
                {
                    throw new Exception("Site name not initialized");
                }
                parts.Insert(1, current_site);
            }
            while (parts.Count < CMSDB.LOCATION_LENGTH) parts.Add("*");
            return String.Join('/', parts);
        }


        public List<StorageLocation> GetStorageLocations(bool endpoints_only = true, int maxlevel = 99)
        {
            return Locations.GetStorageLocations(endpoints_only, maxlevel);
        }


        public LocationSubtree GetSubtree(StorageLocation root, int depth)
        {
            // get the location subtree
            LocationSubtree result = Locations.GetSubtree(root, depth);
            return result;
        }

        public LocationSubtree GetSubtree(int location_id, int depth)
        {
            StorageLocation root = FindLocation(location_id);
            if (root != null) return GetSubtree(root, depth);
            else return null;
        }

        public List<LocationData> GetStorageLocationData(bool endpoints_only = true)
        {
            List<LocationData> result = new List<LocationData>();
            List<StorageLocation> locations = GetStorageLocations(endpoints_only);
            foreach (var loc in Locations)
            {
                InitializeLocationNames(loc);
                result.Add(new LocationData(loc));
            }
            return result;
        }

        public void UpdateLocationName(int location_id, string newname)
        {
            StorageLocation loc = StorageLocations.FirstOrDefault(x => x.LocationID == location_id);
            if (loc != null)
            {
                loc.Name = newname;
                SaveChanges();
                UpdateLocationPath(loc.LocationID);
                Locations.Initialize(this);
            }
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find a StorageLocation by its LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID to look for</param>
        /// <returns>a StorageLocation object or null</returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation FindLocation(int id)
        {
            StorageLocation result = Locations.Find(id);
            if (result != null) InitializeLocationNames(result);
            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find a StorageLocation by its LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID to look for</param>
        /// <returns>a StorageLocation object or null</returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation FindLocation(string location_name)
        {
            return FindOrAddLocation(location_name, false);
        }


        public StorageLocation AddLocation(StorageLocation location, bool save_changes)
        {
            StorageLocations.Add(location);
            SaveChanges();
            Locations.Add(location);
            UpdateLocationPath(location.LocationID);
            return location;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       AddLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Add a StorageLocation under the given parent, or return the
        /// existing location if one exists
        /// </summary>
        ///
        /// <param name="name">StorageLocation name (no slashes)</param>
        /// <param name="parent_id">the LocationID of the parent</param>
        /// <param name="location_type_id">the LocationTypeID for the location's type</param>
        /// <param name="save_changes">true to save database changes</param>
        /// <returns>a new or existing StorageLocation object</returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation AddLocation(string name, int parent_id, int location_type_id, bool save_changes)
        {
            StorageLocation parent = Locations[parent_id];
            int level = (parent == null ? 1 : parent.LocationLevel + 1);

            // if this location already exists, return it
            StorageLocation existing = Locations.FirstOrDefault(x => x.ParentID == parent_id && x.Name == name);
            if (existing != null) return existing;

            StorageLocation loc = new StorageLocation
            {
                Name = name,
                ParentID = parent_id,
                LocationLevel = level,
                LocationTypeID = location_type_id,
            };
            return AddLocation(loc, save_changes);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       AddLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Add a StorageLocation under the given parent, or return the
        /// existing location if one exists
        /// </summary>
        ///
        /// <param name="name">StorageLocation name (no slashes)</param>
        /// <param name="parent_id">the LocationID of the parent</param>
        /// <param name="save_changes">true to save database changes</param>
        /// <returns>a new or existing StorageLocation object</returns>
        /// 
        /// <remarks>
        /// NOTE: THIS FUNCTION IS NO LONGER VALID BECAUSE ALL LOCATIONS
        /// MUST HAVE A LocationTypeID.  IT IS STILL USED BY AddLocations
        /// IN APIController.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public StorageLocation AddLocation(string name, int parent_id, bool save_changes)
        {
            throw new Exception("CMSDB.AddLocation called with non LocationTypeID");
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       FindOrAddLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find a StorageLocation by its component names, creating a new one of needed
        /// </summary>
        ///
        /// <param name="names">the component names of the location</param>
        /// <param name="cursor">index of the current name we are processing</param>
        /// <param name="allow_add">if false, adding new entries is disabled</param>
        /// <returns>a storage location</returns>
        /// 
        /// <remarks>
        /// The cursor value corresponds to the location level.  Location levels
        /// run from 0..3 coresponding to "Country", "Site", "Bldg" and "Room".
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        private StorageLocation FindOrAddLocation(string[] names, int cursor, bool save_changes, bool allow_add)
        {
            // cursor will go from 0 .. 4
            if (cursor < 0) return null;

            // get my parent's id, creating the parent and ancestors as needed
            int parent_id = 0;
            StorageLocation parent_loc = FindOrAddLocation(names, cursor - 1, save_changes, allow_add);

            if (parent_loc != null) parent_id = parent_loc.LocationID;
            // add a location id for the current location name, adding a new one if necessary
            string this_name = names[cursor];
            // see if a location exists with this name and this parent
            StorageLocation this_loc = Locations.Find(this_name, parent_id);

            if (this_loc == null && allow_add)
            {
                // this is a new location name - add it to the database
                this_loc = new StorageLocation { Name = this_name, ParentID = parent_id, LocationLevel = cursor };
                AddLocation(this_loc, save_changes);
            }
            return this_loc;

        }


        ///----------------------------------------------------------------
        ///
        /// Function:       FindOrAddLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Find or create a location using its fully qualified name
        /// </summary>
        ///
        /// <param name="location_string">a string of the form "aaa/bbb/ccc/ddd"</param>
        /// <param name="allow_add">if false, adding new entries is disabled</param>
        /// <returns>the StorageLocation of "ddd"</returns>
        /// 
        /// <remarks>
        /// Adding "US/Sandia/Bldg821/213" will result in the following rows
        /// in the StorageLocations table.
        ///     Level     Name
        ///     -----     ----------
        ///       0       US
        ///       1       Sandia
        ///       2       Bldg821
        ///       3       213
        ///       
        /// If the location string contains less than 4 parts, it will be
        /// treated as a suffix.  So "aaa/bbb" wil result in:
        ///     Level     Name
        ///     -----     ----------
        ///       0       aaa
        ///       1       bbb
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public StorageLocation FindOrAddLocation(string location_string, bool allow_add = true)
        {
            string[] names = CMSDB.ParseLocation(location_string);
            return FindOrAddLocation(names, names.Length - 1, false, allow_add);
        }


        public void DeleteLocation(int location_id)
        {
            StorageLocation doomed = Locations[location_id];
            LocationSubtree subtree = Locations.GetSubtree(doomed);
            List<StorageLocation> subtree_locations = subtree.Locations;

            int[] location_ids = subtree_locations.Select(x => x.LocationID).ToArray();
            foreach (int id in location_ids)
            {
                if (InventoryItems.Any(x => x.LocationID == id)) throw new Exception($"{Locations.Find(id).FullLocation} is referenced by one or more inventory item");
            }
            StorageLocations.RemoveRange(subtree_locations);
            SaveChanges();
            Locations.Initialize(this);
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       GetLocationName
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the full location name for a given LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID of interest</param>
        /// <param name="minlevel">the minumum level to lookup</param>
        /// <returns>a string of the form "xxx/yyy/..."</returns>
        /// 
        /// <remarks>
        /// If you are looking up a room and you just want the building and room,
        /// use minlevel = 2.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public string GetLocationName(int id, int minlevel = 0)
        {
            return Locations.GetLocationName(id);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetShortLocationName
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the short location name for a given LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID of interest - must be level 3</param>
        /// <returns>a string of the form "xxx/yyy/..."</returns>
        ///
        ///----------------------------------------------------------------

        public string GetShortLocationName(int id)
        {
            string result = null;
            StorageLocation loc = FindLocation(id);
            if (loc == null) throw new Exception($"CMSDB.GetShortLocationName - LocationID {id} does not exist");
            switch (loc.LocationLevel)
            {
                case 0:
                    result = GetLocationName(id);
                    break;
                case 1:
                    result = GetLocationName(id, 1);
                    break;
                default:
                    result = GetLocationName(id, 2);
                    break;
            }
            if (result == null)
            {
                throw new Exception($"CMSDB.GetShortLocationName - unable to generate short name for LocationID {id}");
            }
            return result;

        }

        public string GetFullLocationName(int id)
        {
            return Locations.GetFullLocationName(id);
        }

        public void InitializeLocationNames(StorageLocation loc)
        {
            Locations.InitializeLocationNames(loc);
        }

        public static string SitePart(string location_name, int ix)
        {
            string[] parts = CMSDB.ParseLocation(location_name);
            if (ix < parts.Length) return parts[ix];
            else return "";
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetSite
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a site by name
        /// </summary>
        ///
        /// <param name="location_name">the full name of the site to find</param>
        /// <returns>the name of the site part of the location string</returns>
        ///
        ///----------------------------------------------------------------
        public static string GetSite(string location_name)
        {
            return CMSDB.SitePart(location_name, 1);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetSite
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the site associated with a StorageLocation
        /// </summary>
        ///
        /// <param name="loc">the StorageLocation of interest</param>
        /// <returns>a StorageLocation or null</returns>
        ///
        ///----------------------------------------------------------------
        public StorageLocation GetSite(StorageLocation loc)
        {
            // is this a site?
            if (loc.LocationLevel == 1)
            {
                return loc;
            }
            // does the location have a parent
            if (loc.ParentID > 0)
            {
                // get it from the parent
                return GetSite(this.FindLocation(loc.ParentID));
            }
            else
            {
                // this must be the root location
                return null;
            }
        }

        public string GetSiteName(StorageLocation loc)
        {
            StorageLocation site = GetSite(loc);
            return (site == null ? "" : site.Name);
        }

        public List<StorageLocation> GetLocations(ELocationLevel level = ELocationLevel.SHELF)
        {
            int levelnum = (int)level;
            return Locations.Where(x => x.LocationLevel == levelnum).ToList();
        }





        public LocationSchema GetLocationSchema()
        {
            if (s_location_schema == null)
            {
                string json = GetStringSetting(LocationSchemaKey, null);
                if (json == null) throw new Exception($"GetLocationSchema - no setting named {LocationSchemaKey} exists.");
                s_location_schema = JsonConvert.DeserializeObject<LocationSchema>(json);
            }
            return s_location_schema;
        }


        //#################################################################
        //
        // Settings
        //
        //#################################################################

        public void InitializeStandardSettings()
        {
            InitializeSetting(MaxInventoryRowsKey, "100");
            InitializeSetting(AnnouncementKey, "");
            InitializeSetting(TempDirKey, System.IO.Path.DirectorySeparatorChar + "tmp");
            InitializeSetting(SearchLevelKey, "1");
            InitializeSetting(CreateImportLocationsKey, "no");
        }


        public void InitializeSetting(string key, string value)
        {
            if (GetSetting(key) == null) StoreSetting(key, value);

        }

        ///----------------------------------------------------------------
        ///
        /// Function:       SystemSettings
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get system settings (except for Site/LocationSchema)
        /// </summary>
        ///
        /// <returns>array of Setting</returns>
        ///
        ///----------------------------------------------------------------
        public Setting[] SystemSettings()
        {
            Setting[] result = Settings.Where(x => x.SettingKey != LocationSchemaKey && !x.SettingKey.StartsWith("user.")).OrderBy(x => x.SettingKey).ToArray();
            return result;
        }

        public Setting GetSetting(string key)
        {
            Setting result = Settings.FirstOrDefaultAsync(x => x.SettingKey == key).Result;
            return result;
        }

        public Setting StoreSetting(string key, string value)
        {
            try
            {
                Setting setting = GetSetting(key);
                bool is_new = false;
                if (setting == null)
                {
                    setting = new Setting { SettingKey = key, SettingValue = value };
                    Settings.Add(setting);
                    is_new = true;
                }
                else
                {
                    setting.SettingValue = value;
                }
                SaveChanges();
                if (is_new) LogInfo("unknown", "settings", $"Adding setting {setting.SettingID} with key \"{key}\"");
                else LogInfo("unknown", "settings", $"Updated setting {setting.SettingID} with key \"{key}\"");
                return setting;
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                this.LogError("unknown", "settings", ex.Message);
            }
            return null;
        }

        public string GetStringSetting(string key, string default_value)
        {
            Setting setting = GetSetting(key);
            return (setting == null ? default_value : setting.SettingValue);
        }

        public int GetIntSetting(string key, int default_value)
        {
            int result = default_value;
            Setting setting = GetSetting(key);
            if (setting != null) Int32.TryParse(setting.SettingValue, out result);
            return result;
        }

        public Setting GetUserSetting(string login_name, string key)
        {
            string fullkey = $"user.{login_name}.{key}";
            return GetSetting(fullkey);
        }


        public Setting StoreUserSetting(string login_name, string key, string value)
        {
            string fullkey = $"user.{login_name}.{key}";
            return StoreSetting(fullkey, value);
        }



        //#################################################################
        //
        // Attachments
        //
        //#################################################################

        public int GetAttachmentID(string login, string attachment_name)
        {
            int[] attachment_ids = Attachments.Where(a => a.Login == login && a.Name == "photo").Select(p => p.AttachmentID).ToArray();
            if (attachment_ids.Length > 0) return attachment_ids[0];
            else return 0;
        }

        public void SaveAttachment(string current_user, string attachment_user, string attachment_name, string description, byte[] data)
        {
            Attachment existing = Attachments.FirstOrDefault(x => x.Login == attachment_user && x.Name == attachment_name);
            if (existing == null)
            {
                Attachment attachment = new Attachment
                {
                    Login = attachment_user,
                    Name = attachment_name,
                    Description = description,
                    Data = data
                };
                Attachments.Add(attachment);
                LogInfo(current_user, "attachment", $"New attachment {attachment_name} created for user {attachment_user}", true);
            }
            else
            {
                existing.Description = description;
                existing.Data = data;
                LogInfo(current_user, "attachment", $"Attachment {attachment_name} updated for user {attachment_user}", true);
            }
        }


        //#################################################################
        //
        // LOG ENTRIES
        //
        //#################################################################

        public void AddLogEntry(string login, string category, string text, int message_level = 0, bool save_changes = true)
        {
            LogEntry entry = new LogEntry()
            {
                Login = login,
                Category = category,
                Text = text,
                MessageLevel = message_level
            };
            LogEntries.Add(entry);
            if (save_changes) SaveChanges();
        }

        public void LogError(string login, string category, string text, bool save_changes = true)
        {
            AddLogEntry(login, category, text, 2, save_changes);
        }


        public void LogWarning(string login, string category, string text, bool save_changes = true)
        {
            AddLogEntry(login, category, text, 1, save_changes);
        }


        public void LogInfo(string login, string category, string text, bool save_changes = true)
        {
            AddLogEntry(login, category, text, 0, save_changes);
        }

        public static void LogErrorMessage(string login, string category, string text)
        {
            using (CMSDB db = new CMSDB())
            {
                db.LogError(login, category, text);
            }
        }

        public static void LogWarningMessage(string login, string category, string text)
        {
            using (CMSDB db = new CMSDB())
            {
                db.LogWarning(login, category, text);
            }
        }

        public static void LogInfoMessage(string login, string category, string text)
        {
            using (CMSDB db = new CMSDB())
            {
                db.LogInfo(login, category, text);
            }
        }


        //#################################################################
        //
        // REPORT QUERIES
        //
        //#################################################################

        public void GetStockCheckData(QueryResult result)
        {
            string sql = @"(select Item.LocationID as Location
                                 , Item.Barcode as Barcode
                                 , ISN.Name as Status
	                             , Item.StockCheckPreviousLocation as 'StockCheckLocation'
	                             , Item.CASNumber as 'CASNumber'
	                             , Item.ChemicalName as 'Chemical'
                           from InventoryItems AS Item
                           left outer join InventoryStatusNames ISN on Item.InventoryStatusID = ISN.InventoryStatusID
                           left outer join StorageLocations Loc1 on Loc1.LocationID = Item.LocationID
                           left outer join StorageLocations Loc2 on Loc2.LocationID = Item.StockCheckPreviousLocation)

                           UNION

                           (select ExpectedLocation as Location, Barcode, 'No Record' as Status, ExpectedLocation as 'StockCheckLocation', CASNumber, ChemicalName as 'Chemical'
                            from Orphans)

                           order by Location";

            LocationManager locations = this.Locations;
            DataTable datatable = DbContextHelper.ExecuteUnboundQuery(this, sql, (reader) =>
            {
                int location_id = Int32.Parse(reader["Location"].ToString());
                string location = locations.GetLocationName(location_id);
                string barcode = reader["Barcode"].ToString();
                string status = reader["Status"].ToString();
                string previous_location = "";
                if (reader["StockCheckLocation"] != DBNull.Value)
                {
                    int id = Int32.Parse(reader["StockCheckLocation"].ToString());
                    previous_location = locations.GetLocationName(id);
                }
                string cas_number = reader["CasNumber"].ToString();
                string chemical_name = reader["Chemical"].ToString();

                result.AddRow(
                    new ColumnData(location),
                    new ColumnData(barcode),
                    new ColumnData(status),
                    new ColumnData(previous_location),
                    new ColumnData(cas_number),
                    new ColumnData(chemical_name)
                );
                return true;
            });

        }


        //#################################################################
        //
        // UTILITY FUNCTIONS
        //
        //#################################################################


        ///----------------------------------------------------------------
        ///
        /// Function:       IsBarcodeValie
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Check for a valid barcode
        /// </summary>
        ///
        /// <param name="barcode">the barcode string to check</param>
        /// <returns>a tuple, (bool rc, string errmsg)</returns>
        ///
        ///----------------------------------------------------------------
        public static (bool, string) IsBarcodeValid(string barcode)
        {
            bool rc = true;
            string error_message = "";
            if (barcode.Contains("'"))
            {
                error_message = "Quote marks are not valid in bar codes.";
                rc = false;
            }
            return (rc, error_message);
        }

        public static string StandardDate(DateTime? dateval = null)
        {
            string fmt = "{0:0000}-{1:00}-{2:00}";
            DateTime val = dateval ?? DateTime.Now;
            return String.Format(fmt, val.Year, val.Month, val.Day);
        }

        public static string StandardDateTime(DateTime? dateval = null)
        {
            string fmt = "{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}";
            DateTime val = dateval ?? DateTime.Now;
            return String.Format(fmt, val.Year, val.Month, val.Day, val.Hour, val.Minute, val.Second);
        }



        //#################################################################
        //
        // STATIC DATA
        //
        //#################################################################



        private static string s_getlocationpath_func = @"
            create function GetLocationPath
            (
                location_id INTEGER
            )
            returns varchar(512)
            READS SQL DATA
            DETERMINISTIC
            BEGIN
                declare fullname varchar(512);
                declare locname varchar(64);
                declare tmpname varchar(64);
                declare parent_id integer;
                declare loclevel integer;
    
                set fullname = '';
                select Name, LocationLevel, ParentID into fullname, loclevel, parent_id from StorageLocations where LocationID = location_id;
                while loclevel > 1 do
    	              select Name, LocationLevel, ParentID into tmpname, loclevel, parent_id from StorageLocations where LocationID = parent_id;
		              set fullname = CONCAT(tmpname, '/', fullname);
                end while;
	            return fullname;
            END
        ";

        private static string[] s_un_country_codes =
        {
            "AF|Afghanistan",
            "AX|Åland Islands",
            "AL|Albania",
            "DZ|Algeria",
            "AS|American Samoa",
            "AD|Andorra",
            "AO|Angola",
            "AI|Anguilla",
            "AQ|Antarctica",
            "AG|Antigua and Barbuda",
            "AR|Argentina",
            "AM|Armenia",
            "AW|Aruba",
            "AU|Australia",
            "AT|Austria",
            "AZ|Azerbaijan",
            "BS|Bahamas",
            "BH|Bahrain",
            "BD|Bangladesh",
            "BB|Barbados",
            "BY|Belarus",
            "BE|Belgium",
            "BZ|Belize",
            "BJ|Benin",
            "BM|Bermuda",
            "BT|Bhutan",
            "BO|Bolivia",
            "BQ|Bonaire, Sint Eustatius and Saba",
            "BA|Bosnia and Herzegovina",
            "BW|Botswana",
            "BR|Brazil",
            "IO|British Indian Ocean Territory",
            "BN|Brunei Darussalam",
            "BG|Bulgaria",
            "BF|Burkina Faso",
            "BI|Burundi",
            "KH|Cambodia",
            "CM|Cameroon",
            "CA|Canada",
            "CV|Cape Verde",
            "KY|Cayman Islands",
            "CF|Central African Republic",
            "TD|Chad",
            "CL|Chile",
            "CN|China",
            "CX|Christmas Island",
            "CC|Cocos (Keeling) Islands",
            "CO|Colombia",
            "KM|Comoros",
            "CG|Congo",
            "CD|Congo, The Democratic Republic of the",
            "CK|Cook Islands",
            "CR|Costa Rica",
            "CI|Côte d'Ivoire",
            "HR|Croatia",
            "CU|Cuba",
            "CW|Curaçao",
            "CY|Cyprus",
            "CZ|Czech Republic",
            "DK|Denmark",
            "DJ|Djibouti",
            "DM|Dominica",
            "DO|Dominican Republic",
            "EC|Ecuador",
            "EG|Egypt",
            "SV|El Salvador",
            "GQ|Equatorial Guinea",
            "ER|Eritrea",
            "EE|Estonia",
            "ET|Ethiopia",
            "FK|Falkland Islands (Malvinas)",
            "FO|Faroe Islands",
            "FJ|Fiji",
            "FI|Finland",
            "FR|France",
            "GF|French Guiana",
            "PF|French Polynesia",
            "TF|French Southern Territories",
            "GA|Gabon",
            "GM|Gambia",
            "GE|Georgia",
            "DE|Germany",
            "GH|Ghana",
            "GI|Gibraltar",
            "GR|Greece",
            "GL|Greenland",
            "GD|Grenada",
            "GP|Guadeloupe",
            "GU|Guam",
            "GT|Guatemala",
            "GG|Guernsey",
            "GN|Guinea",
            "GW|Guinea-Bissau",
            "GY|Guyana",
            "HT|Haiti",
            "HM|Heard Island and McDonald Islands",
            "VA|Holy See (Vatican City State)",
            "HN|Honduras",
            "HK|Hong Kong",
            "HU|Hungary",
            "IS|Iceland",
            "IN|India",
            "ID|Indonesia",
            "XZ|Installations in International Waters",
            "IR|Iran, Islamic Republic of",
            "IQ|Iraq",
            "IE|Ireland",
            "IM|Isle of Man",
            "IL|Israel",
            "IT|Italy",
            "JM|Jamaica",
            "JP|Japan",
            "JE|Jersey",
            "JO|Jordan",
            "KZ|Kazakhstan",
            "KE|Kenya",
            "KI|Kiribati",
            "KP|Korea, Democratic People's Republic of",
            "KR|Korea, Republic of",
            "KW|Kuwait",
            "KG|Kyrgyzstan",
            "LA|Lao People's Democratic Republic",
            "LV|Latvia",
            "LB|Lebanon",
            "LS|Lesotho",
            "LR|Liberia",
            "LY|Libya",
            "LI|Liechtenstein",
            "LT|Lithuania",
            "LU|Luxembourg",
            "MO|Macao",
            "MK|Macedonia, The former Yugoslav Republic of",
            "MG|Madagascar",
            "MW|Malawi",
            "MY|Malaysia",
            "MV|Maldives",
            "ML|Mali",
            "MT|Malta",
            "MH|Marshall Islands",
            "MQ|Martinique",
            "MR|Mauritania",
            "MU|Mauritius",
            "YT|Mayotte",
            "MX|Mexico",
            "FM|Micronesia, Federated States of",
            "MD|Moldova, Republic of",
            "MC|Monaco",
            "MN|Mongolia",
            "ME|Montenegro",
            "MS|Montserrat",
            "MA|Morocco",
            "MZ|Mozambique",
            "MM|Myanmar",
            "NA|Namibia",
            "NR|Nauru",
            "NP|Nepal",
            "NL|Netherlands",
            "NC|New Caledonia",
            "NZ|New Zealand",
            "NI|Nicaragua",
            "NE|Niger",
            "NG|Nigeria",
            "NU|Niue",
            "NF|Norfolk Island",
            "MP|Northern Mariana Islands",
            "NO|Norway",
            "OM|Oman",
            "PK|Pakistan",
            "PW|Palau",
            "PS|Palestine, State of",
            "PA|Panama",
            "PG|Papua New Guinea",
            "PY|Paraguay",
            "PE|Peru",
            "PH|Philippines",
            "PN|Pitcairn",
            "PL|Poland",
            "PT|Portugal",
            "PR|Puerto Rico",
            "QA|Qatar",
            "RE|Reunion",
            "RO|Romania",
            "RU|Russian Federation",
            "RW|Rwanda",
            "BL|Saint Barthélemy",
            "SH|Saint Helena, Ascension and Tristan Da Cunha",
            "KN|Saint Kitts and Nevis",
            "LC|Saint Lucia",
            "MF|Saint Martin (French Part)",
            "PM|Saint Pierre and Miquelon",
            "VC|Saint Vincent and the Grenadines",
            "WS|Samoa",
            "SM|San Marino",
            "ST|Sao Tome and Principe",
            "SA|Saudi Arabia",
            "SN|Senegal",
            "RS|Serbia",
            "SC|Seychelles",
            "SL|Sierra Leone",
            "SG|Singapore",
            "SX|Sint Maarten (Dutch Part)",
            "SK|Slovakia",
            "SI|Slovenia",
            "SB|Solomon Islands",
            "SO|Somalia",
            "ZA|South Africa",
            "GS|South Georgia and the South Sandwich Islands",
            "SS|South Sudan",
            "ES|Spain",
            "LK|Sri Lanka",
            "SD|Sudan",
            "SR|Suriname",
            "SJ|Svalbard and Jan Mayen",
            "SZ|Swaziland",
            "SE|Sweden",
            "CH|Switzerland",
            "SY|Syrian Arab Republic",
            "TW|Taiwan, Province of China",
            "TJ|Tajikistan",
            "TZ|Tanzania, United Republic of",
            "TH|Thailand",
            "TL|Timor-Leste",
            "TG|Togo",
            "TK|Tokelau",
            "TO|Tonga",
            "TT|Trinidad and Tobago",
            "TN|Tunisia",
            "TR|Turkey",
            "TM|Turkmenistan",
            "TC|Turks and Caicos Islands",
            "TV|Tuvalu",
            "UG|Uganda",
            "UA|Ukraine",
            "AE|United Arab Emirates",
            "GB|United Kingdom",
            "US|United States",
            "UM|United States Minor Outlying Islands",
            "UY|Uruguay",
            "UZ|Uzbekistan",
            "VU|Vanuatu",
            "VE|Venezuela",
            "VN|Viet Nam",
            "VG|Virgin Islands, British",
            "VI|Virgin Islands, U.S.",
            "WF|Wallis and Futuna",
            "EH|Western Sahara",
            "YE|Yemen",
            "ZM|Zambia",
            "ZW|Zimbabwe"
        };

    }

    public class LocationData
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public int LocationID { get; set; }
        public int Level { get; set; }
        public string Site { get; set; }

        public LocationData() { }
        public LocationData(StorageLocation loc)
        {
            Name = loc.Name;
            LocationID = loc.LocationID;
            ShortName = loc.ShortLocation;
            LongName = loc.FullLocation;
            Level = loc.LocationLevel;
            Site = CMSDB.GetSite(LongName);
        }

    }

    public class LocationSubtree
    {
        public Dictionary<int, StorageLocation> LocationDictionary { get; private set; }
        public StorageLocation Root { get; set; }
        public List<StorageLocation> Locations
        {
            get
            {
                List<StorageLocation> result = new List<StorageLocation>();
                CollectLocations(Root, result);
                return result;
            }
        }

        public LocationSubtree(StorageLocation root)
        {
            Root = root;
            LocationDictionary = new Dictionary<int, StorageLocation>();
            LocationDictionary[root.LocationID] = root;
        }

        public LocationSubtree Add(StorageLocation loc)
        {
            LocationDictionary[loc.LocationID] = loc;
            return this;
        }


        public List<StorageLocation> GetChildren(StorageLocation loc)
        {
            int parent_id = loc.LocationID;
            return LocationDictionary.Values.Where(x => x.ParentID == parent_id).ToList();
        }

        private void CollectLocations(StorageLocation loc, List<StorageLocation> locations)
        {
            locations.Add(loc);
            foreach (StorageLocation child in GetChildren(loc))
            {
                CollectLocations(child, locations);
            }
        }


        private void CollectViewableLocations(StorageLocation loc, List<StorageLocation> locations)
        {
            foreach (StorageLocation child in GetChildren(loc))
            {
                CollectViewableLocations(child, locations);
            }
        }



    }

    //public class UserLocationAccessMap
    //{
    //    /// <summary>
    //    /// Map of LocationID to sticky access
    //    /// </summary>
    //    private Dictionary<int, AccessControl> m_sticky_access;
    //    /// <summary>
    //    /// Map of LocationID to inherited access
    //    /// </summary>
    //    private Dictionary<int, AccessControl> m_inherited_access;

    //    public UserLocationAccessMap()
    //    {
    //        m_sticky_access = new Dictionary<int, AccessControl>();
    //        m_inherited_access = new Dictionary<int, AccessControl>();
    //    }

    //    public void Register(int location_id, AccessControl access, bool is_sticky)
    //    {
    //        if (is_sticky) m_sticky_access[location_id] = access;
    //    }
    //}



    public class LocationManager : IEnumerable<StorageLocation>
    {
        private Dictionary<int, StorageLocation> m_id_to_location_map;
        private Dictionary<int, List<StorageLocation>> m_id_to_children_map;
        private List<StorageLocation> m_storage_locations;

        public int Count { get { return m_storage_locations.Count; } }
        public StorageLocation Root {  get { return m_id_to_location_map[1];  } }
        public List<StorageLocation> AllLocations { get { return m_storage_locations; } }

        // indexer
        public StorageLocation this[int location_id]
        {
            get { return Find(location_id); }
        }

        public LocationManager(CMSDB db)
        {
            m_id_to_location_map = new Dictionary<int, StorageLocation>();
            m_id_to_children_map = new Dictionary<int, List<StorageLocation>>();
            m_storage_locations = new List<StorageLocation>();
            Initialize(db);
        }

        public List<StorageLocation> Where(System.Func<StorageLocation, bool> filter)
        {
            return m_storage_locations.Where(filter).ToList();
        }

        public StorageLocation Find(int location_id, bool initialize_names = true)
        {
            StorageLocation result = null;
            if (m_id_to_location_map.TryGetValue(location_id, out result))
            {
                InitializeLocationNames(result);
                return result;
            }
            else return null;
        }

        public StorageLocation Find(string name, int parent_id)
        {
            var result = m_storage_locations.FirstOrDefault(x => x.Name == name && x.ParentID == parent_id);
            if (result != null) InitializeLocationNames(result);
            return result;
        }

        public StorageLocation FindPath(string path)
        {
            if (path.EndsWith('/')) path = path.Substring(0, path.Length - 1);
            StorageLocation result = m_storage_locations.FirstOrDefault(x => x.Path == path);
            return result;
        }

        public List<StorageLocation> FindByLevel(int level)
        {
            return (m_storage_locations.Where(x => x.LocationLevel == level).ToList());
        }

        public StorageLocation FindSite(string name)
        {
            return (m_storage_locations.FirstOrDefault(x => x.LocationLevel == 1 && x.Name == name));
        }

        private void Clear()
        {
            m_id_to_location_map.Clear();
            m_id_to_children_map.Clear();
            m_storage_locations.Clear();
        }

        public void Initialize(CMSDB db)
        {
            // initialize the Path column of every row in StorageLocations
            Clear();
            db.ReadLocations(Add);
        }

        public void Refresh(CMSDB db)
        {
            // initialize the Path column of every row in StorageLocations
            db.ReadLocations(loc =>
            {
                if (loc.Path == null)
                {
                    Console.WriteLine($"Location {loc.LocationID} has no path");
                }
                else
                {
                    StorageLocation myloc = m_id_to_location_map[loc.LocationID];
                    myloc.Path = loc.Path;
                }
            });
        }

        public void Add(StorageLocation loc)
        {
            m_id_to_location_map[loc.LocationID] = loc;
            m_storage_locations.Add(loc);
            if (loc.ParentID > 0)
            {
                List<StorageLocation> children = null;
                if (!m_id_to_children_map.TryGetValue(loc.ParentID, out children))
                {
                    children = new List<StorageLocation>();
                    m_id_to_children_map.Add(loc.ParentID, children);
                }
                children.Add(loc);
            }

        }

        public StorageLocation FindLocation(int location_id)
        {
            return m_id_to_location_map[location_id];
        }

        public List<StorageLocation> FindChildren(int location_id)
        {
            if (m_id_to_children_map.ContainsKey(location_id)) return m_id_to_children_map[location_id];
            else return new List<StorageLocation>();
        }


        public List<StorageLocation> GetStorageLocations(bool endpoints_only = true, int maxlevel = 99)
        {
            List<StorageLocation> result = new List<StorageLocation>();
            List<StorageLocation> locations;
            int leaf_level = (int)ELocationLevel.SHELF;
            if (endpoints_only)
            {
                locations = m_storage_locations.Where(x => x.LocationLevel == leaf_level).OrderBy(x => x.Name).ToList();
            }
            else
            {
                locations = m_storage_locations.Where(x => x.LocationLevel <= maxlevel).OrderBy(x => x.Name).ToList();
            }
            foreach (var loc in locations)
            {
                InitializeLocationNames(loc);
                result.Add(loc);
            }
            if (endpoints_only) return result.OrderBy(x => x.ShortLocation).ToList();
            else return result.OrderBy(x => x.FullLocation).ToList();
        }

        private void PopulateSubtree(StorageLocation loc, LocationSubtree result, int depth = 99)
        {
            result.Add(loc);
            InitializeLocationNames(loc);
            if (depth > 0)
            {
                //foreach (var child in m_storage_locations.Where(x => x.ParentID == loc.LocationID))
                foreach (var child in FindChildren(loc.LocationID))
                {
                    PopulateSubtree(child, result, depth - 1);
                }
            }
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetSubtree
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the location subtree rooted at the given location
        /// </summary>
        ///
        /// <param name="root">the root of the desired subtree</param>
        /// <param name="depth">how deep to traverse</param>
        /// <returns>a LocationSubtree instance</returns>
        ///
        ///----------------------------------------------------------------
        public LocationSubtree GetSubtree(StorageLocation root, int depth = 99)
        {
            LocationSubtree result = new LocationSubtree(root);
            InitializeLocationNames(root);
            if (depth > 0)
            {
                //foreach (var child in m_storage_locations.Where(x => x.ParentID == root.LocationID))
                foreach (var child in FindChildren(root.LocationID))
                {
                    int count = result.LocationDictionary.Count;
                    PopulateSubtree(child, result, depth - 1);
                }
            }

            return result;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetSubtree
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the location subtree rooted at the given location
        /// </summary>
        ///
        /// <param name="location_id">the location_id of the root of the desired subtree</param>
        /// <param name="depth">how deep to traverse</param>
        /// <returns>a LocationSubtree instance</returns>
        ///
        ///----------------------------------------------------------------
        public LocationSubtree GetSubtree(int location_id, int depth = 99)
        {
            StorageLocation loc = Find(location_id);
            if (loc != null) return GetSubtree(loc, depth);
            else return null;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Subsumes
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Determine if one location is the same as, or an ancestor of another
        /// </summary>
        ///
        /// <param name="ancestor">the ancestor</param>
        /// <param name="descendant">the descendant</param>
        /// <returns>true/false</returns>
        ///
        ///----------------------------------------------------------------
        public bool Subsumes(StorageLocation ancestor, StorageLocation descendant)
        {
            // if the descendant is further up the tree it obviously isn't subsumed
            if (descendant.LocationLevel < ancestor.LocationLevel) return false;

            // any location subsumes itself
            bool subsumes = (ancestor.LocationID == descendant.LocationID);
            if (!subsumes)
            {
                // try looking up from the descendant
                subsumes = (descendant.ParentID > 0 && Subsumes(ancestor, m_id_to_location_map[descendant.ParentID]));
            }
            return subsumes;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       Subsumes
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Determine if one location is the same as, or an ancestor of another
        /// </summary>
        ///
        /// <param name="ancestor">the ancestor LocationID</param>
        /// <param name="descendant">the descendant LocationID</param>
        /// <returns>true/false</returns>
        ///
        ///----------------------------------------------------------------
        public bool Subsumes(int ancestor, int descendant)
        {
            return Subsumes(m_id_to_location_map[ancestor], m_id_to_location_map[descendant]);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetLocationName
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the full location name for a given LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID of interest</param>
        /// <param name="minlevel">the minumum level to lookup</param>
        /// <returns>a string of the form "xxx/yyy/..."</returns>
        /// 
        /// <remarks>
        /// If you are looking up a room and you just want the building and room,
        /// use minlevel = 2.
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        public string GetLocationName(int id)
        {
            StorageLocation loc = null;
            if (!m_id_to_location_map.TryGetValue(id, out loc)) throw new Exception($"CMSDB.GetLocationName - LocationID {id} does not exist");
            return loc.Path;
            //string name = loc.Name;
            //if (loc.LocationLevel > minlevel && loc.ParentID > 0)
            //{
            //    string parent_name = GetLocationName(loc.ParentID, minlevel);
            //    if (parent_name != null) name = parent_name + "/" + name;
            //}
            //return name;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       GetShortLocationName
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the short location name for a given LocationID
        /// </summary>
        ///
        /// <param name="id">the LocationID of interest - must be level 3</param>
        /// <returns>a string of the form "xxx/yyy/..."</returns>
        ///
        ///----------------------------------------------------------------

        public string GetShortLocationName(int id)
        {
            return GetLocationName(id);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetFullLocationName
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Return the full location path for a location element, including
        /// the top-level component
        /// </summary>
        ///
        /// <param name="location_id">the LocationID of the location of interest</param>
        /// <param name="include_root">true to return a path that starts with root</param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public string GetFullLocationName(int location_id, bool include_root = false)
        {
            string result = "/" + GetLocationName(location_id);
            if (include_root)
            {
                string root_string = "/" + Root.Name;
                if (!result.StartsWith(root_string)) result = Combine(root_string, result);
            }
            return result;
        }



        public void InitializeLocationNames(StorageLocation loc)
        {
            if (String.IsNullOrEmpty(loc.FullLocation))
            {
                loc.FullLocation = loc.Path;
                loc.ShortLocation = loc.Path;
            }
        }

        public static string SitePart(string location_name, int ix)
        {
            string[] parts = CMSDB.ParseLocation(location_name);
            if (ix < parts.Length) return parts[ix];
            else return "";
        }

        public static string GetSite(string location_name)
        {
            return CMSDB.SitePart(location_name, 1);
        }

        public string GetSite(StorageLocation loc)
        {
            InitializeLocationNames(loc);
            return CMSDB.SitePart(loc.FullLocation, 1);
        }

        public List<StorageLocation> GetLocations(ELocationLevel level = ELocationLevel.SHELF)
        {
            int levelnum = (int)level;
            return m_storage_locations.Where(x => x.LocationLevel == levelnum).ToList();
        }

        public IEnumerator<StorageLocation> GetEnumerator()
        {
            return ((IEnumerable<StorageLocation>)m_storage_locations).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<StorageLocation>)m_storage_locations).GetEnumerator();
        }

        public static string Combine(string prefix, string suffix)
        {
            if (!suffix.StartsWith("/")) suffix = "/" + suffix;
            if (prefix.EndsWith("/")) prefix = prefix.Substring(0, prefix.Length - 1);
            string result = prefix + suffix;
            return result;
        }
    }



    public class InventorySearchSettings
    {
        public int RootID { get; set; }
        public bool IncludeAllLocations { get; set; }
        public string BarCode { get; set; }
        public string CASNumber { get; set; }
        public string Owner { get; set; }
        public string Chemical { get; set; }
        public int ItemsMatched { get; set; }
        public int ResultOffset { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public bool IsInitialQuery { get; set; } = true;
    }


    public class UserSubtreeLocations
    {
        public string Login { get; set; }
        public int RootLocationID { get; set; }
        public string DictionaryKey { get; set; }
        public int[] ValidLocations { get; set; }

        public UserSubtreeLocations(string login, int root_location_id)
        {
            Login = login;
            RootLocationID = root_location_id;
            DictionaryKey = $"{Login}:{RootLocationID}";
        }
    }

    public class UserSubtreeCache
    {
        public int CacheSize { get; set; } = 4;
        private Dictionary<string, LocationSubtree> m_subtree_dictionary = new Dictionary<string, LocationSubtree>();
        private Queue<LocationSubtree> m_cached_subtrees = new Queue<LocationSubtree>();


        public LocationSubtree GetCachedSubtree(string login, int root_location_id)
        {
            string key = MakeKey(login, root_location_id);
            UserSubtreeLocations subtree = new UserSubtreeLocations(login, root_location_id);
            if (m_subtree_dictionary.ContainsKey(key)) return m_subtree_dictionary[key];
            else return null;
        }

        public void AddToCache(string login, int root_location_id, LocationSubtree subtree)
        {
            string key = MakeKey(login, root_location_id);
            if (!m_subtree_dictionary.ContainsKey(key))
            {
                if (m_cached_subtrees.Count >= CacheSize)
                {
                    // pop this from the queue
                    var doomed = m_cached_subtrees.Dequeue();
                    // remove it from the dictionary
                    m_subtree_dictionary.Remove(key);
                }
            }
            m_cached_subtrees.Enqueue(subtree);
            m_subtree_dictionary.Add(key, subtree);
        }

        public void Clear()
        {
            m_subtree_dictionary.Clear();
            m_cached_subtrees.Clear();
        }

        private string MakeKey(string login, int root_id)
        {
            return $"{login}:{root_id}";
        }
    }


    public class ConnectionSettings
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
