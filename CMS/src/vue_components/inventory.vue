<template>
    <v-layout>
        <div v-show="inventory" class="controls">
            <v-btn class="blue--text font-weight-bold" flat large icon v-on:click="on_bookmark('set-bookmark')"><v-icon title="Bookmark" large>bookmark</v-icon></v-btn>
            <v-spacer></v-spacer>
            <span style="font-weight: bold;">Selected Location: {{ nice_location(selected_location) }}</span>
            <v-btn @click="on_select_location()" icon small><v-icon>search</v-icon></v-btn>
            <span style="margin-left: 2rem;" v-bind:class="inventory_message_class">{{ inventory_message }}</span>
            <v-btn icon style="margin-top: 0.4rem;" v-if="have_prev_page()" v-on:click="on_prev_page()"><v-icon title="Previous Page">fast_rewind</v-icon></v-btn>
            <span class="subtitle-1" v-if="inventory.length > 0">Items {{ search_settings.ResultOffset + 1 }} ... {{ search_settings.ResultOffset + inventory.length }} of {{ search_settings.ItemsMatched }}</span>
            <span class="subtitle-1" v-if="inventory.length == 0">No items found</span>
            <v-btn icon style="margin-top: 0.4rem;" v-if="have_next_page()" v-on:click="on_next_page()"><v-icon title="Next Page">fast_forward</v-icon></v-btn>
            <v-spacer></v-spacer>
            <v-btn v-if="!readonly" class="red--text font-weight-bold" flat icon v-on:click="on_add()"><v-icon large title="Add New">control_point</v-icon></v-btn>
        </div>
        <searchsettings ref="searchsettings"></searchsettings>
    </v-layout>
</template>

<script>
console.log("Loading inventory.vue");

Vue.component("searchsettings", VueComponents.SearchSettings);

const mymodule = {
    components: {},
    props: {
        debug: {
            type: Boolean,
            required: false,
            default: false,
        },
        readonly: {
            type: Boolean,
            required: false,
            default: false,
        },
    },
    data: function() {
        return {
            inventory: [],
            root_location: { LocationID: 0, FullLocation: "" },
            search_settings: { Barcode: "", CASNumber: "", Owner: "", Chemical: "", ItemsMatched: 0 },
            selected_location: { LocationID: 0, FullLocation: "" },
            inventory_message: "",
            inventory_message_class: "normal",
        };
    },

    created: function() {
        if (this.debug) console.log("inventory component created");
    },

    mounted: function() {
        if (this.debug) console.log("inventory component mounted");
        this.$emit("mounted");
    },

    methods: {
        set_root_location: function(root_loc, bookmark) {
            if (this.root_location.LocationID == 0) {
                this.root_location = root_loc;
                if (this.debug) console.log("set_root_location", root_loc.LocationID, root_loc.Path);
                this.fetch_inventory(true, bookmark);
            } else console.error("already have root location");
        },

        fetch_inventory: function(reset_settings, bookmark) {
            this.$emit("begin-load");
            let self = this;
            if (this.search_settings.RootID == undefined) {
                if (this.debug) console.log("inventory.vue.fetch_inventory: no RootID");
                if (bookmark) {
                    if (this.debug) console.log("    using bookmark:", bookmark);
                    this.search_settings.RootID = bookmark.LocationID;
                } else {
                    if (this.debug) console.log("    using root location:", this.root_location);
                    this.search_settings.RootID = this.root_location.LocationID;
                }
            }
            if (reset_settings) {
                this.search_settings.IsInitialQuery = true;
                this.search_settings.ItemsMatched = 0;
                this.search_settings.ResultOffset = 0;
            }
            api.search_inventory(this.search_settings, function(result) {
                self.inventory.length = 0;
                if (result.Success) {
                    self.inventory = result.Data.Inventory;
                    self.selected_location = result.Data.HomeLocation;
                    self.search_settings = result.Data.SearchSettings;
                    self.inventory_message_class = "normal";
                    self.$emit("end-load", self.inventory);
                } else {
                    self.inventory_message = result.Message;
                    self.inventory_message_class = "error-message";
                    if (this.debug) console.error(result.Message);
                    self.$emit("end-load");
                }
                setTimeout(function() {
                    self.inventory_message = "";
                    self.inventory_message_class = "normal";
                }, 5000);
            });
        },

        refresh: function() {
            this.fetch_inventory(false, undefined);
        },

        nice_location: function(location) {
            if (location) {
                if (location.LocationLevel == 0) return location.Name;
                let ix = location.FullLocation.indexOf("/", 1);
                if (ix > 0) return location.FullLocation.substr(ix + 1);
                return location.FullLocation;
            } else return "";
        },

        have_next_page: function() {
            return this.search_settings.ResultOffset + this.inventory.length < this.search_settings.ItemsMatched;
        },

        have_prev_page: function() {
            return this.search_settings.ResultOffset > 0;
        },

        on_prev_page: function() {
            if (this.search_settings.ResultOffset > 0) {
                this.search_settings.ResultOffset -= this.search_settings.ItemsPerPage;
                if (this.search_settings.ResultOffset < 0) this.search_settings.ResultOffset = 0;
                this.fetch_inventory(false, undefined);
            }
        },

        on_next_page: function() {
            if (this.search_settings.ResultOffset < this.search_settings.ItemsMatched) {
                this.search_settings.ResultOffset += this.search_settings.ItemsPerPage;
                this.fetch_inventory(false, undefined);
            }
        },

        on_select_location: function() {
            // search from the top
            if (this.debug) console.log("inventory.on_select_loction:", this.search_settings);
            if (this.root_location.LocationID) {
                this.search_settings.RootID = this.root_location.LocationID;
                let self = this;
                this.$refs["searchsettings"].open(this.search_settings, function(result) {
                    if (self.debug) console.log("*** In searchsettings callback:", result);
                    self.search_settings = result;
                    //self.selected_location = result.Location;
                    //console.error("New location", result);
                    self.fetch_inventory(true, undefined);
                    self.$forceUpdate();
                    self.$emit("location-changed", result);
                });
            } else console.error("inventory.vue.on_select)_location - root_location is invalid");
        },

        on_add: function() {
            this.$emit("action", "add");
        },

        on_bookmark: function(action) {
            this.$emit("action", action);
        },
    },
};

module.exports = mymodule;
if (window.VueComponents) window.VueComponents["Inventory"] = mymodule;
else window.VueComponents = { Inventory: mymodule };
</script>

<style>
.controls {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: center;
    width: 100%;
}
</style>
