import { IInventoryItemFlags } from "./itemflags";

export enum EMaterialType {
    CHEMICAL = 0,
    BIOLOGICAL,
    RADIOLOGICAL,
    OTHER = 99
}

export interface IInventoryItem {
    InventoryID: number;
    Barcode: string;
    CASNumber: string;
    ChemicalName: string;
    LocationID: number;
    SiteID: number;
    GroupID?: number;
    OwnerID?: number;
    DateIn?: string;
    ExpirationDate?: string;
    ContainerSize?: number;
    RemainingQuantity?: number;
    Units: string;
    State: string;
    Flags: string;
    SDS: string;
    Notes: string;
    Path: string;

    //----------------------------------------------------------------
    //
    // New items from feature requests
    //
    //----------------------------------------------------------------

    MaterialType: EMaterialType;
    Custom1: string;
    Custom2: string;
    Custom3: string;
}

export class InventoryItem {
    InventoryID: number;
    Barcode: string;
    CASNumber: string;
    ChemicalName: string;
    LocationID: number;
    SiteID: number;
    GroupID?: number;
    OwnerID?: number;
    DateIn?: Date;
    ExpirationDate?: Date;
    ContainerSize?: number;
    RemainingQuantity?: number;
    Units: string;
    State: string;
    Flags: string;
    SDS: string;
    Notes: string;
    Path: string;

    //----------------------------------------------------------------
    //
    // New items from feature requests
    //
    //----------------------------------------------------------------

    MaterialType: EMaterialType;
    Custom1: string;
    Custom2: string;
    Custom3: string;

    constructor(i: IInventoryItem) {
        this.InventoryID = i.InventoryID;
        this.Barcode = i.Barcode;
        this.CASNumber = i.CASNumber;
        this.ChemicalName = i.ChemicalName;
        this.LocationID = i.LocationID;
        this.SiteID = i.SiteID;
        this.GroupID = i.GroupID;
        this.OwnerID = i.OwnerID;
        if (i.DateIn) this.DateIn = new Date(i.DateIn);
        if (i.ExpirationDate) this.ExpirationDate = new Date(i.ExpirationDate);
        this.ContainerSize = i.ContainerSize;
        this.RemainingQuantity = i.RemainingQuantity;
        this.Units = i.Units;
        this.State = i.State;
        this.Flags = i.Flags;
        this.SDS = i.SDS;
        this.Notes = i.Notes;

        //----------------------------------------------------------------
        //
        // New items from feature requests
        //
        //----------------------------------------------------------------

        this.MaterialType = i.MaterialType;
        this.Custom1 = i.Custom1;
        this.Custom2 = i.Custom2;
        this.Custom3 = i.Custom3;
    }
}
