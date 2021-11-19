<template>
    <!-- Modal Structure -->
    <div id="locationselect" class="modal location-dialog" v-bind:style="{ minHeight: min_height, width: '600px' }">
        <div class="modal-content">
            <span class="headline">Select Location</span>
            <hr />
            <div style="margin-top: 8px;  margin-bottom: 8px; font-weight: bold;">
                {{format_selected_location()}}
            </div>
            <hr/>
            <div class="row valign-wrapper" style="margin-top: 1em;" v-for="i in [1,2,3,4,5,6]" v-if="levels[i].IsVisible()">
                <div class="col s12 m3">
                    <v-icon small color="default" v-if="i < 6" v-on:click="toggle_visibility(i)">add_circle</v-icon>
                    &nbsp; {{levels[i].m_levelname}}
                </div>
                <div class="col s12 m9">
                    <select class='browser-default' style="border: 1px solid gray" v-model="levels[i].m_selected_location" v-on:change="refresh_level(i)">
                        <option v-for="child in levels[i].m_locations" v-bind:value="child">
                            {{child.Name}}
                        </option>
                    </select>
                </div>
            </div>
        </div>
        <div class="modal-footer">
            <button class="btn green" v-on:click="on_accept()">Save</button>
            <button class="btn red" v-on:click="on_decline()">Cancel</button>
            <span>&nbsp;&nbsp;</span>
        </div>
    </div>
</template>


<script>

    console.log("Loading locationselect.vue");

    //-----------------------------------------------------------------
    //
    // ES5 class to represent a level in the location hierarchy
    //
    //-----------------------------------------------------------------
    function LocationLevel(levelnum, name, parent_level) {
        this.m_levelnum = levelnum;
        this.m_levelname = name;
        this.m_locations = [];
        this.m_selected_location = undefined;
        this.m_is_visible = false;
        this.m_child_level = undefined;
        this.NewLocation = undefined;
        if (parent_level) parent_level.m_child_level = this;
    }

    LocationLevel.prototype.IsVisible = function () {
        return this.m_is_visible;
    }

    LocationLevel.prototype.HasChild = function () {
        return this.m_child_level != undefined;
    }

    LocationLevel.prototype.SetVisibility = function (val) {
        this.m_is_visible = val;
        if (!this.IsVisible() && this.HasChild()) {
            this.m_child_level.SetVisibility(false);
        }
    }
    LocationLevel.prototype.ToggleVisibility = function () {
        this.SetVisibility(!this.m_is_visible);
    }
    LocationLevel.prototype.SelectLocations = function (all_locations, parent_location_id) {
        //console.log("SelectLocations for level " + this.m_levelnum, this);
        let self = this;
        if (parent_location_id) {
             //console.log("    using locations that are children of ", parent_location_id);
;            this.m_locations = all_locations.filter(function (x) { return (x.ParentID == parent_location_id); });
        }
        else {
            //console.log("    using locations at level " + this.m_levelnum);
            this.m_locations = all_locations.filter(function (x) { return (x.LocationLevel == self.m_levelnum); })
        }
        if (this.m_locations.length > 0) {
            //console.log("    locations found:", this.m_locations);
            this.m_selected_location = this.m_locations[0];
            if (this.m_child_level) {
                this.m_child_level.SelectLocations(all_locations, this.m_selected_location.LocationID);
            }
        }
        else console.log("    No locations found");
    }

    //-----------------------------------------------------------------
    //
    // Component definition
    //
    //-----------------------------------------------------------------
    const mymodule = {
        data: function () {
            return {
                min_height: '600px',
                locations: [],
                levels: []
            }
        },
        props: {
            debug: Boolean
        },
        mounted: function () {
            console.log("In locationselect.mounted");
            $('select').formSelect();
            var elems = document.querySelectorAll('#locationselect');
            var instances = M.Modal.init(elems, {});
        },
        beforeCreate: function () {
        },

        created: function () {
            this.callback = undefined;
            let level_names = ['Institution', 'University', 'Department', 'Building', 'Room', 'Storage', 'Shelf'];
            this.levels.push(new LocationLevel(0, 'Institution', undefined));
            for (let i = 1; i < level_names.length; i++) {
                this.levels.push(new LocationLevel(i, level_names[i], this.levels[i - 1]));
            }
            this.levels[1].m_is_visible = true;
        },

        methods: {
            open: function (locations, levelnames, callback) {
                if (this.debug) {
                    console.log("In locationselect.open");
                    console.log("    locations:", locations);
                    console.log("    levelnames:", levelnames);
                    console.log("    levels:", this.levels);
                }
                let self = this;
                this.locations = locations;
                this.callback = callback;

                for (let i = 1; i < this.levels.length; i++) this.levels[i].m_is_visible = (i < 2);

                this.levels[0].SelectLocations(this.locations, undefined);
                if (this.debug) console.log("Levels:", this.levels);

                // open the modal dialog
                let dlg = $('#locationselect');
                if (dlg.modal) {
                    dlg.modal('open');
                }
                else {
                    if (this.debug) console.log("dialog.modal is not defined");
                }
            },

            toggle_visibility: function (i) {
                if (i < 6) {
                    i += 1;
                    level = this.levels[i];
                    if (this.debug) console.log("toggle_visibility at level " + i + ": " + level.m_levelname);
                    level.ToggleVisibility();
                }
            },

            on_accept: function () {
                if (this.debug) console.log("Closing usereditor dialog", this.levels);
                $('#locationselect').modal('close');
                let result = this.get_selected_location();
                this.callback(result);
            },

            on_decline: function () {
                if (this.debug) console.log("Closing usereditor dialog");
                $('#locationselect').modal('close');
                this.$emit('cancel');
            },

            refresh_level: function (level) {
                let leveldata = this.levels[level];
                if (this.debug) console.log("refresh_level " + level + " " + leveldata.m_levelname);
                let selected_location = leveldata.m_selected_location;
                if (this.debug) console.log("    selected_location:", selected_location);
                if (level < 6) {
                    let child = this.levels[level + 1];
                    if (this.debug) console.log("Refresh child: ", child);
                    child.SelectLocations(this.locations, selected_location.LocationID);
                }
            },

            find_location: function (location_id) {
                return this.locations.filter(function (x) { return (x.LocationID == location_id); })[0];
            },

            get_selected_location: function () {
                let result = undefined;
                for (let i = 1; i < this.levels.length && this.levels[i].IsVisible(); i++) {
                    result = this.levels[i].m_selected_location;
                }
                return result;
            },

            format_selected_location: function () {
                let loc = this.get_selected_location();
                return this.format_location(loc);
            },

            format_location: function (loc) {
                if (loc) {
                    if (loc.LocationLevel > 1  && loc.ParentID > 0) {
                        let parent = this.find_location(loc.ParentID);
                        return (this.format_location(parent) + "/" + loc.Name);
                    }
                    else return ("/" + loc.Name);
                }
                else return ("");
            }

        }
    }

    // make this component visible to outer modules without a module system
    // e.g. that use script tags
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['LocationSelect'] = mymodule;
    else window.VueComponents = { LocationSelect: mymodule };

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