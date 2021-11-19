using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

// mysql > create table HazardCodes(HazardCodeID int primary key, GHSCode varchar(16) not null, CASNumber varchar(32) not null);
// Query OK, 0 rows affected (0.06 sec)

// mysql > create index IX_HazardCodes_CASNumber on HazardCodes (CASNumber);
// Query OK, 0 rows affected (0.04 sec)
// Records: 0  Duplicates: 0  Warnings: 0

// mysql > create index IX_HazardCodes_GHSCode on HazardCodes (GHSCode);
// Query OK, 0 rows affected (0.07 sec)
// Records: 0  Duplicates: 0  Warnings: 0

namespace DataModel
{
    public enum EFLAG
    {
        CWC = 0, THEFT, OTHERSECURITY, CARCINOGEN, HEALTHHAZARD, IRRITANT,
        ACUTETOXICITY, CORROSIVE, EXPLOSIVE, FLAMABLE, OXIDIZER, COMPRESSEDGAS,
        ENVIRONMENT, OTHERHAZARD
    }

    // Storage location levels
    public enum ELocationLevel { INSTITUTION = 0, SITE, LOCALE, BUILDING, ROOM, STORAGE, SHELF, LEVEL7, LEVEL8, LEVEL9, LEVEL10 }

    public enum EInventoryStatus { UNKNOWN = 0, CONFIRMED, CONFIRMED_AT_NEW_LOCATION, NOT_FOUND }

    public enum EMaterialType { CHEMICAL = 0, BIOLOGICAL, RADIOLOGICAL, OTHER = 99 }


    public class Owner
    {
        [Key]
        public int OwnerID { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }

        [NotMapped]
        public bool IsChanged { get; set; }

        [NotMapped]
        public bool IsDeleted { get; set; }
    }

    public class StorageLocation
    {
        [Key]
        public int LocationID { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }
        [Required]
        public int ParentID { get; set; }

        [Required]
        public int LocationTypeID { get; set; }

        [DefaultValue(false)]
        public bool IsLeaf { get; set; } = false;

        // PH - don't include joined LocationType because
        // that will cause it to be hydrated any time a 
        // StorageLocation is serialized

        [ForeignKey("LocationTypeID")]
        public LocationType LocationType { get; set; }

        public int LocationLevel { get; set; }

        [MaxLength(4095)]
        public string Path { get; set; }

        [NotMapped]
        public bool IsChanged { get; set; }

        [NotMapped]
        public ELocationLevel LevelEnum { get { return LocationLevelName.GetLevelEnum(LocationLevel); } }

        [NotMapped]
        public string FullLocation { get; set; }

        [NotMapped]
        public string ShortLocation { get; set; }


        public override string ToString()
        {
            return $"[Location {Name},{LocationLevel},{ParentID}]";
        }
    }


    ///----------------------------------------------------------------
    ///
    /// Class:          LocationType
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Class to represent site-specific location types, e.g. Ministy, University, etc.
    /// </summary>
    /// 
    /// <remarks>
    /// LocationType and LocationSchema were added to make location hierarchies
    /// more flexible and not based on rigid location level names. For Iraq, 
    /// a Store can be under University, Department, or Branch, and a cabinet
    /// or shelf can appear under a Lab or a Store.
    /// 
    /// LocationType is a database entity.  LocationSchema is just a
    /// convenience class.  A site's location schema is stored in a
    /// a Setting named Site/LocationSchema (see CMSDB.LocationSchemaKey).
    /// </remarks>
    ///
    ///----------------------------------------------------------------
    public class LocationType
    {
        private List<string> m_valid_child_list;

        [Key]
        public int LocationTypeID { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [Required, MaxLength(255)]
        // comma separated list of names
        public string ValidChildren { get; set; }

        [NotMapped]
        public List<string> ValidChildList
        {
            get
            {
                if (m_valid_child_list == null)
                {
                    m_valid_child_list = Utility.SplitString(ValidChildren, ',');
                }
                return m_valid_child_list;
            }
        }

        [NotMapped]
        public bool IsDeleted { get; set; }
    }


    public class LocationSchema : Dictionary<string, LocationType>
    {

