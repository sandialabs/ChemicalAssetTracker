<template>
    <div v-if="visible">
        <v-card flat dark>
            <v-card-title class="headline" primary-title>
                {{ header }}
            </v-card-title>
            <v-card-text>
                <div class="checkboxes">
                    <v-checkbox class='mycheckbox' 
                        v-for="def in column_defs" 
                        v-model="def.value" 
                        :label="def.label" 
                        v-bind:key="def.name" 
                        v-on:change="on_checkbox_change(def.name)"> </v-checkbox>
                </div>
            </v-card-text>
        </v-card>
    </div>
</template>

<script>

//###################################################################A#########
//
// Use: <columnselect :visible="colsel_visible" :callback="update_column"></columnselect>
//
// The colsel_visible prop is a boolean in your Vue data.  You can show/hide
// the column selector by toggling this value.
//
// The callback is optional. It will be called whenever a checkbox value
// changes.  The callback is called with three arguments.
//   op:    currently always 'update'
//   name:  the column name
//   value: the new boolean value for the column
//
// data: {
//     column_flags: [
//        barcode: true,
//        casnumber: false,
//        chemical: true
//        etc
//     ],
//     colsel_visible: false
// }
//
// on_column_change: function(op, name, value) {
//     if (op == 'update') column_flags[name] = value;
// }
//
//#############################################################################

console.log("Loading columnselect.vue");

const mymodule = {
    props: {
        columns: {
            type: String,
            required: false
        },
        visible: {
            type: Boolean,
            required: true
        },
        callback: {
            type: Function,
            required: true
        },
        column_defs: {
            type: Array,
            default: function() {
                return [
                    { name: "barcode", label: "Barcode", value: true, enabled: true },
                    { name: "casnumber", label: "CAS #", value: true, enabled: true },
                    { name: "chemical", label: "Chemical Name", value: true, enabled: true },
                    { name: "location", label: "Location", value: true, enabled: true },
                    { name: "datein", label: "Date In", value: true, enabled: true },
                    { name: "expiry", label: "Expiration", value: true, enabled: true },
                    { name: "owner", label: "Owner", value: true, enabled: true },
                    { name: "group", label: "Storage Group", value: true, enabled: true },
                    { name: "size", label: "Size", value: true, enabled: true },
                    { name: "remaining", label: "Remaining Qty", value: true, enabled: true },
                    { name: "state", label: "State", value: true, enabled: true },
                    { name: "sds", label: "SDS", value: true, enabled: true },
                    { name: "hazards", label: "Hazards", value: true, enabled: true },
                    { name: "units", label: "Units", value: true, enabled: true },
                    { name: "notes", label: "Notes", value: true, enabled: true },
                ];
            }
        }
    },
    data: function() {
        return {
            header: "Select Display Columns"
        };
    },
    created: function() {
        console.log("In columnselect.mounted");
    },
    mounted: function() {
        console.log("In columnselect.mounted");
    },
    methods: {

        on_checkbox_change: function(name) {
            let newvalue = this.column_defs.filter(function(def) { return def.name == name; } )[0].value;
            //console.log("in on_checkbox_change:", name, newvalue);
            if (this.callback) {
                this.callback("update", name, newvalue);
            }
            this.$emit("save", this.column_defs);
        }, 

    }
};

module.exports = mymodule;
if (window.VueComponents) window.VueComponents["ColumnSelect"] = mymodule;
else window.VueComponents = { ColumnSelect: mymodule };
</script>
<style scoped>
.checkboxes {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
}

.mycheckbox {
    width: 180px;
    min-width: 180px;
    max-width: 180px;
}
</style>
