<template>
    <div class="white">
        <v-toolbar dark>
            <v-toolbar-title>{{ item.ChemicalName }}</v-toolbar-title>
            <v-spacer></v-spacer>
            <v-toolbar-title>{{ item.CASNumber }}</v-toolbar-title>
        </v-toolbar>
        <v-layout class="mt-2">
            <v-flex sm6 xs12 class="mx-2">
                <v-card flat>
                    <v-card-title class="headline grey lighten-2" primary-title> Hazard Information</v-card-title>
                    <v-card-text style="max-height: 400px; overflow-y: auto;">
                        <div v-if="hazards.length == 0">
                            No hazard information available.
                        </div>
                        <div v-if="hazards.length > 0">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Hazard Class</th>
                                        <th>Category Code(s)</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="hazard in hazards" v-bind:key="hazard.GHSCode">
                                        <td style="text-align: center;">{{ hazard.GHSCode }}</td>
                                        <td style="text-align: center;">{{ hazard.HazardClass }}</td>
                                    </tr>
                                </tbody>
                            </table>
                            <i>Annex VI of the CLP Regulation (EU REGULATION (EC) No 1272/2008)</i>
                        </div>
                    </v-card-text>
                </v-card>
            </v-flex>
            <v-flex sm6 xs12 class="mx-2">
                <v-card flat style="max-height: 600px;">
                    <v-card-title class="headline grey lighten-2" primary-title>
                        Disposal Recommendations
                    </v-card-title>
                    <v-card-text class='subheading' style="max-height: 400px; overflow-y: auto;">
                        <div class='mt-2'>
                            CWC Schedule chemical.  Contact your local OPCW National Authority for specific disposal recommendations.
                        </div>
                        <div class='mt-2'>
                            <a href="https://www.osti.gov/biblio/1618030-neutralization-disposal-laboratory-scale-toxic-chemicals" target="Disposal">
                                Go To Web Site
                            </a>
                        </div>
                        <div class='mt-2'>
                            <a href="/assets/neutralization_and_disposal.pdf" target="Disposal">
                                Open PDF
                            </a>
                        </div>
                    </v-card-text>
                </v-card>
            </v-flex>
        </v-layout>
    </div>
</template>

<script>
console.log("Loading hazardinfo.vue");

let mymodule = {
    props: ["debug", "item"],
    data: function() {
        return {
            casnumber: "",
            hazards: [],
            procedures: [],
        };
    },
    created: function() {
        this.callback = undefined;
    },
    mounted: function() {
        console.log("In hazardinfo.mounted");
    },
    watch: {
        item: function(val) {
            console.log("HazardInfo item changed: ", val);
            this.lookup(val);
        },
    },
    methods: {
        lookup: function(item) {
            let self = this;
            let url = utils.api_url("gethazardinfo") + "/" + item.CASNumber;
            console.log("Calling " + url);
            api.axios_get({
                url: url,
                verbose: true,
                caller: "lookup",
                onsuccess: function(ajax_result) {
                    console.log("lookup succeeded", ajax_result);
                    self.casnumber = item.CASNumber;
                    self.hazards = ajax_result.Data.hazardcodes.splice(0);
                    self.procedures = ajax_result.Data.disposal;
                },
            });
        },
    },
};
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["HazardInfo"] = mymodule;
else window.VueComponents = { HazardInfo: mymodule };
</script>

<style scoped>
.dialog-header {
    font-weight: bold;
}
</style>