        ///----------------------------------------------------------------
        ///
        /// Function:       ValidChildren
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get a list of the location type names that can be under a given parent.
        /// </summary>
        ///
        /// <param name="location_type_name">type name of the parent type</param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public List<string> ValidChildren(string location_type_name)
        {
            List<string> result = null;
            if (this.ContainsKey(location_type_name))
            {
                LocationType ltype = this[location_type_name];
                return ltype.ValidChildList;
            }
            return result;
        }

        public bool CanAttach(string parent_type, string child_type)
        {
            List<string> valid_children = ValidChildren(parent_type);
            return (valid_children != null && valid_children.Contains(child_type));
        }
    }


    public class LocationLevelName
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LocationLevel { get; set; }
        [Required, MaxLength(16)]
        public string Name { get; set; }

        public LocationLevelName()
        {

        }

        public LocationLevelName(int level, string name)
        {
            LocationLevel = level;
            Name = name;
        }

        public LocationLevelName(ELocationLevel level, string name)
        {
            LocationLevel = (int)level;
            Name = name;
        }

        [NotMapped]
        public ELocationLevel LevelEnum { get { return LocationLevelName.GetLevelEnum(LocationLevel); } }

        public static ELocationLevel GetLevelEnum(int level)
        {
            if (level < 0 || level > (int)ELocationLevel.LEVEL10) throw new Exception($"Invalid LocationLevel value {level}");
            return (ELocationLevel)level;
        }


        public override string ToString()
        {
            return $"[LevelName {Name},{LocationLevel}]";
        }
    }

    public class StorageGroup
    {
        [Key]
        public int GroupID { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }

        [NotMapped]
        public bool IsChanged { get; set; }

        [NotMapped]
        public bool IsDeleted { get; set; }
    }

    public class Setting
    {
        [Key]
        public int SettingID { get; set; }
        [Required, MaxLength(64)]
        public string SettingKey { get; set; }
        [Required]
        public string SettingValue { get; set; }

        [NotMapped]
        public bool IsChanged { get; set; }
    }

    [Table("RemovedItems")]
    public class RemovedItem
    {
        public enum ERemovalReason { UNKNOWN = 0, NOTFOUND = 1, DELETED = 2 }

        [Key]
        public int RemovedItemID { get; set; }

        [Required]
        public int InventoryID { get; set; }

        [Required, MaxLength(64)]
        public string Barcode { get; set; }

        [MaxLength(256)]
        public string ChemicalName { get; set; }

        [MaxLength(32)]
        public string CASNumber { get; set; }

        public int LocationID { get; set; }

        public int? GroupID { get; set; }

        public int? OwnerID { get; set; }

        public DateTime? DateIn { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public double? ContainerSize { get; set; }

        public double? RemainingQuantity { get; set; }

        [MaxLength(64)]
        public string Units { get; set; }

        [MaxLength(64)]
        public string State { get; set; }

        [MaxLength(16)]
        public string Flags { get; set; }

        [Required]
        public ERemovalReason RemovalReason { get; set; } = ERemovalReason.UNKNOWN;

        [Required]
        public DateTime DateRemoved { get; set; }

        public RemovedItem()
        {

        }

        public RemovedItem(InventoryItem item, ERemovalReason reason)
        {
            InventoryID = item.InventoryID;
            Barcode = item.Barcode;
            ChemicalName = item.ChemicalName;
            CASNumber = item.CASNumber;
            LocationID = item.LocationID;
            GroupID = item.GroupID;
            OwnerID = item.OwnerID;
            DateIn = item.DateIn;
            ExpirationDate = item.ExpirationDate;
            ContainerSize = item.ContainerSize;
            RemainingQuantity = item.RemainingQuantity;
            Units = item.Units;
            State = item.State;
            Flags = item.Flags;
            RemovalReason = reason;
            DateRemoved = DateTime.Now;
        }
    }

    public class InventoryStatus
    {
        [Key]
        public int InventoryStatusID { get; set; }
        [Required, MaxLength(256)]
        public string Name { get; set; }
    }

    public class CASData
    {
        [Key]
        public int CASDataID { get; set; }
        [Required, MaxLength(32)]
        public string CASNumber { get; set; }
        [MaxLength(1024)]
        public string ChemicalName { get; set; }
        [Required]
        public char CWCFlag { get; set; }
        [Required]
        public char TheftFlag { get; set; }
        [Required]
        public char CarcinogenFlag { get; set; }
        [MaxLength(256)]
        public string Pictograms { get; set; }
    }

