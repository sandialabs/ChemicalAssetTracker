using ImportLib;
using DataModel;
using System;
using System.Linq;
using System.Collections.Generic;
using Common;

namespace ImportLib
{
    public class Importer
    {
        public delegate void ImportLogger(string message);

        private ImportLogger m_logger = null;
        private Dictionary<int, LocationType> m_location_type_id_map;
        private Dictionary<string, LocationType> m_location_type_name_map;
        private DataModel.StorageLocation m_root_location;
        private Dictionary<int, int> m_new_location_id_map;
        private Dictionary<string, int> m_new_location_string_map;  // full location string => LocationID
        bool m_need_nolocation_location;
        private string m_login;
        private int m_root_location_id;

        public Importer(ImportLogger logger)
        {
            m_logger = logger;
        }

        private void Log(string message)
        {
            if (m_logger != null) m_logger(message);
        }

        private static DateTime? ParseDateTime(string datetime)
        {
            DateTime result = DateTime.MinValue;
            if (DateTime.TryParse(datetime, out result)) return result;
            else return null;
        }

        private bool ValidateCSV()
        {
            return false;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ValidateImport
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Import a legacy CMS database
        /// </summary>
        ///
        /// <param name="sqlite_file">legacy SQLite database file path</param>
        /// <param name="root_location_id">the LocationID of the existing target location</param>
        /// <returns>the full path of the target location</returns>
        ///
        ///----------------------------------------------------------------
        public DatabaseValidationResult ValidateImport(string sqlite_file, int root_location_id)
        {
            DatabaseValidationResult result = new DatabaseValidationResult();

            if (!sqlite_file.ToLower().EndsWith(".db"))
            {
                result.AddMessage("The import file must be a CMS database file (.db)");
                result.Success = false;
                return result;
            }

            TaskTimer t1 = new TaskTimer("timer");
            Log($"Validating import file {sqlite_file}");
            List<string> new_locations = new List<string>();
            try
            {
                using (CMSDB db = new CMSDB())
                {
                    ImportValidator validator = new ImportValidator(db, root_location_id);
                    validator.Validate(sqlite_file);
                    result = validator.ValidationResult;
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                result.AddMessage(ex.Message);
                result.Success = false;
            }
            Log($"Validation took {t1.Format()}");
            return result;
        }

        public DatabaseValidationResult Import(string filename, int root_location_id, string login)
        {
            string lowfile = filename.ToLower();
            if (lowfile.EndsWith(".db") || lowfile.EndsWith(".sqlite"))
            {
                return ImportSQLite(filename, root_location_id, login);
            }
            if (lowfile.EndsWith(".xlsx"))
            {
                return ImportExcel(filename, root_location_id, login);
            }
            throw new Exception("Invalid filename: " + filename);
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       ImportSQLite
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Import a legacy CMS database
        /// </summary>
        ///
        /// <param name="filename">legacy SQLite database or Excel file path</param>
        /// <param name="root_location_id">the LocationID of the existing target location</param>
        /// <param name="login">the current user's login name</param>
        /// <returns>the full path of the target location</returns>
        ///
        ///----------------------------------------------------------------
        public DatabaseValidationResult ImportSQLite(string filename, int root_location_id, string login)
        {
            DatabaseValidationResult result;

            TaskTimer t1 = new TaskTimer("timer");
            m_login = login;
            m_root_location_id = root_location_id;

            Log($"Opening {filename}");
            m_new_location_id_map = new Dictionary<int, int>();
            m_new_location_string_map = new Dictionary<string, int>();
            using (CMSDB db = new CMSDB())
            {
                ImportValidator validator = new ImportValidator(db, root_location_id);

                if (!validator.Validate(filename))
                {
                    return validator.ValidationResult;
                }
                result = validator.ValidationResult;
                using (LegacyDatabase olddb = new LegacyDatabase(filename))
                {
                    if (!olddb.IsOpen)
                    {
                        throw new Exception($"Unable to connect to SQLite database: {olddb.LastError}");
                    }
                    result.Messages.Clear();

                    // create dictionaries of LocationTypes
                    m_location_type_id_map = validator.LocationTypeIDMap;
                    m_location_type_name_map = validator.LocationTypeNameMap;
                    m_root_location = validator.AttachmentLocation;

                    // see if every inventory item has a location
                    var dbresult = olddb.Query("select * from InventoryItems where LocationID is null or LocationID = 0", null);
                    m_need_nolocation_location = (dbresult.Rows.Count > 0);

                    olddb.Refresh();

                    string msg = $"Importing legacy database file {filename} into {m_root_location.Path}";
                    db.AddLogEntry(login, "import", msg, LogEntry.INFO_MESSAGE, false);
                    result.AddMessage(msg);

                    ImportOwners(olddb, db, result);
                    ImportGroups(olddb, db, result);
                    ImportNewLocations(olddb, db, result);
                }
            }

            // open a new database context so that new location paths are valid
            using (CMSDB db = new CMSDB())
            {
                using (LegacyDatabase olddb = new LegacyDatabase(filename))
                {
                    int items_imported = ImportNewItems(olddb, result, db);
                    result.ItemsImported = items_imported;
                }
                Console.WriteLine("Done");
                db.RefreshLocations();
                db.AddLogEntry(login, "import", $"Database import complete in  {t1.Format()}", LogEntry.INFO_MESSAGE, true);
                result.AddMessage($"Database import complete in  {t1.Format()}");
            }

            return result;
        }
        ///----------------------------------------------------------------
        ///
        /// Function:       ImportExcel
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Import an Excel file
        /// </summary>
        ///
        /// <param name="filename">legacy SQLite database or Excel file path</param>
        /// <param name="root_location_id">the LocationID of the existing target location</param>
        /// <param name="login">the current user's login name</param>
        /// <returns>the full path of the target location</returns>
        ///
        ///----------------------------------------------------------------
        public DatabaseValidationResult ImportExcel(string filename, int root_location_id, string login)
        {
            DatabaseValidationResult result;

            TaskTimer t1 = new TaskTimer("timer");
            m_login = login;
            m_root_location_id = root_location_id;
            ExcelImportFile excelfile;

            Log($"Processing {filename}");
            m_new_location_id_map = new Dictionary<int, int>();
            m_new_location_string_map = new Dictionary<string, int>();
            using (CMSDB db = new CMSDB())
            {
                ImportValidator validator = new ImportValidator(db, root_location_id);

                if (!validator.Validate(filename))
                {
                    return validator.ValidationResult;
                }
                excelfile = validator.ImportFile;
                result = validator.ValidationResult;
                result.Messages.Clear();

                // create dictionaries of LocationTypes
                m_location_type_id_map = validator.LocationTypeIDMap;
                m_location_type_name_map = validator.LocationTypeNameMap;
                m_root_location = validator.AttachmentLocation;

                // see if every inventory item has a location
                m_need_nolocation_location = excelfile.MissingLocationCount() > 0;

                string msg = $"Importing from spreadsheet {filename} into {m_root_location.Path}";
                db.AddLogEntry(login, "import", msg, LogEntry.INFO_MESSAGE, false);
                result.AddMessage(msg);

                ImportOwners(excelfile, db, result);
                ImportGroups(excelfile, db, result);
                ImportNewLocations(excelfile.Locations, db, result);
                // create a placeholder location for items that have no location
                if (m_need_nolocation_location)
                {
                    ImportNewLocation("Import", db, result);
                    db.SaveChanges();
                }
            }

            // open a new database context so that new location paths are valid
            using (CMSDB db = new CMSDB())
            {
                int items_imported = ImportNewItems(excelfile, result, db);
                result.ItemsImported = items_imported;
                Console.WriteLine("Done");
                db.AddLogEntry(login, "import", $"Database import complete in  {t1.Format()}", LogEntry.INFO_MESSAGE, true);
                result.AddMessage($"Database import complete in  {t1.Format()}");
                db.RefreshLocations();
            }

            return result;
        }

        private int ImportNewItems(LegacyDatabase olddb, DatabaseValidationResult result, CMSDB db)
        {
            int items_imported = 0;
            List<ImportLib.InventoryItem> inventory = olddb.FetchInventory();
            Log($"Importing {inventory.Count} inventory items");

            foreach (ImportLib.InventoryItem olditem in inventory)
            {
                if (ImportNewItem(olddb, db, olditem, result)) items_imported += 1;
            }
            db.SaveChanges();
            return items_imported;
        }

        private int ImportNewItems(ExcelImportFile excel, DatabaseValidationResult result, CMSDB db)
        {
            int items_imported = 0;
            List<ExcelRow> inventory_rows = excel.GetInventoryRows();
            Log($"Importing {inventory_rows.Count} inventory items");

            foreach (ExcelRow row in inventory_rows)
            {
                if (ImportNewItem(excel, db, row, result)) items_imported += 1;
            }
            db.SaveChanges();
            return items_imported;
        }

        public bool ImportNewItem(LegacyDatabase olddb, CMSDB db, ImportLib.InventoryItem olditem, DatabaseValidationResult result)
        {
            DataModel.InventoryItem existing = db.GetItemByBarcode(olditem.Barcode);
            if (existing != null)
            {
                string locname = db.Locations.GetLocationName(existing.LocationID);
                result.AddMessage($"Barcode {existing.Barcode} is already in the inventory at {locname}.");
                return false;
            }
            ImportLib.Owner old_owner = olddb.FindById(olditem.OwnerID);
            DataModel.Owner new_owner = null;
            if (old_owner != null) new_owner = db.Owners.FirstOrDefault(x => x.Name == old_owner.Name);

            int new_location_id = m_new_location_id_map[olditem.LocationID];  // undefined locations (0) will map to "./Import"
            DataModel.StorageLocation itemloc = db.ReadLocation(new_location_id);
            if (itemloc == null) throw new Exception($"Missing storage location {new_location_id}");
            ImportLib.StorageGroup old_group = olddb.FindStorageGroup(olditem.StorageGroupID);
            DataModel.StorageGroup new_group = null;
            if (old_group != null) new_group = db.StorageGroups.FirstOrDefault(x => x.Name == old_group.Name);

            // TODO: verify the MSDS reference is valid
            DataModel.InventoryItem newitem = new DataModel.InventoryItem()
            {
                Barcode = olditem.Barcode,
                CASNumber = olditem.CASNumber,
                ChemicalName = olditem.ChemicalName,
                LocationID = new_location_id,
                Owner = new_owner,
                DateIn = ParseDateTime(olditem.DateIn),
                ExpirationDate = ParseDateTime(olditem.ExpirationDate),
                Group = new_group,
                ContainerSize = olditem.ContainerSize,
                RemainingQuantity = olditem.RemainingQuantity,
                Units = olditem.Units,
                State = olditem.State,
                Flags = olditem.Flags,
                // PH: InventoryStatus will be removed in new audit model
                //InventoryStatusID = (olditem.InventoryStatus == 0 ? (int?)null : olditem.InventoryStatus),
                SDS = olditem.MSDS,
                Notes = olditem.Notes
                // PH: don't carry over stock check information
                // StockCheckLocation = olditem.StockCheckLocation
            };
            Console.WriteLine($"    {olditem.Barcode} {olditem.ChemicalName}");
            db.InventoryItems.Add(newitem);
            db.AddLogEntry(m_login, "import", $"Added InventoryItem {olditem.Barcode} at {itemloc.Path}", LogEntry.INFO_MESSAGE, false);
            return true;
        }

        public bool ImportNewItem(ExcelImportFile excelfile, CMSDB db, ExcelRow row, DatabaseValidationResult result)
        {
            string barcode = excelfile.GetRowValue(row, "Barcode");
            DataModel.InventoryItem existing = db.GetItemByBarcode(barcode);
            if (existing != null)
            {
                string locname = db.Locations.GetLocationName(existing.LocationID);
                result.AddMessage($"Barcode {existing.Barcode} is already in the inventory at {locname}.");
                return false;
            }
            string owner = excelfile.GetRowValue(row, "Owner");
            DataModel.Owner new_owner = null;
            if (!String.IsNullOrEmpty(owner)) new_owner = db.Owners.FirstOrDefault(x => x.Name == owner);

            string group = excelfile.GetRowValue(row, "Storage Group");
            DataModel.StorageGroup new_group = null;
            if (!String.IsNullOrEmpty(group)) new_group = db.StorageGroups.FirstOrDefault(x => x.Name == group);

            string locstr = excelfile.GetRowValue(row, "Location");
            if (String.IsNullOrEmpty(locstr))
            {
                locstr = "Import";
            }
            int new_location_id = m_new_location_string_map[locstr];  
            DataModel.StorageLocation itemloc = db.ReadLocation(new_location_id);

            InventoryItemFlags flags = new InventoryItemFlags();
            flags.CWC = excelfile.GetRowCharValue(row, "CWC");
            flags.THEFT = excelfile.GetRowCharValue(row, "Theft");
            flags.CARCINOGEN = excelfile.GetRowCharValue(row, "Carcinogen");
            flags.HEALTHHAZARD = excelfile.GetRowCharValue(row, "Health Hazard");
            flags.IRRITANT = excelfile.GetRowCharValue(row, "Irritant");
            flags.ACUTETOXICITY = excelfile.GetRowCharValue(row, "Acute Toxicity");
            flags.CORROSIVE = excelfile.GetRowCharValue(row, "Corrosive");
            flags.EXPLOSIVE = excelfile.GetRowCharValue(row, "Explosive");
            flags.FLAMABLE = excelfile.GetRowCharValue(row, "Flamable");
            flags.OXIDIZER = excelfile.GetRowCharValue(row, "Oxidizer");
            flags.COMPRESSEDGAS = excelfile.GetRowCharValue(row, "Gas Cylinder");
            flags.OTHERHAZARD = excelfile.GetRowCharValue(row, "Other Hazard");
            flags.OTHERSECURITY = excelfile.GetRowCharValue(row, "Other Security");

            string casnumber = excelfile.GetRowValue(row, "CAS #");
            string chemicalname = excelfile.GetRowValue(row, "Chemical Name");

            DataModel.InventoryItem newitem = new DataModel.InventoryItem()
            {
                Barcode = barcode,
                CASNumber = casnumber,
                ChemicalName = chemicalname,
                LocationID = new_location_id,
                Owner = new_owner,
                DateIn = ParseDateTime(excelfile.GetRowValue(row, "Date In")),
                ExpirationDate = ParseDateTime(excelfile.GetRowValue(row, "Expiration Date")),
                Group = new_group,
                ContainerSize = excelfile.GetRowDoubleValue(row, "Container Size"),
                RemainingQuantity = excelfile.GetRowDoubleValue(row, "Remaining Quantity"),
                Units = excelfile.GetRowValue(row, "Units"),
                State = excelfile.GetRowValue(row, "State"),
                Flags = flags.DatabaseString(),
                // PH: InventoryStatus will be removed in new audit model
                //InventoryStatusID = (olditem.InventoryStatus == 0 ? (int?)null : olditem.InventoryStatus),
                Notes = excelfile.GetRowValue(row, "Notes")
                // PH: don't carry over stock check information
                // StockCheckLocation = olditem.StockCheckLocation
            };
            Console.WriteLine($"    {barcode} {chemicalname}");
            db.InventoryItems.Add(newitem);
            db.AddLogEntry(m_login, "import", $"Added InventoryItem {barcode} at {itemloc.Path}", LogEntry.INFO_MESSAGE, false);
            return true;
        }

        private void ImportNewLocations(LegacyDatabase olddb, CMSDB db, DatabaseValidationResult result)
        {
            int count = 0;
            Log("Importing Locations");
            result.AddMessage("Importing Locations");
            DataModel.StorageLocation root = m_root_location;
            string root_location_path = db.Locations.GetFullLocationName(root.LocationID, true);
            foreach (StorageLocation loc in olddb.FetchStorageLocations())
            {
                string target_location_string = root_location_path + "/" + loc.Name;
                DataModel.StorageLocation target_location = db.FindLocation(target_location_string);
                if (target_location == null)
                {
                    int location_id = ImportNewLocation(loc.Name, db, result);
                    m_new_location_id_map.Add(loc.StorageLocationID, location_id);
                    m_new_location_string_map.Add(loc.Name, location_id);
                    count += 1;
                }
                else m_new_location_id_map[loc.StorageLocationID] = target_location.LocationID;
                result.LocationsImported = count;
                result.AddMessage($"Imported {count} locations.");
            }
            if (m_need_nolocation_location)
            {
                AddNolocationLocation(db, result);
            }
            db.SaveChanges();
            db.UpdateMissingLocationPaths();
            Log($"    Imported {count} locations");
            result.AddMessage($"    Imported {count} locations");
        }

        private void ImportNewLocations(List<string> location_strings, CMSDB db, DatabaseValidationResult result)
        {
            int count = 0;
            Log("Importing Locations");
            result.AddMessage("Importing Locations");
            foreach (string locstr in location_strings)
            {
                int location_id = ImportNewLocation(locstr, db, result);
                count += 1;
                m_new_location_string_map.Add(locstr, location_id);
                count += 1;
                result.LocationsImported = count;
                result.AddMessage($"Imported {count} locations.");
            }
            if (m_need_nolocation_location)
            {
                AddNolocationLocation(db, result);
            }
            db.SaveChanges();
            db.UpdateMissingLocationPaths();
            Log($"    Imported {count} locations");
            result.AddMessage($"    Imported {count} locations");
        }

        private void AddNolocationLocation(CMSDB db, DatabaseValidationResult result)
        {
            string msg = $"Added Location \"Import\" under {m_root_location.Path} for imported items with no location.";
            LocationType parent_loc_type = m_location_type_id_map[m_root_location.LocationTypeID];
            string child_location_typename = parent_loc_type.ValidChildList[0];
            LocationType child_location_type = m_location_type_name_map[child_location_typename];
            db.AddLogEntry(m_login, "import", msg, LogEntry.INFO_MESSAGE, false);
            result.AddMessage(msg);
            var newloc = db.AddLocation("Import", m_root_location_id, child_location_type.LocationTypeID, false);
            // map 0 the the LocationID of "./Import"
            m_new_location_id_map.Add(0, newloc.LocationID);
            m_new_location_string_map.Add("Import", newloc.LocationID);
        }



        ///----------------------------------------------------------------
        ///
        /// Function:       ImportNewLocation
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Import a new set of locations from a location string
        /// </summary>
        ///
        /// <param name="locstr">the location string ("xxx/yyy/zzz")</param>
        /// <param name="db">open database context</param>
        /// <param name="result">a DatabaseValidation result</param>
        /// <returns>LocationID of the endpoint of the location string</returns>
        ///
        ///----------------------------------------------------------------
        private int ImportNewLocation(string locstr, CMSDB db, DatabaseValidationResult result)
        {
            Log($"    Adding location \"{locstr}\"");
            result.AddMessage($"    Adding location \"{locstr}\"");
            string[] parts = CMSDB.ParseLocation(locstr);
            string partial_location = "/";
            DataModel.StorageLocation parent = m_root_location;
            string msg;

            foreach (string part in parts)
            {
                LocationType parent_loc_type = m_location_type_id_map[parent.LocationTypeID];
                partial_location += (part + "/");

                if (parent_loc_type.ValidChildren.Length == 0)
                {
                    throw new Exception($"Error: Locations of type {parent_loc_type.Name} cannot have child locations.");
                }
                string child_loc_type = parent_loc_type.ValidChildList[0];
                if (parent_loc_type.ValidChildList.Count > 0)
                {
                    (string match, double score) = DTW.Search(part, parent_loc_type.ValidChildList);
                    child_loc_type = match;
                }
                LocationType loctype = m_location_type_name_map[child_loc_type];
                db.AddLogEntry(m_login, "import", $"Added location {partial_location}", LogEntry.INFO_MESSAGE, false);
                // db.AddLocation ensures that existing entries are not duplicated
                var existing = db.Locations.FirstOrDefault(x => x.ParentID == parent.LocationID && x.Name == part);
                if (existing != null) result.AddMessage($"        Location {part} already exists under {parent.Name}.");
                else
                {
                    msg = $"Added Location {part} under {parent.Name}";
                    db.AddLogEntry(m_login, "import", msg, LogEntry.INFO_MESSAGE, false);
                    result.AddMessage("        " + msg);
                    parent = db.AddLocation(part, parent.LocationID, loctype.LocationTypeID, false);
                }
            }
            if (parent.LocationID == 0) throw new Exception("Need SaveChanges in order to get StorageLocationId");
            return parent.LocationID;

        }

        private void ImportGroups(LegacyDatabase olddb, CMSDB db, DatabaseValidationResult result)
        {
            Log("Importing Groups");
            result.AddMessage("Importing Groups");
            int count = 0;
            foreach (var g in olddb.FetchStorageGroups())
            {
                if (ImportGroup(g.Name, db, result)) count += 1;
            }
            Log($"    Imported {count} groups");
            result.AddMessage($"    Imported {count} groups");
            db.SaveChanges();
        }

        private void ImportGroups(ExcelImportFile excel, CMSDB db, DatabaseValidationResult result)
        {
            Log("Importing Groups");
            result.AddMessage("Importing Groups");
            int count = 0;
            foreach (string groupname in excel.StorageGroups)
            {
                if (ImportGroup(groupname, db, result)) count += 1;
            }
            Log($"    Imported {count} groups");
            result.AddMessage($"    Imported {count} groups");
            db.SaveChanges();
        }

        private bool ImportGroup(string name, CMSDB db, DatabaseValidationResult result)
        {
            bool rc = false;
            var newgroup = db.StorageGroups.FirstOrDefault(x => x.Name == name);
            if (newgroup == null)
            {
                newgroup = new DataModel.StorageGroup()
                {
                    Name = name
                };
                Log($"    Adding {name}");
                result.AddMessage($"    Adding {name}");
                db.StorageGroups.Add(newgroup);
                db.AddLogEntry(m_login, "import", $"Added Storage Group {name}", LogEntry.INFO_MESSAGE, false);
                rc = true;
            }
            else
            {
                Log($"    Storage group \"{name}\" already exists");
            }
            return rc;
        }

        private void ImportOwners(LegacyDatabase olddb, CMSDB db, DatabaseValidationResult result)
        {
            Log("Importing Owners");
            result.AddMessage("Importing Owners");
            int count = 0;
            foreach (var o in olddb.FetchOwners())
            {
                if (ImportOwner(o.Name, db, result)) count += 1;
            }
            db.SaveChanges();
            Log($"    Imported {count} owners");
            result.AddMessage($"    Imported {count} owners");
        }

        private void ImportOwners(ExcelImportFile excel, CMSDB db, DatabaseValidationResult result)
        {
            Log("Importing Owners");
            result.AddMessage("Importing Owners");
            int count = 0;
            foreach (string o in excel.Owners)
            {
                if (ImportOwner(o, db, result)) count += 1;
            }
            db.SaveChanges();
            Log($"    Imported {count} owners");
            result.AddMessage($"    Imported {count} owners");
        }

        private bool ImportOwner(string name, CMSDB db, DatabaseValidationResult result)
        {
            bool rc = false;
            var existing = db.Owners.FirstOrDefault(x => x.Name == name);
            if (existing == null)
            {
                DataModel.Owner owner = new DataModel.Owner()
                {
                    Name = name,
                };
                Log($"    {name}");
                db.Owners.Add(owner);
                db.AddLogEntry(m_login, "import", $"Added Owner {name}", LogEntry.INFO_MESSAGE, false);
                result.AddMessage($"    Added Owner {name}");
                rc = true;
            }
            return rc;
        }

    }
}
