CREATE TABLE `__EFMigrationsHistory` (
    `MigrationId` varchar(95) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
);

CREATE TABLE `Attachments` (
    `AttachmentID` int NOT NULL AUTO_INCREMENT,
    `Login` varchar(64) NOT NULL,
    `Name` varchar(64) NOT NULL,
    `Description` varchar(128) NOT NULL,
    `Data` longblob NOT NULL,
    CONSTRAINT `PK_Attachments` PRIMARY KEY (`AttachmentID`)
);

CREATE TABLE `CASDataItems` (
    `CASDataID` int NOT NULL AUTO_INCREMENT,
    `CASNumber` varchar(32) NOT NULL,
    `ChemicalName` varchar(1024) NULL,
    `CWCFlag` varchar(1) NOT NULL,
    `TheftFlag` varchar(1) NOT NULL,
    `CarcinogenFlag` varchar(1) NOT NULL,
    `Pictograms` varchar(256) NULL,
    CONSTRAINT `PK_CASDataItems` PRIMARY KEY (`CASDataID`)
);

CREATE TABLE `CASDisposalProcedures` (
    `CASDisposalProcedureID` int NOT NULL AUTO_INCREMENT,
    `CASNumber` varchar(32) NOT NULL,
    `DisposalProcedureID` int NOT NULL,
    CONSTRAINT `PK_CASDisposalProcedures` PRIMARY KEY (`CASDisposalProcedureID`)
);

CREATE TABLE `ChemicalsOfConcern` (
    `COCID` int NOT NULL AUTO_INCREMENT,
    `ChemicalName` varchar(256) NOT NULL,
    `CASNumber` varchar(32) NOT NULL,
    `CWC` bit NOT NULL,
    `CFATS` bit NOT NULL,
    `EU` bit NOT NULL,
    `AG` bit NOT NULL,
    `WMD` bit NOT NULL,
    `OTHER` bit NOT NULL,
    CONSTRAINT `PK_ChemicalsOfConcern` PRIMARY KEY (`COCID`)
);

CREATE TABLE `DatabaseQueries` (
    `DatabaseQueryID` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(64) NOT NULL,
    `QueryText` longtext NOT NULL,
    CONSTRAINT `PK_DatabaseQueries` PRIMARY KEY (`DatabaseQueryID`)
);

CREATE TABLE `DisposalProcedures` (
    `DisposalProcedureID` int NOT NULL AUTO_INCREMENT,
    `Schedule` varchar(8) NULL,
    `Category` varchar(32) NULL,
    `ChemicalName` longtext NULL,
    `Treatment` varchar(64) NULL,
    `Techniques` longtext NOT NULL,
    `Products` longtext NULL,
    `WasteDisposal` longtext NULL,
    CONSTRAINT `PK_DisposalProcedures` PRIMARY KEY (`DisposalProcedureID`)
);

CREATE TABLE `GHSClassifications` (
    `LCSSInformationID` int NOT NULL AUTO_INCREMENT,
    `CASNumber` varchar(32) NOT NULL,
    `SourceID` int NOT NULL,
    `ChemicalName` longtext NOT NULL,
    `CID` int NOT NULL,
    `IsHealthHazard` bit NOT NULL,
    `IsIrritant` bit NOT NULL,
    `IsAccuteToxicity` bit NOT NULL,
    `IsCorrosive` bit NOT NULL,
    `IsEnvironmental` bit NOT NULL,
    `IsExplosive` bit NOT NULL,
    `IsFlamable` bit NOT NULL,
    `IsOxidizer` bit NOT NULL,
    `IsCompressedGas` bit NOT NULL,
    `Pictograms` varchar(1024) NOT NULL,
    `Signal` varchar(32) NOT NULL,
    `HazardStatements` longtext NULL,
    `PrecautionaryCodes` longtext NULL,
    CONSTRAINT `PK_GHSClassifications` PRIMARY KEY (`LCSSInformationID`)
);

CREATE TABLE `HazardCodes` (
    `HazardCodeID` int NOT NULL AUTO_INCREMENT,
    `GHSCode` varchar(16) NOT NULL,
    `CASNumber` varchar(32) NOT NULL,
    `HazardClass` varchar(128) NOT NULL,
    CONSTRAINT `PK_HazardCodes` PRIMARY KEY (`HazardCodeID`)
);

CREATE TABLE `InventoryStatusNames` (
    `InventoryStatusID` int NOT NULL,
    `Name` varchar(256) NOT NULL,
    CONSTRAINT `PK_InventoryStatusNames` PRIMARY KEY (`InventoryStatusID`)
);