    public class InventoryItem
    {
        [Key]
        /// Primary Key
        public int InventoryID { get; set; }
        [Required, MaxLength(64)]
        public string Barcode { get; set; }
        [MaxLength(32)]
        public string CASNumber { get; set; }
        [MaxLength(256)]
        public string ChemicalName { get; set; }
        public int LocationID { get; set; }
        [ForeignKey("LocationID")]
        public StorageLocation Location { get; set; }
        //public int SiteID { get; set; }
        public int? GroupID { get; set; }
        [ForeignKey("GroupID")]
        public StorageGroup Group { get; set; }
        public int? OwnerID { get; set; }
        [ForeignKey("OwnerID")]
        public Owner Owner { get; set; }
        public DateTime? DateIn { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public double? ContainerSize { get; set; }
        public double? RemainingQuantity { get; set; }
        [MaxLength(64)]
        public string Units { get; set; }
        [MaxLength(64)]
        public string State { get; set; }

        /// Flags is a character string encoding GHS hazards.
        /// The EFLAG enum specifies the location of each hazard flag.
        /// Each flag will be a space or 'X' to indicate off or on.
        /// Note the first three characters (CWC, THEFT, CARCINOGEN)
        /// have been replaced by booleans in the ChemicalsOfConcern table.
        [MaxLength(16)]
        public string Flags { get; set; }

        [DefaultValue(0)]
        public bool IsOtherCOC { get; set; }
        public int? InventoryStatusID { get; set; }
        [ForeignKey("InventoryStatusID")]
        public InventoryStatus InventoryStatus { get; set; }
        public DateTime? LastInventoryDate { get; set; }
        [MaxLength(32)]
        public string SDS { get; set; }
        [MaxLength(4096)]
        public string Notes { get; set; }

        //---------------------------------------------------------------------
        // Stock Check Fields
        //
        // If StockCheckPreviousLocation is null, this item has not yet been confirmed
        //     otherwise it be the ID of the location it was supposed to be at
        // StockCheckTime records the date/time of the item confirmation
        // StockCheckUser records the login name of the user that confirmed
        //---------------------------------------------------------------------
        public int? StockCheckPreviousLocation { get; set; }
        public DateTime? StockCheckTime { get; set; }
        [MaxLength(64)]
        public string StockCheckUser { get; set; }


        //----------------------------------------------------------------
        //
        // New items from feature requests
        //
        //----------------------------------------------------------------
        [Required]
        [DefaultValue(EMaterialType.CHEMICAL)]
        public EMaterialType MaterialType { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool DisposeFlag { get; set; }

        [MaxLength(256)]
        public string Custom1 { get; set; }

        [MaxLength(256)]
        public string Custom2 { get; set; }

        [MaxLength(256)]
        public string Custom3 { get; set; }


        //----------------------------------------------------------------
        //
        // Fields that are not mapped to the database
        //
        //----------------------------------------------------------------

        [NotMapped]
        public string FullLocation { get; set; }

        [NotMapped]
        public string ShortLocation { get; set; }

        [NotMapped]
        // Interface to item's flags for accessing flags by name
        public InventoryItemFlags ItemFlags { get; set; }

        [NotMapped]
        public string DisplayFlags { get; set; }

        //[NotMapped]
        //public string SiteName { get; set; }

        [NotMapped]
        public string status { get; set; }  // confirmed, unconfirmed, missing


        ///----------------------------------------------------------------
        ///
        /// Function:       Constructor
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// InventoryItem constructor
        /// </summary>
        ///
        ///----------------------------------------------------------------
        public InventoryItem()
        {
            MaterialType = EMaterialType.CHEMICAL;
            IsOtherCOC = false;
            DisposeFlag = false;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       InitializeItemFlags
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Initialize the hazard flags for an inventory item
        /// </summary>
        ///
        /// <param name="casdata">the CASData associated with the item</param>
        /// <returns>this</returns>
        ///
        ///----------------------------------------------------------------
        public InventoryItem InitializeItemFlags(CASData casdata)
        {
            ItemFlags = new InventoryItemFlags(Flags, casdata);
            DisplayFlags = ItemFlags.ToString();
            return this;
        }

        ///----------------------------------------------------------------
        ///
        /// Function:       InitializeItemFlags
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Initialize the hazard flags for an inventory item
        /// </summary>
        ///
        /// <param name="flags">a 16-character string with X's for flags to set</param>
        /// <returns>this</returns>
        ///
        ///----------------------------------------------------------------
        public InventoryItem InitializeItemFlags(string flags)
        {
            ItemFlags = new InventoryItemFlags(flags, null);
            DisplayFlags = ItemFlags.ToString();
            return this;
        }

        public InventoryItem InitializeItemFlags(CMSDB db)
        {
            CASData casdata = db.GetCASData(CASNumber);
            return InitializeItemFlags(casdata);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       SetItemFlagsFromPictograms
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Set individual hazard flags based on pictograms associated with CAS#
        /// </summary>
        ///
        /// <param name="db">an open database context</param>
        /// <returns></returns>
        ///
        ///----------------------------------------------------------------
        public void SetItemFlagsFromPictograms(CMSDB db)
        {
            CASData casdata = db.GetCASData(CASNumber);
            if (casdata != null  &&  !String.IsNullOrEmpty(casdata.Pictograms))
            {
                string[] names = casdata.Pictograms.Split(',');
                foreach (string name in names)
                {
                    switch (name)
                    {
                        case "GHS01":   // Explosive
                            ItemFlags.EXPLOSIVE = 'X';
                            break;
                        case "GHS02":   // Flammable
                            ItemFlags.FLAMABLE = 'X';
                            break;
                        case "GHS03":   // Oxidizing
                            ItemFlags.OXIDIZER = 'X';
                            break;
                        case "GHS04":   // Compressed Gas
                            ItemFlags.COMPRESSEDGAS = 'X';
                            break;
                        case "GHS05":   // Corrosive
                            ItemFlags.CORROSIVE = 'X';
                            break;
                        case "GHS06":   // Acute toxicity
                            ItemFlags.ACUTETOXICITY = 'X';
                            break;
                        case "GHS07":   // Harmful (we call it "Irritant"
                            ItemFlags.IRRITANT = 'X';
                            break;
                        case "GHS08":   // Health Hazard
                            ItemFlags.HEALTHHAZARD = 'X';
                            break;
                        case "GHS09":   // Environmental Hazard
                            ItemFlags.ENVIRONMENT = 'X';
                            break;
                    }
                }
                Flags = ItemFlags.ToString();
                DisplayFlags = Flags;
            }
        }

        public InventoryItem ExpandLocation(CMSDB db, bool force)
        {
            if (force || FullLocation == null) FullLocation = db.GetLocationName(LocationID, 1);
            if (force || ShortLocation == null) ShortLocation = db.GetShortLocationName(LocationID);
            //if (String.IsNullOrEmpty(SiteName))
            //{
            //    var site = db.FindLocation(SiteID);
            //    if (site != null) SiteName = site.Name;
            //}
            return this;
        }

        public void SetFlags()
        {
            if (ItemFlags != null) Flags = ItemFlags.ToString();
        }

        public bool SetStockCheckInformation(int location_id, string username, bool at_expected_location)
        {
            bool result = (StockCheckPreviousLocation == null);
            if (StockCheckPreviousLocation == null) StockCheckPreviousLocation = LocationID;
            LocationID = location_id;
            StockCheckTime = DateTime.Now;
            StockCheckUser = username;
            InventoryStatusID = (at_expected_location ? (int)EInventoryStatus.CONFIRMED : (int)EInventoryStatus.CONFIRMED_AT_NEW_LOCATION);
            return result;
        }

        public void ClearStockCheckInformation(int location_id, string username)
        {
            StockCheckPreviousLocation = null;
            StockCheckTime = null;
            StockCheckUser = null;
            InventoryStatusID = null;
        }
    }

    //public class RemovedInventoryItem
    //{
    //    /// <summary>
    //    /// Redefine this, and turn off auto-increment so we'll use
    //    /// the value assigned when it went into the InventoryItems table
    //    /// </summary>
    //    [Key]
    //    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    //    public int InventoryID { get; set; }

    //    [Required]
    //    public DateTime DateOut { get; set; }
    //}


    ///----------------------------------------------------------------
    ///
    /// Class:          InventoryItemFlags
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// Utility class to facilitate working with InventoryItem flags.
    /// </summary>
    /// 
    /// <remarks>
    /// This class has getters and setters for the various flags in
    /// the InventoryItem.Flags field.  A constructor is provided that
    /// takes a InventoryItem.Flags string and an optional CASData instance
    /// (corresponding to the InventoryItem CASNumber). Since the security
    /// flags were moved into ChemicalsOfConcern the CASData argument is
    /// no longer necessary.  ToString is overridden to return the 
    /// InventoryItem.Flags value in the proper format.
    /// </remarks>
    ///
    ///----------------------------------------------------------------
    public class InventoryItemFlags
    {
        public char[] Values;

        public char this[EFLAG flag]
        {
            get { return Values[(int)flag]; }
            set { Values[(int)flag] = value; }
        }

        public char CWC { get => this[EFLAG.CWC]; set => this[EFLAG.CWC] = value; }
        public char THEFT { get => this[EFLAG.THEFT]; set => this[EFLAG.THEFT] = value; }
        public char OTHERSECURITY { get => this[EFLAG.OTHERSECURITY]; set => this[EFLAG.OTHERSECURITY] = value; }
        public char CARCINOGEN { get => this[EFLAG.CARCINOGEN]; set => this[EFLAG.CARCINOGEN] = value; }
        public char HEALTHHAZARD { get => this[EFLAG.HEALTHHAZARD]; set => this[EFLAG.HEALTHHAZARD] = value; }
        public char IRRITANT { get => this[EFLAG.IRRITANT]; set => this[EFLAG.IRRITANT] = value; }
        public char ACUTETOXICITY { get => this[EFLAG.ACUTETOXICITY]; set => this[EFLAG.ACUTETOXICITY] = value; }
        public char CORROSIVE { get => this[EFLAG.CORROSIVE]; set => this[EFLAG.CORROSIVE] = value; }
        public char EXPLOSIVE { get => this[EFLAG.EXPLOSIVE]; set => this[EFLAG.EXPLOSIVE] = value; }
        public char FLAMABLE { get => this[EFLAG.FLAMABLE]; set => this[EFLAG.FLAMABLE] = value; }
        public char OXIDIZER { get => this[EFLAG.OXIDIZER]; set => this[EFLAG.OXIDIZER] = value; }
        public char COMPRESSEDGAS { get => this[EFLAG.COMPRESSEDGAS]; set => this[EFLAG.COMPRESSEDGAS] = value; }
        public char ENVIRONMENT { get => this[EFLAG.ENVIRONMENT]; set => this[EFLAG.ENVIRONMENT] = value; }
        public char OTHERHAZARD { get => this[EFLAG.OTHERHAZARD]; set => this[EFLAG.OTHERHAZARD] = value; }


        public InventoryItemFlags()
        {
            Reset();
        }

        public InventoryItemFlags(string flagstr, CASData casdata)
        {
            Reset();
            if (flagstr.Length > 16) flagstr = flagstr.Substring(0, 16);
            if (flagstr.Length < 16) flagstr = flagstr.PadRight(16);
            Values = flagstr.ToCharArray();
            if (casdata != null)
            {
                Values[(int)EFLAG.CWC] = casdata.CWCFlag;
                Values[(int)EFLAG.THEFT] = casdata.TheftFlag;
                Values[(int)EFLAG.CARCINOGEN] = casdata.CarcinogenFlag;
            }
        }

        public void Reset()
        {
            Values = "                ".ToCharArray();
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       DatabaseString
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Return a string for storing in the database
        /// </summary>
        ///
        /// <returns>a string with asterisks for CWC, THEFT, and CARCINOGEN</returns>
        ///
        ///----------------------------------------------------------------
        public string DatabaseString()
        {
            char[] result = new char[16];
            Array.Copy(Values, result, 16);
            result[(int)EFLAG.CWC] = '*';
            result[(int)EFLAG.THEFT] = '*';
            result[(int)EFLAG.CARCINOGEN] = '*';
            return new string(result);
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       ToString
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Return a string with CWC, THEFT, and CARCINOGEN values from CASData
        /// </summary>
        ///
        /// <returns>a string with hazard flags and CASData values for CWC, 
        /// THEFT, and CARCINOGEN (which are no longer used)
        /// </returns>
        ///
        ///----------------------------------------------------------------
        public override string ToString()
        {
            return new string(Values).ToUpper();
        }
    }






    public class LogEntry
    {
        public const int INFO_MESSAGE = 0;
        public const int WARNING_MESSAGE = 1;
        public const int ERROR_MESSAGE = 2;

        [Key]
        public int LogEntryID { get; set; }
        [Required]
        public DateTime EntryDateTime { get; set; }
        [Required]
        public int MessageLevel { get; set; }  // 0 = INFO, 1 = WARNING, 2 = ERROR
        [Required, MaxLength(256)]
        public string Login { get; set; }
        [Required, MaxLength(32)]
        public string Category { get; set; }
        [Required, MaxLength(1024)]
        public string Text { get; set; }

        public LogEntry()
        {
            EntryDateTime = DateTime.Now;
        }
    }

    public class ReportDefinition
    {
        public delegate List<Dictionary<string, string>> ReportDGenerator(CMSDB db);

        [Key]
        public int ReportID { get; set; }
        [Required, MaxLength(256)]
        public string ReportName { get; set; }
        [Required, MaxLength(1024)]
        public string Description { get; set; }
        public int DatabaseQueryID { get; set; }
        [ForeignKey("DatabaseQueryID")]
        public DatabaseQuery Query { get; set; }
        public string WhereClause { get; set; }
        [Required, MaxLength(64)]
        public string Roles { get; set; }
        [Required, MaxLength(64)]
        public string Widgets { get; set; }
        public string ColumnDefinitions { get; set; }     // JSON to configure Vue Grid

        [NotMapped]
        public ReportDGenerator Generate;


        public ReportDefinition()
        {
        }

        public UserRoles Permissions()
        {
            UserRoles result = new UserRoles();
            result.Assign(Roles);
            return result;
        }


        ///----------------------------------------------------------------
        ///
        /// Function:       CanRun
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Determine whether a user can run this report
        /// </summary>
        ///
        /// <param name="user_permissions">the user's permissions</param>
        /// <returns>true/false</returns>
        ///
        ///----------------------------------------------------------------
        public bool CanRun(UserRoles user_permissions)
        {
            UserRoles report_permissions = Permissions();
            return report_permissions.Intersects(user_permissions);
        }


        public ReportDefinition AddColumn(string column_name, string header, int width, string datatype = null)
        {
            return this;
        }

        public List<Dictionary<string, string>> Run(CMSDB db)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            DatabaseQuery query = db.DatabaseQueries.FirstOrDefault(x => x.DatabaseQueryID == this.DatabaseQueryID);
            if (query != null)
            {
                DbContextHelper.ExecuteUnboundQuery(db, query.QueryText, (reader) =>
                {
                    Dictionary<string, string> rowdata = new Dictionary<string, string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var val = reader[i];
                        string valstr = (val == DBNull.Value ? "null" : val.ToString());
                        rowdata[reader.GetName(i)] = val.ToString();
                    }
                    result.Add(rowdata);
                    return true;
                });
                return result;
            }
            else return null;
        }

        public List<Dictionary<string, string>> Run()
        {
            using (CMSDB db = new CMSDB())
            {
                return Run(db);
            }
        }
    }


    ///----------------------------------------------------------------
    ///
    /// Class:          DatabaseQuery class/table
    /// Author:         Pete Humphrey
    ///
    /// <summary>
    /// This class stores a predefined database query that can be be run
    /// using the DatabaseQuery Vue component
    /// </summary>
    /// 
    /// <remarks>
    /// Columns:
    ///     DatabaseQueryID     primary key
    ///     Name                a short name that the Vue component will use
    ///     SQL                 the parameterized SQL that you want to run
    ///     
    /// Parameters should appear in your query as @paramname.  The Vue
    /// component is passed a query definition structure that contains
    /// parameters as name/value pairs. where the names must match the
    /// placeholders in your SQL (without the @-sign).
    /// 
    /// </remarks>
    ///
    ///----------------------------------------------------------------
    public class DatabaseQuery
    {
        [Key]
        public int DatabaseQueryID { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }
        [Required]
        public string QueryText { get; set; }
    }


    public class GHSData
    {
        [Key]
        public int LCSSInformationID { get; set; }

        [Required, MaxLength(32)]
        public string CASNumber { get; set; }

        [Required]
        public int SourceID { get; set; }

        [Required, MaxLength(4095)]
        public string ChemicalName { get; set; }

        [Required]
        public int CID { get; set; }

        [Required]
        public bool IsHealthHazard { get; set; }

        [Required]
        public bool IsIrritant { get; set; }

        [Required]
        public bool IsAccuteToxicity { get; set; }

        [Required]
        public bool IsCorrosive { get; set; }

        [Required]
        public bool IsEnvironmental { get; set; }

        [Required]
        public bool IsExplosive { get; set; }

        [Required]
        public bool IsFlamable { get; set; }

        [Required]
        public bool IsOxidizer { get; set; }

        [Required]
        public bool IsCompressedGas { get; set; }

        [Required, MaxLength(1024)]
        public string Pictograms { get; set; }      // name|name|...

        [Required, MaxLength(32)]
        public string Signal { get; set; }          // Warning, Danger, ...

        public string HazardStatements { get; set; }

        public string PrecautionaryCodes { get; set; }

        public GHSData()
        {
            CASNumber = "";
            ChemicalName = "";
            Pictograms = "";
            Signal = "";
        }
    }

    public class ChemicalOfConcern
    {
        [Key]
        public int COCID { get; set; }
        [Required, MaxLength(256)]
        public string ChemicalName { get; set; }
        [Required, MaxLength(32)]
        public string CASNumber { get; set; }
        [Required]
        public bool CWC { get; set; }
        [Required]
        public bool CFATS { get; set; }
        [Required]
        public bool EU { get; set; }
        [Required]
        public bool AG { get; set; }
        [Required]
        public bool WMD { get; set; }
        [Required]
        public bool OTHER { get; set; }
    }

    public class InventoryAudit
    {
        [Key]
        public int InventoryAuditID { get; set; }

        /// <summary>
        /// The time the audit occurred
        /// </summary>
        [Required]
        public DateTime AuditTime { get; set; }

        [Required, MaxLength(64)]
        public string Barcode { get; set; }

        /// <summary>
        /// The InventoryID in either the InventoryItems table, 
        /// or in the RemovedInventoryItems table, if the item
        /// has been removed.
        /// </summary>
        [Required]
        public int InventoryID { get; set; }


        /// <summary>
        /// The item that was audited
        /// </summary>
        public InventoryItem Item { get; set; }

        public int? LocationID { get; set; }

        public int? PreviousLocationID { get; set; }

        /// <summary>
        /// Where the inventory item is as of the time of this audit.
        /// Can be null if the item is removed.
        /// </summary>
        [NotMapped]
        public StorageLocation CurrentLocation { get; set; }

        /// <summary>
        /// The user performing the audit. Is a GUID that links
        /// to the CMSUsers.aspnetusers.ID field
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string User { get; set; }

        public InventoryAudit()
        {
            AuditTime = DateTime.Now;
        }
    }


    public class Attachment
    {
        [Key]
        public int AttachmentID { get; set; }
        [Required, MaxLength(64)]
        public string Login { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }
        [Required, MaxLength(128)]
        public string Description { get; set; }
        [Required]
        public byte[] Data { get; set; }

    }

    public class HazardCode
    {
        [Key]
        public int HazardCodeID { get; set; }

        [Required, MaxLength(16)]
        public string GHSCode { get; set; }

        [Required, MaxLength(32)]
        public string CASNumber { get; set; }

        [Required, MaxLength(128)]
        public string HazardClass { get; set; }
    }

    public class DisposalProcedure
    {
        [Key]
        public int DisposalProcedureID { get; set; }

        [MaxLength(8)]
        public string Schedule { get; set; }

        [MaxLength(32)]
        public string Category { get; set; }

        public string ChemicalName { get; set; }

        [MaxLength(64)]
        public string Treatment { get; set; }

        [Required]
        public string Techniques { get; set; }

        public string Products { get; set; }

        public string WasteDisposal { get; set; }
    }

    public class CASDisposalProcedure
    {
        [Key]
        public int CASDisposalProcedureID { get; set; }

        [Required, MaxLength(32)]
        public string CASNumber { get; set; }

        [Required]
        public int DisposalProcedureID { get; set; }
    }
}
