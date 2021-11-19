using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataModel.Migrations
{
    public partial class _01_InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(maxLength: 64, nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    Description = table.Column<string>(maxLength: 128, nullable: false),
                    Data = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentID);
                });

            migrationBuilder.CreateTable(
                name: "CASDataItems",
                columns: table => new
                {
                    CASDataID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: false),
                    ChemicalName = table.Column<string>(maxLength: 1024, nullable: true),
                    CWCFlag = table.Column<string>(nullable: false),
                    TheftFlag = table.Column<string>(nullable: false),
                    CarcinogenFlag = table.Column<string>(nullable: false),
                    Pictograms = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASDataItems", x => x.CASDataID);
                });

            migrationBuilder.CreateTable(
                name: "CASDisposalProcedures",
                columns: table => new
                {
                    CASDisposalProcedureID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: false),
                    DisposalProcedureID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASDisposalProcedures", x => x.CASDisposalProcedureID);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalsOfConcern",
                columns: table => new
                {
                    COCID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChemicalName = table.Column<string>(maxLength: 256, nullable: false),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: false),
                    CWC = table.Column<bool>(nullable: false),
                    CFATS = table.Column<bool>(nullable: false),
                    EU = table.Column<bool>(nullable: false),
                    AG = table.Column<bool>(nullable: false),
                    WMD = table.Column<bool>(nullable: false),
                    OTHER = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalsOfConcern", x => x.COCID);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseQueries",
                columns: table => new
                {
                    DatabaseQueryID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    QueryText = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseQueries", x => x.DatabaseQueryID);
                });

            migrationBuilder.CreateTable(
                name: "DisposalProcedures",
                columns: table => new
                {
                    DisposalProcedureID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Schedule = table.Column<string>(maxLength: 8, nullable: true),
                    Category = table.Column<string>(maxLength: 32, nullable: true),
                    ChemicalName = table.Column<string>(nullable: true),
                    Treatment = table.Column<string>(maxLength: 64, nullable: true),
                    Techniques = table.Column<string>(nullable: false),
                    Products = table.Column<string>(nullable: true),
                    WasteDisposal = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalProcedures", x => x.DisposalProcedureID);
                });

            migrationBuilder.CreateTable(
                name: "GHSClassifications",
                columns: table => new
                {
                    LCSSInformationID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: false),
                    SourceID = table.Column<int>(nullable: false),
                    ChemicalName = table.Column<string>(maxLength: 4095, nullable: false),
                    CID = table.Column<int>(nullable: false),
                    IsHealthHazard = table.Column<bool>(nullable: false),
                    IsIrritant = table.Column<bool>(nullable: false),
                    IsAccuteToxicity = table.Column<bool>(nullable: false),
                    IsCorrosive = table.Column<bool>(nullable: false),
                    IsEnvironmental = table.Column<bool>(nullable: false),
                    IsExplosive = table.Column<bool>(nullable: false),
                    IsFlamable = table.Column<bool>(nullable: false),
                    IsOxidizer = table.Column<bool>(nullable: false),
                    IsCompressedGas = table.Column<bool>(nullable: false),
                    Pictograms = table.Column<string>(maxLength: 1024, nullable: false),
                    Signal = table.Column<string>(maxLength: 32, nullable: false),
                    HazardStatements = table.Column<string>(nullable: true),
                    PrecautionaryCodes = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GHSClassifications", x => x.LCSSInformationID);
                });

            migrationBuilder.CreateTable(
                name: "HazardCodes",
                columns: table => new
                {
                    HazardCodeID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GHSCode = table.Column<string>(maxLength: 16, nullable: false),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: false),
                    HazardClass = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HazardCodes", x => x.HazardCodeID);
                });

            migrationBuilder.CreateTable(
                name: "InventoryStatusNames",
                columns: table => new
                {
                    InventoryStatusID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStatusNames", x => x.InventoryStatusID);
                });

            migrationBuilder.CreateTable(
                name: "LocationLevelNames",
                columns: table => new
                {
                    LocationLevel = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationLevelNames", x => x.LocationLevel);
                });

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    LocationTypeID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    ValidChildren = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.LocationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    LogEntryID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntryDateTime = table.Column<DateTime>(nullable: false),
                    MessageLevel = table.Column<int>(nullable: false),
                    Login = table.Column<string>(maxLength: 256, nullable: false),
                    Category = table.Column<string>(maxLength: 32, nullable: false),
                    Text = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.LogEntryID);
                });

            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    OwnerID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.OwnerID);
                });

            migrationBuilder.CreateTable(
                name: "RemovedItems",
                columns: table => new
                {
                    RemovedItemID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InventoryID = table.Column<int>(nullable: false),
                    Barcode = table.Column<string>(maxLength: 64, nullable: false),
                    ChemicalName = table.Column<string>(maxLength: 256, nullable: true),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: true),
                    LocationID = table.Column<int>(nullable: false),
                    GroupID = table.Column<int>(nullable: true),
                    OwnerID = table.Column<int>(nullable: true),
                    DateIn = table.Column<DateTime>(nullable: true),
                    ExpirationDate = table.Column<DateTime>(nullable: true),
                    ContainerSize = table.Column<double>(nullable: true),
                    RemainingQuantity = table.Column<double>(nullable: true),
                    Units = table.Column<string>(maxLength: 64, nullable: true),
                    State = table.Column<string>(maxLength: 64, nullable: true),
                    Flags = table.Column<string>(maxLength: 16, nullable: true),
                    RemovalReason = table.Column<int>(nullable: false),
                    DateRemoved = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemovedItems", x => x.RemovedItemID);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SettingKey = table.Column<string>(maxLength: 64, nullable: false),
                    SettingValue = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingID);
                });

            migrationBuilder.CreateTable(
                name: "StorageGroups",
                columns: table => new
                {
                    GroupID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageGroups", x => x.GroupID);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    ReportID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReportName = table.Column<string>(maxLength: 256, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false),
                    DatabaseQueryID = table.Column<int>(nullable: false),
                    WhereClause = table.Column<string>(nullable: true),
                    Roles = table.Column<string>(maxLength: 64, nullable: false),
                    Widgets = table.Column<string>(maxLength: 64, nullable: false),
                    ColumnDefinitions = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_ReportDefinitions_DatabaseQueries_DatabaseQueryID",
                        column: x => x.DatabaseQueryID,
                        principalTable: "DatabaseQueries",
                        principalColumn: "DatabaseQueryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    LocationID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    ParentID = table.Column<int>(nullable: false),
                    LocationTypeID = table.Column<int>(nullable: false),
                    IsLeaf = table.Column<bool>(nullable: false),
                    LocationLevel = table.Column<int>(nullable: false),
                    Path = table.Column<string>(maxLength: 4095, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_StorageLocations_LocationTypes_LocationTypeID",
                        column: x => x.LocationTypeID,
                        principalTable: "LocationTypes",
                        principalColumn: "LocationTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    InventoryID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Barcode = table.Column<string>(maxLength: 64, nullable: false),
                    CASNumber = table.Column<string>(maxLength: 32, nullable: true),
                    ChemicalName = table.Column<string>(maxLength: 256, nullable: true),
                    LocationID = table.Column<int>(nullable: false),
                    GroupID = table.Column<int>(nullable: true),
                    OwnerID = table.Column<int>(nullable: true),
                    DateIn = table.Column<DateTime>(nullable: true),
                    ExpirationDate = table.Column<DateTime>(nullable: true),
                    ContainerSize = table.Column<double>(nullable: true),
                    RemainingQuantity = table.Column<double>(nullable: true),
                    Units = table.Column<string>(maxLength: 64, nullable: true),
                    State = table.Column<string>(maxLength: 64, nullable: true),
                    Flags = table.Column<string>(maxLength: 16, nullable: true),
                    IsOtherCOC = table.Column<bool>(nullable: false, defaultValue: false),
                    InventoryStatusID = table.Column<int>(nullable: true),
                    LastInventoryDate = table.Column<DateTime>(nullable: true),
                    SDS = table.Column<string>(maxLength: 32, nullable: true),
                    Notes = table.Column<string>(maxLength: 4096, nullable: true),
                    StockCheckPreviousLocation = table.Column<int>(nullable: true),
                    StockCheckTime = table.Column<DateTime>(nullable: true),
                    StockCheckUser = table.Column<string>(maxLength: 64, nullable: true),
                    MaterialType = table.Column<int>(nullable: false, defaultValue: 0),
                    DisposeFlag = table.Column<bool>(nullable: false),
                    Custom1 = table.Column<string>(maxLength: 256, nullable: true),
                    Custom2 = table.Column<string>(maxLength: 256, nullable: true),
                    Custom3 = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_InventoryItems_StorageGroups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "StorageGroups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_InventoryStatusNames_InventoryStatusID",
                        column: x => x.InventoryStatusID,
                        principalTable: "InventoryStatusNames",
                        principalColumn: "InventoryStatusID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_StorageLocations_LocationID",
                        column: x => x.LocationID,
                        principalTable: "StorageLocations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Owners_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "Owners",
                        principalColumn: "OwnerID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAudits",
                columns: table => new
                {
                    InventoryAuditID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuditTime = table.Column<DateTime>(nullable: false),
                    Barcode = table.Column<string>(maxLength: 64, nullable: false),
                    InventoryID = table.Column<int>(nullable: false),
                    LocationID = table.Column<int>(nullable: true),
                    PreviousLocationID = table.Column<int>(nullable: true),
                    User = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAudits", x => x.InventoryAuditID);
                    table.ForeignKey(
                        name: "FK_InventoryAudits_InventoryItems_InventoryID",
                        column: x => x.InventoryID,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Login",
                table: "Attachments",
                column: "Login");

            migrationBuilder.CreateIndex(
                name: "IX_CASDataItems_CASNumber",
                table: "CASDataItems",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CASDisposalProcedures_CASNumber",
                table: "CASDisposalProcedures",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalsOfConcern_CASNumber",
                table: "ChemicalsOfConcern",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HazardCodes_CASNumber",
                table: "HazardCodes",
                column: "CASNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HazardCodes_GHSCode",
                table: "HazardCodes",
                column: "GHSCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAudits_InventoryID",
                table: "InventoryAudits",
                column: "InventoryID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_GroupID",
                table: "InventoryItems",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryStatusID",
                table: "InventoryItems",
                column: "InventoryStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_LocationID",
                table: "InventoryItems",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_OwnerID",
                table: "InventoryItems",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_DatabaseQueryID",
                table: "ReportDefinitions",
                column: "DatabaseQueryID");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_LocationTypeID",
                table: "StorageLocations",
                column: "LocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_ParentID",
                table: "StorageLocations",
                column: "ParentID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "CASDataItems");

            migrationBuilder.DropTable(
                name: "CASDisposalProcedures");

            migrationBuilder.DropTable(
                name: "ChemicalsOfConcern");

            migrationBuilder.DropTable(
                name: "DisposalProcedures");

            migrationBuilder.DropTable(
                name: "GHSClassifications");

            migrationBuilder.DropTable(
                name: "HazardCodes");

            migrationBuilder.DropTable(
                name: "InventoryAudits");

            migrationBuilder.DropTable(
                name: "LocationLevelNames");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "RemovedItems");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "DatabaseQueries");

            migrationBuilder.DropTable(
                name: "StorageGroups");

            migrationBuilder.DropTable(
                name: "InventoryStatusNames");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropTable(
                name: "Owners");

            migrationBuilder.DropTable(
                name: "LocationTypes");
        }
    }
}
