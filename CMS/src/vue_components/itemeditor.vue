<template>
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
                            <v-text-field background-color="#8F8" v-model="itemdata.ChemicalName" label="Name" v-bind:readonly="readonly"></v-text-field>
                        </td>
                        <td>
                            <v-text-field background-color="#8F8" v-model="itemdata.Barcode" label="BARCODE" v-bind:disabled="readonly"></v-text-field>
                        </td>
                        <td>
                            <v-text-field background-color="#8F8" v-model="itemdata.CASNumber" label="CAS #" v-bind:readonly="readonly" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <v-text-field v-model="itemdata.SDS" label="SDS" v-bind:disabled="readonly" />
                        </td>
                        <td>
                            <v-select :items="owners" item-text="Name" item-value="OwnerID" label="Owner" v-model="itemdata.OwnerID" v-bind:disabled="readonly"></v-select>
                        </td>
                        <td>
                            <v-select :items="groups" item-text="Name" item-value="GroupID" label="Storage Group" v-model="itemdata.GroupID" v-bind:disabled="readonly"></v-select>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <v-text-field type="date" label="DateIn" v-model="itemdata.DateIn" v-bind:readonly="readonly"></v-text-field>
                        </td>
                        <td>
                            <v-text-field type="date" label="Expiration" v-model="itemdata.ExpirationDate" v-bind:readonly="readonly"></v-text-field>
                        </td>
                        <td>
                            <v-select :items="states" label="State" v-model="itemdata.State" v-bind:disabled="readonly"></v-select>
                        </td>
                    </tr>

                    <tr>
                        <td>
                            <v-text-field type="number" label="Container Size" v-model="itemdata.ContainerSize" placeholder="Size" v-bind:readonly="readonly" />
                        </td>
                        <td>
                            <v-text-field type="number" v-model="itemdata.RemainingQuantity" label="Remaining" v-bind:readonly="readonly"></v-text-field>
                        </td>
                        <td>
                            <v-select :items="units" label="Units" v-model="itemdata.Units" v-bind:disabled="readonly"></v-select>
                        </td>
                    </tr>
                </table>
            </v-layout>
            <v-layout>
                <v-flex>
                    <div>Notes</div>
                    <textarea label="Notes" style="width: 100%; border: 1px solid gray;" v-model="itemdata.Notes" v-bind:readonly="readonly">Notes</textarea>
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
                                <input type="checkbox" v-on:changed="on_modified" class="browser-default" :disabled="readonly || flag.IsReadonly" v-model="flag.Value" />
                                <span class="attr">{{ flag.Label }}</span>
                            </label>
                        </td>
                    </tr>
                </tbody>
            </table>
        </v-card-text>
        <v-card-actions>
            <v-btn small color="green white--text" v-on:click="on_accept()">Update</v-btn>
            <v-btn small color="red white--text" v-on:click="on_decline()">Cancel</v-btn>
        </v-card-actions>
        <locationpicker ref="locationpicker"></locationpicker>
    </v-card>
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
    { name: "OTHERHAZARD", position: 13 }
];

function ItemFlag(label, flagname, readonly) {
    this.Label = label;
    this.FlagName = flagname;
    this.Value = false;
    this.IsReadonly = readonly == true;
}

ItemFlag.prototype.setValue = function(flags) {
    // the ghs flags are characters, the coc flags are booleans
    if (flags[this.FlagName]) {
        let value = flags[this.FlagName];
        if (typeof value != "boolean") value = value == "X";
        console.log("ItemFlag.setValue - " + this.FlagName + " <- " + value);
        this.Value = value;
    } else console.log("ItemFlag.setValue - no value for " + this.FlagName);
};

ItemFlag.prototype.resetValue = function() {
    this.Value = false;
};

