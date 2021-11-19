export enum EFLAG {
    CWC = 0,
    THEFT,
    OTHERSECURITY,
    CARCINOGEN,
    HEALTHHAZARD,
    IRRITANT,
    ACUTETOXICITY,
    CORROSIVE,
    EXPLOSIVE,
    FLAMABLE,
    OXIDIZER,
    COMPRESSEDGAS,
    ENVIRONMENT,
    OTHERHAZARD,

    NUMFLAGS
}

export interface IInventoryItemFlags {
    Values: string[];
}

export class InventoryItemFlags implements IInventoryItemFlags {
    Values: string[];

    constructor(flags?: IInventoryItemFlags) {
        this.Values = [];
        for (let i = 0; i < EFLAG.NUMFLAGS; ++i) this.Values.push(flags ? flags.Values[i] : "");
    }

    get CWC(): string {
        return this.Values[EFLAG.CWC];
    }
    get THEFT(): string {
        return this.Values[EFLAG.THEFT];
    }
    get OTHERSECURITY(): string {
        return this.Values[EFLAG.OTHERSECURITY];
    }
    get CARCINOGEN(): string {
        return this.Values[EFLAG.CARCINOGEN];
    }
    get HEALTHHAZARD(): string {
        return this.Values[EFLAG.HEALTHHAZARD];
    }
    get IRRITANT(): string {
        return this.Values[EFLAG.IRRITANT];
    }
    get ACUTETOXICITY(): string {
        return this.Values[EFLAG.ACUTETOXICITY];
    }
    get CORROSIVE(): string {
        return this.Values[EFLAG.CORROSIVE];
    }
    get EXPLOSIVE(): string {
        return this.Values[EFLAG.EXPLOSIVE];
    }
    get FLAMABLE(): string {
        return this.Values[EFLAG.FLAMABLE];
    }
    get OXIDIZER(): string {
        return this.Values[EFLAG.OXIDIZER];
    }
    get COMPRESSEDGAS(): string {
        return this.Values[EFLAG.COMPRESSEDGAS];
    }
    get ENVIRONMENT(): string {
        return this.Values[EFLAG.ENVIRONMENT];
    }
    get OTHERHAZARD(): string {
        return this.Values[EFLAG.OTHERHAZARD];
    }

    set CWC(s: string) {
        this.Values[EFLAG.CWC] = s;
    }
    set THEFT(s: string) {
        this.Values[EFLAG.THEFT] = s;
    }
    set OTHERSECURITY(s: string) {
        this.Values[EFLAG.OTHERSECURITY] = s;
    }
    set CARCINOGEN(s: string) {
        this.Values[EFLAG.CARCINOGEN] = s;
    }
    set HEALTHHAZARD(s: string) {
        this.Values[EFLAG.HEALTHHAZARD] = s;
    }
    set IRRITANT(s: string) {
        this.Values[EFLAG.IRRITANT] = s;
    }
    set ACUTETOXICITY(s: string) {
        this.Values[EFLAG.ACUTETOXICITY] = s;
    }
    set CORROSIVE(s: string) {
        this.Values[EFLAG.CORROSIVE] = s;
    }
    set EXPLOSIVE(s: string) {
        this.Values[EFLAG.EXPLOSIVE] = s;
    }
    set FLAMABLE(s: string) {
        this.Values[EFLAG.FLAMABLE] = s;
    }
    set OXIDIZER(s: string) {
        this.Values[EFLAG.OXIDIZER] = s;
    }
    set COMPRESSEDGAS(s: string) {
        this.Values[EFLAG.COMPRESSEDGAS] = s;
    }
    set ENVIRONMENT(s: string) {
        this.Values[EFLAG.ENVIRONMENT] = s;
    }
    set OTHERHAZARD(s: string) {
        this.Values[EFLAG.OTHERHAZARD] = s;
    }
}
