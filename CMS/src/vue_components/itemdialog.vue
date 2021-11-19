<template>
    <div class="text-xs-center">
        <v-dialog persistent v-model="item_dialog_active" :width="width">
            <v-card class="noborder">
                <v-card-title class="headline grey lighten-2 " primary-title>
                    {{ header }}
                </v-card-title>
                <v-card-text>
                    <!-- ------------------------------------------------------ -->
                    <!-- Item Properties                                        -->
                    <!-- ------------------------------------------------------ -->
                    <v-layout class="mb-4">
                        <v-btn small icon color="green" class="white--text" v-on:click="open_location_picker()"><v-icon small>create</v-icon></v-btn>
                        <div style="margin-top: 8px; font-size: larger; font-weight: bold;">
                            <span v-if="!itemdata.FullLocation" class="red--text">Click to select location</span>
                            <span>{{ itemdata.FullLocation }}</span>
                        </div>
                        <v-spacer></v-spacer>
                        <div style="margin-top: 8px; font-size: larger; font-weight: bold;">Items highlighted in green are required.</div>
                    </v-layout>
                    <v-layout>
                        <table class="plain">
                            <tr>
                                <td>
                                    <v-text-field background-color="#8F8" v-model="itemdata.ChemicalName" label="Name" v-bind:readonly="readonly" @input="on_modified"></v-text-field>
                                </td>
                                <td>
                                    <v-text-field background-color="#8F8" v-model="itemdata.Barcode" label="BARCODE" v-bind:disabled="readonly" @input="on_modified"></v-text-field>
                                </td>
                                <td>
                                    <v-text-field background-color="#8F8" v-model="itemdata.CASNumber" label="CAS #" v-bind:readonly="readonly" @input="on_cas_input" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <v-text-field v-model="itemdata.SDS" label="SDS" v-bind:disabled="readonly"  @input="on_modified"/>
                                </td>
                                <td>
                                    <v-select :items="owners" item-text="Name" item-value="OwnerID" label="Owner" v-model="itemdata.OwnerID" v-bind:disabled="readonly"  @change="on_modified"></v-select>
                                </td>
                                <td>
                                    <v-select :items="groups" item-text="Name" item-value="GroupID" label="Storage Group" v-model="itemdata.GroupID" v-bind:disabled="readonly" @change="on_modified"></v-select>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <v-text-field type="date" label="DateIn" v-model="itemdata.DateIn" v-bind:readonly="readonly" @input="on_modified"></v-text-field>
                                </td>
                                <td>
                                    <v-text-field type="date" label="Expiration" v-model="itemdata.ExpirationDate" v-bind:readonly="readonly" @input="on_modified"></v-text-field>
                                </td>
                                <td>
                                    <v-select :items="states" label="State" v-model="itemdata.State" v-bind:disabled="readonly" @input="on_modified"></v-select>
                                </td>
                            </tr>

                            <tr>
                                <td>
                                    <v-text-field type="number" label="Container Size" v-model="itemdata.ContainerSize" placeholder="Size" v-bind:readonly="readonly"  @input="on_modified"/>
                                </td>
                                <td>
                                    <v-text-field type="number" v-model="itemdata.RemainingQuantity" label="Remaining" v-bind:readonly="readonly" @input="on_modified"></v-text-field>
                                </td>
                                <td>
                                    <v-select :items="units" label="Units" v-model="itemdata.Units" v-bind:disabled="readonly" @change="on_modified"></v-select>
                                </td>
                            </tr>
                        </table>
                    </v-layout>
                    <v-layout>
                        <v-flex>
                            <div>Notes</div>
                            <textarea label="Notes" style="width: 100%; border: 1px solid gray;" v-model="itemdata.Notes" v-bind:readonly="readonly"  @input="on_modified">Notes</textarea>
                        </v-flex>
                    </v-layout>
                    <!-- ------------------------------------------------------ -->
                    <!-- Chemicals of Concern (COC) and Hazards (GHS)           -->
                    <!-- ------------------------------------------------------ -->
                    <table class="hazards">
                        <thead>
                            <tr>
                                <td class="red white--text" style="margin:0 5px 0 0; ">COC</td>
                                <td colspan="2" class="yellow black--text">GHS Hazard Symbol</td>
                            </tr>
                        </thead>
                        <tbody v-if="use_new_hazard_flags">
                            <tr v-for="row in itemdata._flagobjects">
                                <td v-for="flag in row">
                                    <div v-if="flag">
                                        <label>
                                            <input type="checkbox" class="browser-default" v-model="flag.Value" v-bind:disabled="flag.IsReadonly" />
                                            <span class="black--text">{{ flag.Label }}</span>
                                        </label>
                                    </div>
                                    <div v-else>
                                        &nbsp;
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                        <tbody v-if="!use_new_hazard_flags">
                            <tr v-for="(row, i) in itemdata_flags" :key="i">
                                <td v-for="(flag, j) in row" :key="j">
                                    <label v-if="flag">
                                        <input type="checkbox" v-on:changed="on_modified" class="browser-default" :disabled="readonly || flag.IsReadonly" v-model="flag.Value" @input="on_hazard_flag_input"/>
                                        <span class="attr">{{ flag.Label }}</span>
                                        <!--
                        <v-checkbox dense :label="flag.Label" v-model="flag.Value" style='margin-top:0; margin-bottom:0; padding: 0;'></v-checkbox>
                        -->
                                    </label>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <div class='title red--text' style='height: 1.5rem;'>
                        {{checkmark_message}}
                    </div>
                </v-card-text>
                <v-card-actions>
                    <v-btn small color="green white--text" v-on:click="on_accept()">{{accept_button_text}}</v-btn>
                    <v-btn small color="red white--text" v-on:click="on_decline()">Cancel</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
        <locationpicker ref="locationpicker"></locationpicker>
        <infodialog ref="infodialog" width="400px"></infodialog>
    </div>
