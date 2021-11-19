<template>
    <div class="text-xs-center">
        <v-dialog v-model="location_picker_active" :width="width" :height="height" style="max-height: 400px;">
            <v-card>
                <v-card-title class="headline grey lighten-2" primary-title>
                    Select Location
                </v-card-title>
                <v-card-text style="text-align: left !important;">
                    <location :debug="debug" ref="location" width="100%" v-on:select="on_location_selected"></location>
                </v-card-text>
                <v-card-actions>
                    <v-btn small color="green" v-on:click="on_accept()">Select</v-btn>
                    <v-btn small color="red" v-on:click="on_decline()">Cancel</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>
</template>

<script>

console.log("Loading locationpicker.vue");

//-----------------------------------------------------------------
//
// Component definition
//
//-----------------------------------------------------------------
const mymodule = {
    components: {
        location: VueComponents.Location,
    },
    data: function() {
        return {
            //locations: [],
            selected_location: undefined,
            location_picker_active: false,
        };
    },
    props: {
        select_level: {
            type: Number,
            default: -1,
        },
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
    },
    mounted: function() {
        console.log("In locationpicker.mounted");
        $("select").formSelect();
        var elems = document.querySelectorAll("#locationpicker");
        var instances = M.Modal.init(elems, {});
    },

    created: function() {
        this.callback = undefined;
    },

    methods: {
        //---------------------------------------------------------
        //
        // Call the open method to open the dialog and return the
        // location that the user selects.  It takes two arguments,
        // a "settings" object and a callback function.
        //
        // In your Vue controller:
        //     this.$refs['locationpicker'].open(settings, function (location_id) {
        //     });
        //
        // settings = {
        //     root_location_id: <number>
        // }
        //
        // The callback function should take one parameter, which will be a struct
        // that has this format:
        //
        // {
        //     "LocationID": 1,
        //     "Name": "Ministry of Education",
        //     "ParentID": 0,
        //     "LocationLevel": 0,
        //     "Path": "Ministry of Education",
        //     "IsChanged": false,
        //     "LevelEnum": 0,
        //     "FullLocation": "Ministry of Education",
        //     "ShortLocation": "Ministry of Education",
        //     "Access": {
        //         "UserLocationPermissionID": 0,
        //         "Login": "alfie",
        //         "LocationID": 1,
        //         "CanView": true,
        //         "CanModify": true,
        //         "IsSticky": true,
        //         "IsAccessChanged": false
        //     }
        // }
        //
        //---------------------------------------------------------
        open: function(settings, callback) {
            let root_location_id = settings.root_location_id;
            let self = this;
            this.callback = callback;

            console.log("In locationpicker.open() - root location id = " + root_location_id);
            //if (callback) console.log("Have callback");
            //else console.log("No callback specified");

            // open the modal dialog
            this.location_picker_active = true;
        },

        set_location: function(loc) {
            console.log("locationpicker.vue.set_location", loc);
            this.$refs.location.set_location(loc);
        },

        on_location_selected: function(location) {
            console.log("on_location_select", location);
            this.selected_location = location;
        },

        on_accept: function() {
            if (this.selected_location) {
                if (this.debug) console.log("Closing locationpicker dialog", this.selected_location);
                this.location_picker_active = false;
                if (this.callback) this.callback(this.selected_location);
                else console.log("No callback");
            }
        },

        on_decline: function() {
            if (this.debug) console.log("Closing usereditor dialog");
            this.location_picker_active = false;
            this.$emit("cancel");
        },
    },
};

// make this component visible to outer modules without a module system
// e.g. that use script tags
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["LocationPicker"] = mymodule;
else window.VueComponents = { LocationPicker: mymodule };
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