const mymodule = {
    // reactive data
    props: {
        groups: {},
        owners: {},
        width: {
            type: Number,
            default: 1100
        },
        readonly: {
            type: Boolean,
            default: false
        }
    },

    components: {
        locationpicker: VueComponents.LocationPicker
    },
    data: function() {
        return {
            itemdata: {},
            item_dialog_active: false,
            min_height: "660px",
            min_width: "1200px",
            header: "Edit Item",
            root_location_id: 0,
            modified: false,

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
                [new ItemFlag("OTHER", "OTHERSECURITY"), undefined, undefined]
            ],

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
                { value: " ", text: "(blank)" }
            ],
            states: [
                { value: "solid", text: "solid" },
                { value: "liquid", text: "liquid" },
                { value: "gas", text: "gas" },
                { value: "other", text: "other" }
            ]
        };
    },

    mounted: function() {
        console.log("In itemeditor.mounted. Readonly is " + this.readonly);
        let self = this;
        let url = utils.api_url("gethazardtables");
        api.axios_get({
            url: url,
            verbose: true,
            onsuccess: function(ajax_result) {
                self.hazard_tables = ajax_result.Data.Hazards;
                console.log("ItemDialog - Hazards:", self.hazard_tables);
                self.$emit("ready");
            }
        });
        //this.prepare_itemdata(this.itemdata);
        //this.initialize_itemdata(this.itemdata);
    },

    // created function is called after reactive hooks are added
    created: function() {},

    methods: {
        initialize: function(itemdata) {
            console.log("In itemeditor.initialize", itemdata);
            window.VueDialogItemData = itemdata;
            this.initialize_itemdata(itemdata);
        },

        //--------------------------------------------------------------
        //
        // Function:        initialize_itemdata
        // Author:          Pete Humphrey
        //
        // Initialize additional fields used for Vue bindings.
        //
        // param: item      the inventory item being edited
        // return:          none
        //
        //--------------------------------------------------------------
        initialize_itemdata: function(item) {
            console.log("### In itemdialog.initialize_itemdata: ", item);
            let itemdata = utils.deep_copy(item);
            itemdata["_flagobjects"] = this.itemdata_flags;
            console.log("###     updated itemdata: ", itemdata);
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
            console.log("### In itemdialog.prepare_itemdata: ", itemdata);
            this.itemdata = itemdata;
            let coc_flags = this.hazard_tables[itemdata.CASNumber];
            if (this.use_new_hazard_flags) {
                if (coc_flags) {
                    console.log("### setting flags:", coc_flags);
                    itemdata._flagobjects.forEach(function(row) {
                        row.forEach(function(flag) {
                            if (flag) flag.setValue(coc_flags);
                        });
                    });
                } else {
                    console.log("############## no hazard flags for " + itemdata.CASNumber);
                    itemdata._flagobjects.forEach(function(row) {
                        row.forEach(function(flag) {
                            if (flag) flag.resetValue();
                        });
                    });
                }
            } else {
                // initialize the (old) GHS flags and COC flags
                console.log("### ItemFlags: ", itemdata.ItemFlags);
                console.log("### COCFlags: ", coc_flags);
                itemdata._flagobjects.forEach(function(row) {
                    row.forEach(function(flag) {
                        if (flag) {
                            flag.setValue(itemdata.ItemFlags);
                            if (coc_flags) flag.setValue(coc_flags);
                        }
                    });
                });
            }
            console.log("### Flag Objects:");
            itemdata._flagobjects.forEach(function(row) {
                row.forEach(function(flag) {
                    if (flag) console.log("    " + flag.Label + " (" + flag.FlagName + ") = " + flag.Value);
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
            console.log("### set_item_flags: ", itemdata, itemdata._flagobjects);

            let itemflags = itemdata.ItemFlags;
            itemdata._flagobjects.forEach(function(row) {
                row.forEach(function(flag) {
                    if (flag) {
                        let name = flag.FlagName;
                        let oldvalue = itemflags[name];
                        let newvalue = flag.Value ? "X" : " ";
                        console.log("    " + name + ": " + oldvalue + " -> " + newvalue);
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
            console.log('### Flags: "' + itemdata.Flags + '"');
            this.show_flags("After set_item_flags", itemdata.ItemFlags);
        },

        on_accept: function() {
            // set itemdata.ItemFlags based on local boolean values
            // and then sets itemdata.Flags from that
            this.set_item_flags(this.itemdata);
            this.show_flags("on_accept - after", this.itemdata.ItemFlags);
            //console.log("itemeditor.vue itemdata", itemdata)

            this.item_dialog_active = false;
            delete this.itemdata._flagobjects;
            let json = JSON.stringify(this.itemdata, null, 4);
            console.log(json);
            let item_copy = JSON.parse(json);

            // reset all the ItemFlags.Values = ' ';
            let flagslen = item_copy.ItemFlags.Values.length;
            for (let i = 0; i < flagslen; i++) {
                item_copy.ItemFlags.Values[i] = " ";
            }
            console.log("    parsed1:", item_copy.ItemFlags);

            item_copy.IsChanged = this.modified;
            console.log("    parsed2:", item_copy.ItemFlags);

            //console.log("accept", itemdata.ItemFlags, this.flags);
            //this.show_flags("Output flags", itemdata.ItemFlags);
            console.log("    parsed3:", item_copy.ItemFlags);
            console.log("Returning", item_copy);

            console.log("    parsed4:", item_copy.ItemFlags);
            this.$emit("save", item_copy);
        },

        on_decline: function() {
            this.$emit("cancel");
        },

        on_modified: function() {
            console.log("### on_modified");
            this.modified = true;
        },

        open_location_picker: function() {
            console.log("Opening Location Picker");
            let self = this;
            let settings = { root_location_id: this.root_location_id, depth: 9 };
            this.$refs["locationpicker"].open(settings, function(sel) {
                console.log("In locationpicker callback:", sel);
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
            //console.log("is_x: " + val + ' => ' + result);
            return result;
        },

        to_x: function(val) {
            let result = " ";
            if (val) result = "X";
            //console.log("to_x: " + val + ' => "' + result + '"');
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
        }
    }
};
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["ItemEditor"] = mymodule;
else window.VueComponents = { ItemEditor: mymodule };
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