</template>

<script>
//-------------------------------------------------------------------------
//
// COC and Hazard (GHS) flag processing
//
// COC flags are readonly and come from an API call to gethazardtables.
// The API call is made in the "created" lifecycle method and returns
// a dictionary that maps CAS numbers to a dictionaries mapping COC
// flag names to boolean values. This dictionary of dictionaries is
// saved in hazard_tables.
//
// If use_new_hazard_flags is true, we expect GHS flags to be returned
// from the API too.  Othewise we get those values by looking at the
// ItemFlags field of the inventory item being edited.  ItemFlags is
// a dictionary of GHS names to chars, 'X' or ' '.  This comes from the
// old way of recording hazards on an item by item basis.  If and when
// we get a way of mapping CAS numbers to LCSS pictograms this may change
// and these flags will be global and readonly.
//
// One COC flag is not readonly and is stored in the inventory item as
// 'OTHERSECURITY'.
//
// A local class, ItemFlag, is used to hold these flags, their names, and
// their values.
//
// GHS Flags
//     OTHERSECURITY
//     HEALTHHAZARD
//     IRRITANT
//     ACCUTETOXICITY
//     CORROSIVE
//     EXPLOSIVE
//     FLAMABLE
//     OXIDIZER
//     COMPRESSEDGAS
//     ENVIRONMENT
//
// COC Flags
//     COC_CWC
//     COC_EFATS
//     COC_EU
//     COC_AG
//     COC_WMD
//
//-------------------------------------------------------------------------

console.log("Loading itemdialog.vue");
console.log("VueComponents:", VueComponents);

const flagnames = [
    { name: "CWC", position: 0 },
    { name: "THEFT", position: 1 },
    { name: "OTHERSECURITY", position: 2 },
    { name: "CARCINOGEN", position: 3 },
    { name: "HEALTHHAZARD", position: 4 },
    { name: "IRRITANT", position: 5 },
    { name: "ACUTETOXICITY", position: 6 },
    { name: "CORROSIVE", position: 7 },
    { name: "EXPLOSIVE", position: 8 },
    { name: "FLAMABLE", position: 9 },
    { name: "OXIDIZER", position: 10 },
    { name: "COMPRESSEDGAS", position: 11 },
    { name: "ENVIRONMENT", position: 12 },
    { name: "OTHERHAZARD", position: 13 },
];

