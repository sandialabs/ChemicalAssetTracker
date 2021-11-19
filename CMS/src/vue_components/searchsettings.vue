<template>
    <div class="text-xs-center">
        <v-dialog v-model="location_picker_active" :width="width">
            <v-card @keyup.enter="on_accept()">
                <v-card-title class="headline grey lighten-2" primary-title>
                    Select Location and Search Values
                </v-card-title>
                <v-card-text style="text-align: left !important;">
                    <v-layout>
                        <div style="max-height: 500px; border: 1px solid black;">
                            <location :debug="true" ref="location" width="800px" v-on:select="on_location_selected"></location>
                        </div>
                    </v-layout>
                    <v-layout class="mt-4">
                        <v-flex>
                            <v-text-field hide-details label="Barcode" class="mr-3" outlined dense v-model="selected_barcode"></v-text-field>
                        </v-flex>
                        <v-flex>
                            <v-text-field hide-details label="CAS #" class="mr-3" outlined dense v-model="selected_casnumber"></v-text-field>
                        </v-flex>
                        <v-flex>
                            <v-text-field hide-details label="Chemical" class="mr-3" outlined dense v-model="selected_chemical"></v-text-field>
                        </v-flex>
                        <v-flex>
                            <v-text-field hide-details label="Owner" class="mr-3" outlined dense v-model="selected_owner"></v-text-field>
                        </v-flex>
                        <v-flex>
                            <v-text-field hide-details label="Max Rows" outlined type="number" dense v-model="batch_size"></v-text-field>
                        </v-flex>
                    </v-layout>
                    <v-layout v-if="!selected_location">
                        <p>
                            Please select a location by clicking on an item in a list.
                        </p>
                    </v-layout>
                </v-card-text>
                <v-card-actions>
                    <v-btn v-if="selected_location" small color="green" v-on:click="on_accept()">Select</v-btn>
                    <v-btn small color="red" v-on:click="on_decline()">Cancel</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>
</template>

<script>
//#################################################################
//
// searchesettings.vue
//
// Props:
//     debug (bool):     set to true to log debugging messages
//     height (number):  dialog height
//     width (number):   dialog width
//     barcode (bool):   show barcode text input
//     casnumber (bool): show CAS # text input
//     owner (bool):     show owner text input
//     chemical(bool):   show chemical name text input
//
// Methods:
//     open(settings, callback): show the dialog
//          settings = {
//              root_location_id (int): root of displayed location tree
//              barcode (string): default barcode value
//              casnumber (string): default CAS #
//              owner (string): default owner
//              chemical (string): default chemical name
//          }
//          callback is function to call when user selects
//
// Values passed to callback
//      {
//          RootID (int): location selected by user
//          Barcode (string): barcode value
//          CASNumber (string): CAS #
//          Owner (string): owner
//          Chemical (string): chemical name
//      }

console.log("Loading searchsettings.vue");

//-----------------------------------------------------------------
//
// Component definition
//
//-----------------------------------------------------------------
const mymodule = {
    components: {
        location: window.VueComponents.Location,
    },

    data: function() {
        return {
            //locations: [],
            items: [],
            selected_items: [],
            root_location_id: 0,
            location_picker_active: false,
            selected_barcode: "",
            selected_chemical: "",
            selected_owner: "",
            selected_casnumber: "",
            selected_location: undefined,
            batch_size: 1000,
        };
    },
    props: {
        debug: {
            type: Boolean,
            default: false,
        },
        height: {
            type: Number,
            default: 800,
        },
        width: {
            type: Number,
            default: 900,
        },
        barcode: { type: Boolean, default: true },
        chemical: { type: Boolean, default: true },
        owner: { type: Boolean, default: true },
        casnumber: { type: Boolean, default: true },
    },
    mounted: function() {
        if (this.debug) console.log("In locationpicker.mounted");
        //$("select").formSelect();
        var elems = document.querySelectorAll("#locationpicker");
        var instances = M.Modal.init(elems, {});
    },

    beforeCreate: function() {
        console.log("In searchsettings:beforeCreate", this);
    },

    created: function() {
        if (this.debug) console.log("In searchsettings:created", this);
        this.callback = undefined;
    },

    methods: {
        open: function(settings, callback) {
            console.error("searchsettings.vue.open", settings);
            let root_location_id = 0;
            this.selected_barcode = settings.Barcode || "";
            this.selected_chemical = settings.Chemical || "";
            this.selected_casnumber = settings.CASNumber || "";
            this.selected_owner = settings.Owner || "";
            let self = this;
            this.callback = callback;
            this.root_location_id = root_location_id;

            if (this.debug) console.log("In searchsettings.open() - root location id = " + root_location_id);
            if (this.debug) console.log("settings:", settings);

            // open the modal dialog
            this.location_picker_active = true;
        },

        on_item_clicked: function(item) {
            if (this.debug) console.log("Item clicked", item);
            if (this.debug) console.log("Selected items:", this.selected_items);
        },

        on_location_selected: function(loc) {
            console.log("searchsettings.on_location_selected:", loc);
            this.selected_location = loc;
        },

        on_accept: function() {
            if (this.debug) console.log("searchsettings.on_accept ", this.selected_location);
            if (this.selected_location) {
                let result = {
                    //SelectedLocation: loc,
                    RootID: this.selected_location.LocationID,
                    Barcode: this.selected_barcode.toLowerCase(),
                    CASNumber: this.selected_casnumber.toLowerCase(),
                    Owner: this.selected_owner.toLowerCase(),
                    Chemical: this.selected_chemical.toLowerCase(),
                };
                if (this.debug) console.log("Closing searchsettings dialog", result);
                this.location_picker_active = false;
                if (this.debug) console.log("searchsettings result:", result);
                if (this.callback) this.callback(result);
                else if (this.debug) console.log("No callback");
            }
        },

        on_decline: function() {
            if (this.debug) console.log("Closing searchsettings dialog");
            this.location_picker_active = false;
            this.$emit("cancel");
        },
    },
};

// make this component visible to outer modules without a module system
// e.g. that use script tags
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["SearchSettings"] = mymodule;
else window.VueComponents = { SearchSettings: mymodule };
</script>

<style scoped>
.location-dialog {
    width: 300px;
    /* min-height: 660px; */
}

.dialog-header {
    font-weight: bold;
}
</style>
