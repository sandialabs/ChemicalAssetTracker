<template>
    <!-- Modal Structure -->
    <div id="locationselect" class="modal location-dialog" v-bind:style="{ minHeight: min_height }">
        <div class="modal-content">
            <span class="dialog-header">Select Location</span>
            <hr />
            <div class="row valign-wrapper">
                <div class="col s12 m3">Site</div>
                <div class="col s12 m9">
                    <select class='browser-default' v-model="levels[0].selected" v-on:change="populate_locations(1)">
                        <option v-for="site in levels[0].locs" v-bind:value="site">{{site.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Building</div>
                <div class="col s12 m9">
                    <select class='browser-default' v-model="levels[1].selected" v-on:change="populate_locations(2)">
                        <option v-for="loc in levels[1].locs" v-bind:value="loc">{{loc.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Room</div>
                <div class="col s12 m9">
                    <select class='browser-default' v-model="levels[2].selected" v-on:change="populate_locations(3)">
                        <option v-for="loc in levels[2].locs" v-bind:value="loc">{{loc.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Storage</div>
                <div class="col s12 m9">
                    <select class='browser-default' v-model="levels[3].selected" v-on:change="populate_locations(4)">
                        <option v-for="loc in levels[3].locs" v-bind:value="loc">{{loc.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Shelf</div>
                <div class="col s12 m9">
                    <select class='browser-default' v-model="levels[4].selected">
                        <option v-for="loc in levels[4].locs" v-bind:value="loc">{{loc.Name}}</option>
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


    module.exports = {
        data: function () {
            return {
                min_height: '500px',
                locations: [],
                levels: [
                    { name: 'sites', locs: [], selected: undefined },
                    { name: 'buildings', locs: [], selected: undefined },
                    { name: 'rooms', locs: [], selected: undefined },
                    { name: 'storages', locs: [], selected: undefined },
                    { name: 'shelves', locs: [], selected: undefined },
                ],
            }
        },
        mounted: function () {
            console.log("In locationselect.mounted");
            $('select').formSelect();
            var elems = document.querySelectorAll('#locationselect');
            var instances = M.Modal.init(elems, {});
        },
        created: function () {
            this.callback = undefined;
        },
        methods: {
            open: function (locations, callback) {
                let self = this;
                this.locations = locations;
                this.callback = callback;
                for (let i = 0; i < 5; i++) {
                    this.levels[i].locs.length = 0;
                    this.levels[i].selected = undefined;
                }
                let sites = this.levels[0];
                sites.locs.length = 0;
                locations.forEach(function (x) {
                    if (x.LocationLevel == 1) {
                        sites.locs.push(x);
                        console.log("Added site", x);
                    }
                });
                sites.selected = sites.locs[0];
                this.populate_locations(1);
                let dlg = $('#locationselect');
                console.log("Dialog:", dlg);
                if (dlg.modal) {
                    console.log("Opening dialog", dlg.modal);
                    dlg.modal('open');
                    console.log("Done");
                }
                else console.log("dialog.modal is not defined");
            },

            on_accept: function () {
                console.log("Closing usereditor dialog", this.levels);
                $('#locationselect').modal('close');
                let result = this.levels[0].selected;
                for (let i = 1; i < 5; i++) {
                    if (this.levels[i].selected) result = this.levels[i].selected;
                }
                this.callback(result);
            },

            on_decline: function () {
                console.log("Closing usereditor dialog");
                $('#locationselect').modal('close');
                this.$emit('cancel');
            },

            populate_locations: function (level) {
                console.log("Populating level " + level);
                for (let i = level; i < 5; i++) {
                    this.levels[i].locs.length = 0;
                    this.levels[i].selected = undefined;
                }
                let locations = this.levels[level].locs;
                let parent = this.levels[level - 1].selected;
                if (parent) {
                    console.log("Looking for children of " + parent.Name, parent.LocationID);
                    let parent_id = parent.LocationID;
                    this.locations.forEach(function (x) {
                        //console.log("    Checking " + x.Name + " with ParentID " + x.ParentID);
                        if (x.ParentID == parent_id) {
                            locations.push(x);
                            //console.log("    populate_locations added " + x.Name);
                        }
                    });
                    this.levels[level].selected = this.levels[level].locs[0];
                }
                if (level < 4) this.populate_locations(level + 1);
            }
        }
    }

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