function ItemFlag(label, flagname, readonly) {
    this.Label = label;
    this.FlagName = flagname;
    this.Value = false;
    this.IsReadonly = readonly == true;
    this.Debug = false;
}

ItemFlag.prototype.setValue = function(flags) {
    // the ghs flags are characters, the coc flags are booleans
    if (this.Debug) console.log("ItemFlag.setValue", this.FlagName);
    if (flags[this.FlagName]) {
        let value = flags[this.FlagName];
        if (this.Debug) console.log("    value: " + value);
        if (typeof value != "boolean") value = (value == "X" || value == 'x');
        // console.log("ItemFlag.setValue - " + this.FlagName + " <- " + value);
        this.Value = value;
    } else if (this.Debug) console.log("    ItemFlag.setValue - no value for " + this.FlagName);
};

ItemFlag.prototype.resetValue = function() {
    this.Value = false;
};

function create_itemflags() {
    let result = [
        // row 0
        [new ItemFlag("CWC", "COC_CWC", true), new ItemFlag("Health Hazard", "HEALTHHAZARD"), new ItemFlag("Exploding Bomb", "EXPLOSIVE")],
        // row 1
        [new ItemFlag("CFATS", "COC_CFATS", true), new ItemFlag("Exclamation Mark", "IRRITANT"), new ItemFlag("Flame", "FLAMABLE")],
        // row 2
        [new ItemFlag("EU", "COC_EU", true), new ItemFlag("Skull and Crossbones", "ACUTETOXICITY"), new ItemFlag("Flame Over Circle", "OXIDIZER")],
        // row 3
        [new ItemFlag("AG", "COC_AG", true), new ItemFlag("Corrosion", "CORROSIVE"), new ItemFlag("Gas Cylinder", "COMPRESSEDGAS")],
        // row 4
        [new ItemFlag("WMD", "COC_WMD", true), new ItemFlag("Environment", "ENVIRONMENT"), undefined],
        // row 5
        [new ItemFlag("OTHER", "OTHERSECURITY"), undefined, undefined],
    ];
    return result;
}

function is_empty(ref) {
    return ref == undefined || ref.length == 0;
}