CREATE TABLE `LocationLevelNames` (
    `LocationLevel` int NOT NULL,
    `Name` varchar(16) NOT NULL,
    CONSTRAINT `PK_LocationLevelNames` PRIMARY KEY (`LocationLevel`)
);

CREATE TABLE `LocationTypes` (
    `LocationTypeID` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(255) NOT NULL,
    `ValidChildren` varchar(255) NOT NULL,
    CONSTRAINT `PK_LocationTypes` PRIMARY KEY (`LocationTypeID`)
);

CREATE TABLE `LogEntries` (
    `LogEntryID` int NOT NULL AUTO_INCREMENT,
    `EntryDateTime` datetime(6) NOT NULL,
    `MessageLevel` int NOT NULL,
    `Login` varchar(256) NOT NULL,
    `Category` varchar(32) NOT NULL,
    `Text` varchar(1024) NOT NULL,
    CONSTRAINT `PK_LogEntries` PRIMARY KEY (`LogEntryID`)
);

CREATE TABLE `Owners` (
    `OwnerID` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(64) NOT NULL,
    CONSTRAINT `PK_Owners` PRIMARY KEY (`OwnerID`)
);

CREATE TABLE `RemovedItems` (
    `RemovedItemID` int NOT NULL AUTO_INCREMENT,
    `InventoryID` int NOT NULL,
    `Barcode` varchar(64) NOT NULL,
    `ChemicalName` varchar(256) NULL,
    `CASNumber` varchar(32) NULL,
    `LocationID` int NOT NULL,
    `GroupID` int NULL,
    `OwnerID` int NULL,
    `DateIn` datetime(6) NULL,
    `ExpirationDate` datetime(6) NULL,
    `ContainerSize` double NULL,
    `RemainingQuantity` double NULL,
    `Units` varchar(64) NULL,
    `State` varchar(64) NULL,
    `Flags` varchar(16) NULL,
    `RemovalReason` int NOT NULL,
    `DateRemoved` datetime(6) NOT NULL,
    CONSTRAINT `PK_RemovedItems` PRIMARY KEY (`RemovedItemID`)
);

CREATE TABLE `Settings` (
    `SettingID` int NOT NULL AUTO_INCREMENT,
    `SettingKey` varchar(64) NOT NULL,
    `SettingValue` longtext NOT NULL,
    CONSTRAINT `PK_Settings` PRIMARY KEY (`SettingID`)
);

CREATE TABLE `StorageGroups` (
    `GroupID` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(64) NOT NULL,
    CONSTRAINT `PK_StorageGroups` PRIMARY KEY (`GroupID`)
);

CREATE TABLE `ReportDefinitions` (
    `ReportID` int NOT NULL AUTO_INCREMENT,
    `ReportName` varchar(256) NOT NULL,
    `Description` varchar(1024) NOT NULL,
    `DatabaseQueryID` int NOT NULL,
    `WhereClause` longtext NULL,
    `Roles` varchar(64) NOT NULL,
    `Widgets` varchar(64) NOT NULL,
    `ColumnDefinitions` longtext NULL,
    CONSTRAINT `PK_ReportDefinitions` PRIMARY KEY (`ReportID`),
    CONSTRAINT `FK_ReportDefinitions_DatabaseQueries_DatabaseQueryID` FOREIGN KEY (`DatabaseQueryID`) REFERENCES `DatabaseQueries` (`DatabaseQueryID`) ON DELETE CASCADE
);

CREATE TABLE `StorageLocations` (
    `LocationID` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(64) NOT NULL,
    `ParentID` int NOT NULL,
    `LocationTypeID` int NOT NULL,
    `IsLeaf` bit NOT NULL,
    `LocationLevel` int NOT NULL,
    `Path` longtext NULL,
    CONSTRAINT `PK_StorageLocations` PRIMARY KEY (`LocationID`),
    CONSTRAINT `FK_StorageLocations_LocationTypes_LocationTypeID` FOREIGN KEY (`LocationTypeID`) REFERENCES `LocationTypes` (`LocationTypeID`) ON DELETE CASCADE
);

