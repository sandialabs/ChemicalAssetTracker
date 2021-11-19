using DataModel;
using System;
using System.Linq;
using System.Collections.Generic;
using Common;

namespace ImportLib
{

    ///----------------------------------------------------------------
    ///
    /// Class:          ImportValidator
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Class to confirm that a legacy database or CSV file can be imported.
    /// </summary>
    ///
    ///----------------------------------------------------------------
    public class ImportValidator
    {
        public DatabaseValidationResult ValidationResult { get; private set; }
        public Dictionary<int, LocationType> LocationTypeIDMap { get; private set; }
        public Dictionary<string, LocationType> LocationTypeNameMap { get; private set; }
        public DataModel.StorageLocation AttachmentLocation { get; private set; }
        public ExcelImportFile ImportFile { get; private set; }
        protected CMSDB m_db;
        protected bool m_allow_missing_locations = false;


        ///----------------------------------------------------------------
        ///
        /// Function:       Constructor
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Constructor for ImportValidator
        /// </summary>
        ///
        /// <param name="cat_db">open ChemicalAssetTracker database context</param>
        /// <param name="root_location_id">where the user wants to import into</param>
        ///
        ///----------------------------------------------------------------
        public ImportValidator(CMSDB cat_db, int root_location_id)
        {
            ValidationResult = new DatabaseValidationResult();
            m_db = cat_db;

            m_allow_missing_locations = ("yes" == m_db.GetStringSetting(CMSDB.CreateImportLocationsKey, "no"));

            // create dictionaries of LocationTypes
            LocationTypeIDMap = new Dictionary<int, LocationType>();
            LocationTypeNameMap = new Dictionary<string, LocationType>();
            foreach (LocationType lt in m_db.LocationTypes)
            {
                LocationTypeIDMap.Add(lt.LocationTypeID, lt);
                LocationTypeNameMap.Add(lt.Name, lt);
            }

            AttachmentLocation = m_db.FindLocation(root_location_id);
            if (AttachmentLocation == null)
            {
                throw new Exception($"Root location id {root_location_id} does not exist.");
            }
            ValidationResult.TargetLocation = AttachmentLocation.Path;
            ValidationResult.TargetLocationID = root_location_id;
            string root_type = (AttachmentLocation.LocationType == null ? "unknown" : AttachmentLocation.LocationType.Name);
            ValidationResult.AddMessage($"Importing into location {AttachmentLocation.Path} of type {root_type}");
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       Validate
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Confirm that a legacy SQLite database or Excel file can be imported at the requested location
        /// </summary>
        ///
        /// <param name="filename">path to legacy file</param>
        /// <returns>true iff validation succeeded</returns>
        ///
        ///----------------------------------------------------------------
        public bool Validate(string filename)
        {
            string lowfile = filename.ToLower();
            if (lowfile.EndsWith(".db") || lowfile.EndsWith(".db"))
            {
                return ValidateSQLite(filename);
            }
            if (lowfile.EndsWith(".xlsx"))
            {
                return ValidateExcel(filename);
            }
            return false;
        }
        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateSQLite
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Confirm that a legacy SQLite database can be imported at the requested location
        /// </summary>
        ///
        /// <param name="sqlite_file">path to legacy SQLite file</param>
        /// <returns>true iff validation succeeded</returns>
        ///
        ///----------------------------------------------------------------
        public bool ValidateSQLite(string sqlite_file)
        {
            TaskTimer t1 = new TaskTimer("timer");
            List<string> new_locations = new List<string>();
            try
            {
                using (LegacyDatabase legacy_db = new LegacyDatabase(sqlite_file))
                {
                    if (!legacy_db.IsOpen)
                    {
                        throw new Exception($"Unable to connect to SQLite database: {legacy_db.LastError}");
                    }
                    using (CMSDB db = new CMSDB())
                    {
                        ValidateLegacyDatabase(legacy_db);
                    }
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                ValidationResult.AddMessage(ex.Message);
                ValidationResult.Success = false;
            }
            return ValidationResult.Success;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateExcel
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Confirm that a CSV can be imported at the requested location
        /// </summary>
        ///
        /// <param name="filename">path to legacy SQLite file</param>
        /// <returns>true iff validation succeeded</returns>
        ///
        ///----------------------------------------------------------------
        public bool ValidateExcel(string filename)
        {
            //XLWorkbook excelfile = new XLWorkbook(filename);
            //IXLWorksheet worksheet = excelfile.Worksheets.First();
            ImportFile = new ExcelImportFile(filename);
            ExcelRow row1 = ImportFile.NthRow(1);
            Dictionary<string, int> column_indexes = ImportFile.Headers;

            //foreach (var col in row1.CellsUsed())
            //{
            //    string value = col.StringValue(true);
            //    if (!String.IsNullOrEmpty(value))
            //    {
            //        (_, int colnum) = col.Address();
            //        column_indexes[value] = colnum;
            //    }
            //}
            if (column_indexes.ContainsKey("Barcode")
                && column_indexes.ContainsKey("Location")
                && column_indexes.ContainsKey("Chemical Name")
                && column_indexes.ContainsKey("CAS #"))
            {
                IEnumerable<ExcelRow> rows = ImportFile.RowsUsed().Skip(1);
                foreach (string name in ImportFile.Owners)
                {
                    var existing = m_db.Owners.FirstOrDefault(x => x.Name == name);
                    if (existing == null)
                    {
                        ValidationResult.AddMessage($"Owner {name} will be added to the database");
                    }
                }

                foreach (string name in ImportFile.StorageGroups)
                {
                    var existing = m_db.StorageGroups.FirstOrDefault(x => x.Name == name);
                    if (existing == null)
                    {
                        ValidationResult.AddMessage($"Storage Group {name} will be added to the database");
                    }
                }

                int missing_location_count = ImportFile.MissingLocationCount();
                foreach (string locstr in ImportFile.Locations)
                {
                    ValidateLocationString(locstr);
                }
                if (missing_location_count > 0)
                {
                    string recs = (missing_location_count == 1 ? "record has" : "records have");
                    ValidationResult.AddMessage($"{missing_location_count} import {recs} no location and will be assigned a default.");
                }
            }
            return ValidationResult.Success;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateLegacyDatabase
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Confirm that a legacy SQLite database can be imported at the requested location
        /// </summary>
        ///
        /// <param name="legacy_db">a legacy SQLite database</param>
        ///
        /// <remarks>
        /// There is no return value from this method.  Look at the public
        /// ValidationResult propery to get the results of validation.
        /// E.g. ValidationResult.Success and ValidationResult.Messages
        /// </remarks>
        ///----------------------------------------------------------------
        protected void ValidateLegacyDatabase(LegacyDatabase legacy_db)
        {
            // make sure every inventory item has a location
            var dbresult = legacy_db.Query("select * from InventoryItems where LocationID is null or LocationID = 0", null);
            int missing_location_count = dbresult.Rows.Count();
            bool need_nolocation_location = (missing_location_count > 0);
            if (need_nolocation_location)
            {
                string recs = (missing_location_count == 1 ? "record has" : "records have");
                ValidationResult.AddMessage($"{missing_location_count} import {recs} no location and will be assigned a default.");
            }

            legacy_db.Refresh();

            // Make sure the levels of the old location are consistant with the new location hierarchy
            foreach (var loc in legacy_db.FetchStorageLocations())
            {
                ValidateLocationString(loc.Name);
            }

            // check owners
            foreach (var o in legacy_db.FetchOwners())
            {
                string name = o.Name;
                var existing = m_db.Owners.FirstOrDefault(x => x.Name == name);
                if (existing == null)
                {
                    ValidationResult.AddMessage($"Owner {name} will be added to the database");
                }
            }

            // check groups
            foreach (var g in legacy_db.FetchStorageGroups())
            {
                var newgroup = m_db.StorageGroups.FirstOrDefault(x => x.Name == g.Name);
                if (newgroup == null)
                {
                    ValidationResult.AddMessage($"Storage Group {g.Name} will be added to the database");
                }
            }


        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateLocationString
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Confirm that a location string is consistant with import location
        /// </summary>
        ///
        /// <param name="location_name">a legacy location string</param>
        ///
        ///----------------------------------------------------------------
        protected void ValidateLocationString(string location_name)
        {
            // split the location name into its component parts
            string[] parts = CMSDB.ParseLocation(location_name);
            // partial_location will be built up starting at the root
            string partial_location = "/";
            // get the location that is the attachment point, and get its type
            DataModel.StorageLocation parent = AttachmentLocation;
            LocationType parent_loc_type = LocationTypeIDMap[parent.LocationTypeID];
            foreach (string part in parts)
            {
                // add the next location component to our partial_location string
                partial_location += (part + "/");
                // get that location
                string target_location = AttachmentLocation.Path + partial_location;
                // it should be a valid location (we require locations to be pre-initialized)
                DataModel.StorageLocation child_loc = m_db.Locations.FindPath(target_location);
                if (child_loc == null)
                {
                    // this is the old code that initializes location types on the fly
                    if (!m_allow_missing_locations)
                    {
                        ValidationResult.MissingLocations.Add(target_location);
                        if (ValidationResult.MissingLocations.Count == 1)
                        {
                            ValidationResult.AddMessage($"Error - the imported data refers to locations that are undefined.");
                        }
                        ValidationResult.Success = false;
                    }
                    // take a guess at the location type of this new location
                    if (parent_loc_type.ValidChildren.Length == 0)
                    {
                        string msg = $"Error: Locations of type {parent_loc_type.Name} cannot have child locations.";
                        ValidationResult.Success = false;
                        ValidationResult.AddMessage(msg);
                    }
                    else
                    {
                        string child_loc_type = parent_loc_type.ValidChildList[0];
                        if (parent_loc_type.ValidChildList.Count > 1)
                        {
                            (string match, double score) = DTW.Search(part, parent_loc_type.ValidChildList);
                            child_loc_type = match;
                            string options = String.Join(", ", parent_loc_type.ValidChildList);
                            string msg = $"Ambiguous child location type for {parent_loc_type.Name}: {options}. Using {child_loc_type}";
                            ValidationResult.AddMessage(msg);

                        }
                        string current_mapping = ValidationResult.GetLocationMap(partial_location);
                        if (current_mapping != null)
                        {
                            if (current_mapping != child_loc_type)
                            {
                                string msg = $"The imported location {partial_location} maps to {current_mapping} and {child_loc_type}";
                                ValidationResult.AddMessage(msg);
                            }
                        }
                        parent_loc_type = LocationTypeNameMap[child_loc_type];
                    }
                }
                else
                {
                    // this location is already in the location hierarchy
                    parent_loc_type = child_loc.LocationType;
                }
            }
        }
    }


public class DatabaseValidationResult
{
    public bool Success { get; set; }
    public int LocationsImported { get; set; }
    public int ItemsImported { get; set; }
    public string TargetLocation { get; set; }
    public int TargetLocationID { get; set; }
    public UniqueStringList Messages { get; set; }
    public UniqueStringList MissingLocations { get; set; }
    public Dictionary<string, string> LocationTypeMap { get; set; }
    public Dictionary<string, string> TargetLocationMap { get; set; }
    public string TempFilePath { get; set; }

    public DatabaseValidationResult()
    {
        Success = true;
        Messages = new UniqueStringList();
        LocationTypeMap = new Dictionary<string, string>();
        TargetLocationMap = new Dictionary<string, string>();
        MissingLocations = new UniqueStringList();
    }

    public void AddMessage(string msg)
    {
        Messages.Add(msg);
    }

    public string GetLocationMap(string imported_location)
    {
        if (LocationTypeMap.ContainsKey(imported_location))
        {
            return LocationTypeMap[imported_location];
        }
        else return null;
    }

    internal void MapLocation(string import_location, string location_type, string target_location)
    {
        if (!LocationTypeMap.ContainsKey(import_location))
        {
            LocationTypeMap[import_location] = location_type;
            TargetLocationMap[import_location] = target_location;
        }
    }
}

}