const mymodule = {
    // reactive data
    props: {
        groups: {},
        owners: {},
        debug: {
            type: Boolean,
            default: false,
        },
        width: {
            type: Number,
            default: 1100,
        },
    },

    components: {
        locationpicker: VueComponents.LocationPicker,
        infodialog: VueComponents.InfoDialog,
    },

    data: function() {
        return {
            item_dialog_active: false,
            min_height: "660px",
            min_width: "1200px",
            header: "Edit Item",
            root_location_id: 0,
            readonly: true,
            modified: false,
            checkmark_message: "",
            accept_button_text: "Update",

            use_new_hazard_flags: false,

            // new hazard flags
            hazard_tables: undefined,
            itemdata_flags: [
                // row 0
                [new ItemFlag("CWC", "COC_CWC", true), new ItemFlag("Health Hazard", "HEALTHHAZARD"), new ItemFlag("Exploding Bomb", "EXPLOSIVE")],
                // row 1
                [new ItemFlag("CFATS", "COC_CFATS", true), new ItemFlag("Exclamation Mark", "IRRITANT"), new ItemFlag("Flame", "FLAMABLE")],
                // row 2
                [new ItemFlag("EU", "COC_EU", true), new ItemFlag("Skull and Crossbones", "ACUTETOXICITY"), new ItemFlag("Flame Over Circle", "OXIDIZER")],
                // row 3
                [new ItemFlag("AG", "COC_AG", true), new ItemFlag("Corrosion", "CORROSIVE"), new ItemFlag("Gas Cylinder", "COMPRESSEDGAS")],
                // row 4
                [new ItemFlag("WMD", "COC_WMD", true), new ItemFlag("Environment", "ENVIRONMENT"), undefined],
                // row 5
                [new ItemFlag("OTHER", "OTHERSECURITY"), undefined, undefined],
            ],

            // local copy if the inventory item
            itemdata: {},

            // dropdown list values
            units: [
                { value: "cm3", text: "Cubic centimeters (cm^3)" },
                { value: "m3", text: "Cubic meters (m^3)" },
                { value: "g", text: "Gram (g)" },
                { value: "kg", text: "Kilogram (kg)" },
                { value: "L", text: "Liter (L)" },
                { value: "mg", text: "Milligram (mg)" },
                { value: "mL", text: "Milliliter (mL)" },
                { value: "ft3", text: "Cubic feet (ft^3)" },
                { value: " ", text: "(blank)" },
            ],
            states: [
                { value: "solid", text: "solid" },
                { value: "liquid", text: "liquid" },
                { value: "gas", text: "gas" },
                { value: "other", text: "other" },
            ],
        };
    },

    mounted: function() {
        if (this.debug) console.log("In itemeditor.mounted");
        ItemFlag.prototype.Debug = this.debug;
    },

    // created function is called after reactive hooks are added
    created: function() {
        if (this.debug) console.log("### In itemdialog.created: ", this.itemdata);
        window.VueDialogItemData = this.itemdata;
        this.itemdata["_flagobjects"] = this.itemdata_flags;
        this.callback = "undefined";
        let self = this;
        let url = utils.api_url("gethazardtables");
        api.axios_get({
            url: url,
            verbose: true,
            onsuccess: function(ajax_result) {
                self.hazard_tables = ajax_result.Data.Hazards;
                if (self.debug) console.log("ItemDialog - Hazards:", self.hazard_tables);
            },
        });
    },

    methods: {
        // open - open the dialog
        // config = {
        //    header: "Dialog Header",
        //    item: inventory item,
        //    readonly: true/false,
        // }
        open: function(config, callback) {
            this.modified = false;
            if (this.debug) console.log("### In itemdialog.open: ", config);
            let self = this;
            let header = config.header;
            let item = config.item;
            if (this.debug) console.log("Editing item", item);
            this.readonly = config.readonly;
            this.callback = callback;
            if (header) this.header = header;
            let dlg = $("#item-modal");
            if (config.root_location_id) this.root_location_id = config.root_location_id;
            else this.root_location_id = 1;
            if (this.debug) console.log("itemdialog root_location_id = " + this.root_location_id);
            this.initialize_itemdata(item);
            if (item.InventoryID == 0) this.accept_button_text = "Add New";
            else this.accept_button_text = "Update";
            this.item_dialog_active = true;
        },

        //--------------------------------------------------------------
        //
        // Function:        initialize_itemdata
        // Author:          Pete Humphrey
        //
        // Create a deep copy if the inventory item being edited
        // and initialize additional fields used for Vue bindings.
        //
        // param: item      the inventory item being edited
        // return:          none
        //
        //--------------------------------------------------------------
        initialize_itemdata: function(item) {
            if (this.debug) console.log("### In itemdialog.initialize_itemdata: ", item);
            let itemdata = utils.deep_copy(item);
            this.itemdata_flags = create_itemflags();
            itemdata["_flagobjects"] = this.itemdata_flags;
            if (this.debug) console.log("###     updated itemdata: ", itemdata);
            this.prepare_itemdata(itemdata);
            //this.flags.CWC = this.itemdata.ItemFlags.CWC;
            this.itemdata.DateIn = this.convert_date_string(item.DateIn);
            this.itemdata.ExpirationDate = this.convert_date_string(item.ExpirationDate);
        },

        //--------------------------------------------------------------
        //
        // Function:        prepare_itemdata
        // Author:          Pete Humphrey
        //
        // Initialize the FlagValue objects in an inventory item
        //
        // param: itemdata  a copy of the inventory item being edited
        // return:          none
        //
        //--------------------------------------------------------------
        prepare_itemdata: function(itemdata) {
            let self = this;
            if (this.debug) console.log("### In itemdialog.prepare_itemdata: ", itemdata);
            this.itemdata = itemdata;
            let coc_flags = this.hazard_tables[itemdata.CASNumber];
            if (this.use_new_hazard_flags) {
                if (coc_flags) {
                    if (this.debug) console.log("### setting flags:", coc_flags);
                    itemdata._flagobjects.forEach(function(row) {
                        row.forEach(function(flag) {
                            if (flag) flag.setValue(coc_flags);
                        });
                    });
                } else {
                    if (this.debug) console.log("############## no hazard flags for " + itemdata.CASNumber);
                    itemdata._flagobjects.forEach(function(row) {
                        row.forEach(function(flag) {
                            if (flag) flag.resetValue();
                        });
                    });
                }
            } else {
                // initialize the (old) GHS flags and COC flags
                if (this.debug) console.log("### ItemFlags: ", itemdata.ItemFlags);
                if (this.debug) console.log("### COCFlags: ", coc_flags);
                itemdata._flagobjects.forEach(function(row) {
                    row.forEach(function(flag) {
                        if (flag) {
                            flag.setValue(itemdata.ItemFlags);
                            if (coc_flags) flag.setValue(coc_flags);
                        }
                    });
                });
            }
            if (this.debug) console.log("### Flag Objects:");
            itemdata._flagobjects.forEach(function(row) {
                row.forEach(function(flag) {
                    if (flag) if (self.debug) console.log("    " + flag.Label + " (" + flag.FlagName + ") = " + flag.Value);
                });
            });
        },

        //--------------------------------------------------------------
        //
        // Function:        set_item_flags
        // Author:          Pete Humphrey
        //
        // Update the ItemFlags object in the local inventory copy
        //
        // param: itemdata  a copy of the inventory item being edited
        // return:          none
        //
        //--------------------------------------------------------------
        set_item_flags: function(itemdata) {
            let self = this;
            if (this.debug) console.log("### set_item_flags: ", itemdata, itemdata._flagobjects);

            let itemflags = itemdata.ItemFlags;
            itemdata._flagobjects.forEach(function(row) {
                row.forEach(function(flag) {
                    if (flag) {
                        let name = flag.FlagName;
                        let oldvalue = itemflags[name];
                        let newvalue = flag.Value ? "X" : " ";
                        if (self.debug) console.log("    " + name + ": " + oldvalue + " -> " + newvalue);
                        itemflags[name] = newvalue;
                    }
                });
            });
            let newflags = "";
            flagnames.forEach(function(flag) {
                let value = itemdata.ItemFlags[flag.name];
                if (value) newflags += value;
                else newflags += " ";
            });
            itemdata.Flags = newflags;
            if (this.debug) console.log('### Flags: "' + itemdata.Flags + '"');
            this.show_flags("After set_item_flags", itemdata.ItemFlags);
        },

        show_itemflags: function() {
            let flags = this.itemdata.ItemFlags;
            console.log("ItemFlags");
            let keys = Object.keys(flags).sort();
            keys.forEach(function(key) {
                console.log("    " + key + ": " + flags[key]);
            });
            console.log("_flagobjects");
            this.itemdata._flagobjects.forEach(function(row) {
                row.forEach(function(itemflag) {
                    if (itemflag) {
                        console.log("    " + itemflag.FlagName + ": (" + itemflag.Label + "}: " + itemflag.Value);
                    }
                });
            })
        },

        on_accept: function() {
            let itemdata = this.itemdata;
            let missing = [];
            if (is_empty(itemdata.Barcode)) missing.push("Barcode");
            if (is_empty(itemdata.ChemicalName)) missing.push("ChemicalName");
            if (is_empty(itemdata.CASNumber)) missing.push("CAS #");
            if (!itemdata.LocationID) missing.push("Location");
            if (itemdata.LocationID == 1) {
                let toploc = this.$refs.locationpicker.find_location(1);
                this.notify("Please select a location below " + toploc.Name);
                return;
            }
            if (missing.length > 0) {
                console.error("ItemData", itemdata);
                let mstr = missing.join(", ");
                this.notify("Please enter values for the following: " + mstr + ".");
                return;
            }
            if (this.debug) console.log("Closing itemeditor dialog", this.itemdata);
            this.show_flags("on_accept - before", this.itemdata.ItemFlags);
            // set itemdata.ItemFlags based on local boolean values
            // and then sets itemdata.Flags from that
            this.set_item_flags(this.itemdata);
            this.show_flags("on_accept - after", this.itemdata.ItemFlags);
            //if (this.debug) console.log("itemeditor.vue itemdata", itemdata)

            this.item_dialog_active = false;
            delete this.itemdata._flagobjects;
            let json = JSON.stringify(this.itemdata, null, 4);
            if (this.debug) console.log(json);
            let item_copy = JSON.parse(json);

            // reset all the ItemFlags.Values = ' ';
            item_copy.ItemFlags.Values = [" ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " "];
            if (this.debug) console.log("    parsed1:", item_copy.ItemFlags);

            item_copy.IsChanged = this.modified;
            if (this.debug) console.log("    parsed2:", item_copy.ItemFlags);

            //if (this.debug) console.log("accept", itemdata.ItemFlags, this.flags);
            //this.show_flags("Output flags", itemdata.ItemFlags);
            if (this.debug) console.log("    parsed3:", item_copy.ItemFlags);
            if (this.debug) console.log("Returning", item_copy);

            if (this.debug) console.log("    parsed4:", item_copy.ItemFlags);
            this.$emit("save", item_copy);
            if (this.callback) this.callback(item_copy);
        },

        on_decline: function() {
            if (this.debug) console.log("Closing itemeditor dialog");
            this.item_dialog_active = false;
            this.$emit("cancel");
        },

        on_modified: function() {
            if (this.debug) console.log("### on_modified");
            this.modified = true;
        },

        reset_hazard_flags: function(flagobjects) {
            console.error("Reset Hazard Flags");
            this.checkmark_message = '   ';
            flagobjects.forEach((row) => { 
                row.forEach((flag) => { 
                    if (flag) {
                        //console.log("    " + flag.FlagName + ": " + flag.Value + " => false"); 
                        flag.Value = false;
                    }
                }); 
            });
        },

        show_hazard_flags: function(show_all) {
            console.log("Hazard Flags");
            this.itemdata._flagobjects.forEach((row) => { 
                row.forEach((flag) => { 
                    if (flag && (show_all || flag.Value)) {
                        console.log("    " + flag.FlagName + " = " + flag.Value); 
                    }
                }); 
            });
        },

        flatten_flags: function(flagobjects) {
            let result = {}
            flagobjects.forEach((row) => { 
                row.forEach((flag) => { 
                    if (flag) {
                        result[flag.FlagName] = flag;
                    }
                }); 
            });
            return result;
        },

        on_hazard_flag_input: function() {
            console.error("on_hazard_flag_input");
            this.checkmark_message = ' ';
        },

        on_cas_input: function() {
            let item = this.itemdata;
            let casnumber = item.CASNumber;
            this.on_modified();
            //if (this.debug) console.log("on_cas_input: " + casnumber);
            if (item.InventoryID == 0) {
                if (casnumber.length == 0) {
                    // CAS # has become empty, uncheck all the flags
                    this.reset_hazard_flags(item._flagobjects);
                    return;
                }
                let self = this;
                if (casnumber.length > 0  && utils.is_cas_number(casnumber)) {
                    if (this.debug) console.log('Looking up "' + casnumber + '"');
                    get_hazard_information_for_casnumber(casnumber, function(result) {
                        if (self.debug) {
                            if (result) console.log("itemdialog.vue - have hazard results for " + casnumber, result);
                            else console.log("No hazard date found for " + casnumber);
                        }
                        if (result) {
                            self.reset_hazard_flags(item._flagobjects);
                            let flags = self.flatten_flags(item._flagobjects);
                            Object.keys(flags).forEach((key) => {
                                let hazard = key;
                                let flag = flags[key];
                                //console.log("    updating " + key);
                                // hazard information flags for non-COC start with "GHS_"
                                if (!key.startsWith("COC")) {
                                    hazard = "GHS_" + key;
                                    //console.log("        using result key " + hazard);
                                }
                                let newvalue = result[hazard];
                                if (newvalue && self.debug) console.log("    setting " + flag.FlagName + " to " + newvalue);
                                flag.Value = newvalue;
                            });
                            self.checkmark_message = "GHS pictograms as indicated in the Annex VI of the CLP Regulation (EU REGULATION (EC) No 1272/2008)";
                        }
                        else self.checkmark_message = ' ';
                    });
                }
                else self.checkmark_message = ' ';
            }
        },

        open_location_picker: function() {
            if (this.debug) console.log("Opening Location Picker");
            let self = this;
            let settings = { root_location_id: this.root_location_id, depth: 9 };
            this.$refs.locationpicker.open(settings, function(sel) {
                if (self.debug) console.log("In locationpicker callback:", sel);
                self.itemdata.FullLocation = sel.FullLocation;
                self.itemdata.LocationID = sel.LocationID;
                self.$forceUpdate();
            });
        },

        convert_date_string: function(datestr) {
            let result = "";
            if (datestr) {
                let m = datestr.match(/^(\d\d\d\d-\d\d-\d\d)/);
                if (m) result = m[1];
            }
            return result;
        },

        is_x: function(val) {
            let result = val == "x" || val == "X";
            //if (this.debug) console.log("is_x: " + val + ' => ' + result);
            return result;
        },

        to_x: function(val) {
            let result = " ";
            if (val) result = "X";
            //if (this.debug) console.log("to_x: " + val + ' => "' + result + '"');
            return val;
        },

        show_flags: function(description, itemflags) {
            let self = this;
            let flagnames = ["HEALTHHAZARD", "IRRITANT", "ACUTETOXICITY", "CORROSIVE", "EXPLOSIVE", "FLAMABLE", "OXIDIZER", "COMPRESSEDGAS", "ENVIRONMENT", "OTHERSECURITY"];
            console.log(description, itemflags);
            flagnames.forEach(function(name) {
                value = itemflags[name];
                console.log("    " + name + " = " + value);
            });
        },

        notify: function(text) {
            this.$refs.infodialog.open(text, "Item Editor");
        },
    },
};
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["ItemEditorDialog"] = mymodule;
else window.VueComponents = { ItemEditorDialog: mymodule };
</script>

<style scoped>
.itemdialog {
    width: 600px;
    /* min-height: 660px; */
}

.dialog-header {
    font-weight: bold;
}

.horizontal-label {
    display: inline;
}

.red-button {
    background-color: red;
}

.permission {
    margin-right: 2em;
    margin-top: 4px;
}

.attr {
    color: black;
}

table.plain {
    border: none;
}

table.plain tr {
    border: none;
    max-height: 1rem;
    margin: 0;
    padding: 0;
}

table.plain td {
    border: none;
    max-height: 1rem;
    margin: 0;
    padding-left: 4px;
    padding-right: 4px;
    width: 33%;
}

tr.attributes {
    width: 100%;
    padding: 4px;
    min-height: 2rem;
    max-height: 2rem;
    height: 2rem;
}

th.attribute {
    border: 1px solid black;
    padding: 4px;
    min-height: 2rem;
    max-height: 2rem;
    height: 2rem;
}

td.attribute {
    border: 1px solid black;
    padding: 4px;
    min-height: 2rem;
    max-height: 2rem;
    height: 2rem;
}

td.cwc {
    padding-left: 4px;
    padding-top: 4px;
    padding-bottom: 0px;
}

table.hazards {
    border: none;
    width: 100%;
}

table.hazards tr {
    border: none;
    padding: 4px;
    min-height: 2rem;
    max-height: 2rem;
    height: 2rem;
}

table.hazards td {
    border: none;
    padding: 4px;
    min-height: 2rem;
    max-height: 2rem;
    height: 2rem;
    width: 33%;
}
</style>