CREATE TABLE `InventoryItems` (
    `InventoryID` int NOT NULL AUTO_INCREMENT,
    `Barcode` varchar(64) NOT NULL,
    `CASNumber` varchar(32) NULL,
    `ChemicalName` varchar(256) NULL,
    `LocationID` int NOT NULL,
    `GroupID` int NULL,
    `OwnerID` int NULL,
    `DateIn` datetime(6) NULL,
    `ExpirationDate` datetime(6) NULL,
    `ContainerSize` double NULL,
    `RemainingQuantity` double NULL,
    `Units` varchar(64) NULL,
    `State` varchar(64) NULL,
    `Flags` varchar(16) NULL,
    `IsOtherCOC` bit NOT NULL DEFAULT FALSE,
    `InventoryStatusID` int NULL,
    `LastInventoryDate` datetime(6) NULL,
    `SDS` varchar(32) NULL,
    `Notes` longtext NULL,
    `StockCheckPreviousLocation` int NULL,
    `StockCheckTime` datetime(6) NULL,
    `StockCheckUser` varchar(64) NULL,
    `MaterialType` int NOT NULL DEFAULT 0,
    `DisposeFlag` bit NOT NULL,
    `Custom1` varchar(256) NULL,
    `Custom2` varchar(256) NULL,
    `Custom3` varchar(256) NULL,
    CONSTRAINT `PK_InventoryItems` PRIMARY KEY (`InventoryID`),
    CONSTRAINT `FK_InventoryItems_StorageGroups_GroupID` FOREIGN KEY (`GroupID`) REFERENCES `StorageGroups` (`GroupID`) ON DELETE NO ACTION,
    CONSTRAINT `FK_InventoryItems_InventoryStatusNames_InventoryStatusID` FOREIGN KEY (`InventoryStatusID`) REFERENCES `InventoryStatusNames` (`InventoryStatusID`) ON DELETE NO ACTION,
    CONSTRAINT `FK_InventoryItems_StorageLocations_LocationID` FOREIGN KEY (`LocationID`) REFERENCES `StorageLocations` (`LocationID`) ON DELETE CASCADE,
    CONSTRAINT `FK_InventoryItems_Owners_OwnerID` FOREIGN KEY (`OwnerID`) REFERENCES `Owners` (`OwnerID`) ON DELETE NO ACTION
);

CREATE TABLE `InventoryAudits` (
    `InventoryAuditID` int NOT NULL AUTO_INCREMENT,
    `AuditTime` datetime(6) NOT NULL,
    `Barcode` varchar(64) NOT NULL,
    `InventoryID` int NOT NULL,
    `LocationID` int NULL,
    `PreviousLocationID` int NULL,
    `User` varchar(255) NOT NULL,
    CONSTRAINT `PK_InventoryAudits` PRIMARY KEY (`InventoryAuditID`),
    CONSTRAINT `FK_InventoryAudits_InventoryItems_InventoryID` FOREIGN KEY (`InventoryID`) REFERENCES `InventoryItems` (`InventoryID`) ON DELETE CASCADE
);

CREATE INDEX `IX_Attachments_Login` ON `Attachments` (`Login`);

CREATE INDEX `IX_CASDataItems_CASNumber` ON `CASDataItems` (`CASNumber`);

CREATE INDEX `IX_CASDisposalProcedures_CASNumber` ON `CASDisposalProcedures` (`CASNumber`);

CREATE INDEX `IX_ChemicalsOfConcern_CASNumber` ON `ChemicalsOfConcern` (`CASNumber`);

CREATE INDEX `IX_HazardCodes_CASNumber` ON `HazardCodes` (`CASNumber`);

CREATE INDEX `IX_HazardCodes_GHSCode` ON `HazardCodes` (`GHSCode`);

CREATE INDEX `IX_InventoryAudits_InventoryID` ON `InventoryAudits` (`InventoryID`);

CREATE INDEX `IX_InventoryItems_GroupID` ON `InventoryItems` (`GroupID`);

CREATE INDEX `IX_InventoryItems_InventoryStatusID` ON `InventoryItems` (`InventoryStatusID`);

CREATE INDEX `IX_InventoryItems_LocationID` ON `InventoryItems` (`LocationID`);

CREATE INDEX `IX_InventoryItems_OwnerID` ON `InventoryItems` (`OwnerID`);

CREATE INDEX `IX_ReportDefinitions_DatabaseQueryID` ON `ReportDefinitions` (`DatabaseQueryID`);

CREATE INDEX `IX_StorageLocations_LocationTypeID` ON `StorageLocations` (`LocationTypeID`);

CREATE INDEX `IX_StorageLocations_ParentID` ON `StorageLocations` (`ParentID`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20211119195643_01_InitialMigration', '2.1.4-rtm-31024');

