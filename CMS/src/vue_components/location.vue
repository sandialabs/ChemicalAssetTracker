<template>
    <div v-bind:style="{ width: width }">
        <div style="text-align: left !important;">
            <div class="subtitle-1 bold">{{ format_selected_location() }}</div>
            <!--
            <div style="max-height: 400px; width: 80%; overflow-y: auto;, margin-top:1em; padding: 4px;">
            -->
            <div v-bind:style="{ maxHeight: height, overflowY: 'auto', marginTop: '1em', padding: '4px', minWidth: width }">
                <v-treeview ref='treeview' :items="items" item-key="id" :active.sync="selected_items" activatable :load-children="load_children">
                    <template slot="label" slot-scope="{ item }">
                        <span flat style="text-align:left; padding-left: 4px;  padding-right: 4px;">{{ item.name }}</span>
                    </template>
                </v-treeview>
            </div>
        </div>
    </div>
</template>

<script>
console.log("Loading location.vue");

//-----------------------------------------------------------------
//
// Component definition
//
//-----------------------------------------------------------------
const mymodule = {
    data: function() {
        return {
            //locations: [],
            items: [],
            selected_items: [],
            selected_location: undefined,
            root_location_id: 0,
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
            default: true,
        },
        height: {
            type: String,
            default: "400px",
        },
        width: {
            type: String,
            default: "400px",
        },
    },
    mounted: function() {
        if (this.debug) console.log("In location.vue.mounted - width is " + this.width);
        let self = this;
        self.initialize();
    },

    beforeCreate: function() {},

    created: function() {
        this.callback = undefined;
        let self = this;
    },

    watch: {
        selected_location: function(val) {
            if (this.debug) console.log("location.vue.watch", val);
            this.$emit("select", val);
        },
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
        initialize: function() {
            if (this.debug) console.log("location.vue.initialize: reading user location subtree");
            let self = this;
            api.fetch_subtree(0, 2, function(result) {
                if (result.Data.subtree) {
                    if (self.debug) console.log("location.vue - have subtree", result.Data.subtree);
                    self.id_map = {};
                    self.all_locations = [];
                    self.register_locations(result.Data.subtree);
                    if (self.debug) console.log("location.vue.initialize: have " + self.all_locations.length + " locations");
                    let root_location = self.all_locations[0];
                    //self.subtree = [];
                    //locationpicker_get_subtree(root_location, self.subtree);
                    //console.log("Have subtree:", this.subtree);
                    self.items.length = 0;
                    let root = self.build_subtree(root_location);
                    if (self.debug) console.log("location.vue.initialize: subtree root", root);
                    self.items.push(root);
                    self.$forceUpdate();
                    self.$emit("ready");
                }
            });
        },

        register_location: function(loc) {
            this.id_map[loc.LocationID] = loc;
            this.all_locations.push(loc);
            loc.children = [];
        },

        register_locations: function(locations) {
            let self = this;
            locations.forEach(function(x) {
                self.register_location(x);
            });
            locations.forEach(function(x) {
                self.find_children(x);
            });
        },

        find_children: function(loc) {
            if (loc.ParentID > 0) {
                let parent = this.id_map[loc.ParentID];
                if (parent) {
                    parent.children.push(loc);
                }
            }
        },

        save_subtree: function(loc, location_list, indent) {
            let self = this;
            indent = indent || "";
            if (this.debug) console.log(indent + "location.vue.save_subtree:", loc);
            if (loc) {
                location_list.push(loc);
                loc.children.forEach(function(child) {
                    this.save_subtree(child, location_list, indent + "    ");
                });
            }
        },

        build_subtree: function(location) {
            let myitem = { id: location.LocationID, name: location.Name, children: [] };
            let child_locations = location.children;
            let self = this;
            child_locations.forEach(function(child) {
                let child_item = self.build_subtree(child);
                myitem.children.push(child_item);
            });
            return myitem;
        },

        set_location: function(loc) {
            console.log("location.vue.select_location", loc);
            if (loc)  this.select_location = loc;
        },

        get_selected_location: function() {
            if (this.debug) console.log("location.vue.get_selected_location: selected_items length is " + this.selected_items.length);
            if (this.selected_items.length > 0) {
                let loc_id = this.selected_items[0];
                this.selected_location = this.find_location(loc_id);
                return this.selected_location;
            } else return undefined;
        },

        find_location: function(location_id) {
            return this.id_map[location_id];
            // return this.subtree.filter(function(loc) {
            //     return loc.LocationID == location_id;
            // })[0];
        },

        format_selected_location: function() {
            if (this.debug) console.log("location.vue.format_selected_location", this.selected_location);
            let loc = this.get_selected_location();
            if (loc) {
                let result = this.format_location(loc);
                if (this.debug) console.log("location.vue.format_selected_location: formatted location is", result);
                return result;
            }
            return "";
        },

        format_location: function(loc, indent) {
            let result = "";
            if (typeof indent != "string") indent = "";
            let prefix = indent + "location.vue.format_location: ";
            if (this.debug) console.log(prefix + "arg is " + loc.Name + " at level " + loc.LocationLevel);
            if (loc) {
                if (loc.LocationLevel > 1 && loc.ParentID > 0) {
                    let parent = this.find_location(loc.ParentID);
                    let parent_loc = this.format_location(parent, indent + "....");
                    if (this.debug) console.log(prefix + 'parent is "' + parent_loc + '"');
                    result = parent_loc + "/" + loc.Name;
                } else {
                    if (this.debug) console.log(prefix + "root is " + loc.Name);
                    result = "/" + loc.Name;
                }
            } else {
                if (this.debug) console.log(prefix + "loc is null or undefined");
            }
            if (this.debug) console.log(prefix + 'returning "' + result + '"');
            return result;
        },

        load_children: function(item) {
            if (this.debug) console.log("load_children:", item);
            let url = utils.api_url("getuserlocationsubtree") + "/*/" + item.id + "/" + 1;
            if (this.debug) console.log("location.vue.load_children: Calling " + url);
            let self = this;
            return fetch(url)
                .then((res) => res.json())
                .then((ajax_result) => {
                    if (self.debug) console.log("location.vue.load_children: ajax_result:", ajax_result);
                    let new_items = ajax_result.Data.subtree.filter(function(x) {
                        return x.LocationID != item.id;
                    });
                    new_items.forEach(function(x) {
                        self.register_location(x);
                        if (x.IsLeaf) item.children.push({ id: x.LocationID, name: x.Name });
                        else {
                            item.children.push({ id: x.LocationID, name: x.Name, children: [] });
                        }
                    });
                })
                .catch((err) => console.warn(err));
        },

        collapse: function() {
            console.log("location.vue - colapsing");
            this.$refs.treeview.updateAll(false);
        }
    },
};

// make this component visible to outer modules without a module system
// e.g. that use script tags
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["Location"] = mymodule;
else window.VueComponents = { Location: mymodule };
</script>

<style scoped>
.location-dialog {
    width: 300px;
    /* min-height: 660px; */
}

.active {
    background-color: lightgreen;
    color: black;
}

.dialog-header {
    font-weight: bold;
}
</style>
