<template>
    <div class="text-xs-center">
        <v-dialog v-model="show_coc_dialog"
                  :width="width"
                   @keyup.enter="close()">
            <v-card >
                <v-card-title class="headline grey lighten-2"
                              primary-title>
                    Chemicals of Concern Information
                </v-card-title>

                <v-card-text>
                    <div v-if="info">
                        <div class='title'>
                            CAS #: {{casnumber}}
                        </div>
                        <div class='subheading' v-if="chemical">
                            {{chemical}}
                        </div>
                        <table class="cat-standard" style="margin-top: 1em;">
                            <thead>
                                <tr><th style='text-align: left;'>Source</th><th style='text-align: left'>Value</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="(value, name) in info">
                                    <td>{{name}}</td>
                                    <td v-bind:class="{ iscoc: value }">{{yesno(value)}}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </v-card-text>

                <v-divider></v-divider>

                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn color="primary"
                           flat
                           @click="close()">
                        Dismiss
                    </v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>

</template>

<script>

    //#########################################################################
    //
    // COCDialog.vue
    //
    // Properties:
    //     width: (string) width of dialog, e.g. "300px"
    //
    // Methods:
    //     open(casnumber, chemical) - open the dialog
    //         casnumber is the CAS# to look up
    //         chemical is tne chemical name
    //
    // Use:
    //     in Vue instance definition:
    //         components: {
    //             "cocdialog": VueComponents.COCDialog
    //         }
    //
    //     Markup:
    //         <cocdialog ref="cocdialog" width="300px"></cocdialog>
    //
    //#########################################################################


    console.log("Loading cocdialog.vue");


    let mymodule = {
        props: ['width'],
        data: function () {
            return {
                show_coc_dialog: false,
                info: undefined,
                inventory_item: undefined,
                casnumber: '',
                chemical: undefined,
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In cocdialog.mounted");
        },
        methods: {
            open: function (inventory_item) {
                this.info = { };
                this.inventory_item = inventory_item;
                this.casnumber = inventory_item.CASNumber;
                this.chemical = inventory_item.ChemicalName;
                let self = this;
                //console.log("In cocdialog.vue.open: width prop is", this.width);
                //console.log("Item", inventory_item);
                get_hazard_information_for_casnumber(this.casnumber, function(result) {
                    console.log("cocdialog.vue - have results for " + self.casnumber, result);
                    if (result) {
                        self.info['AG'] =  result['COC_AG'];
                        self.info['CFATS'] =  result['COC_CFATS'];
                        self.info['CWC'] =  result['COC_CWC'];
                        self.info['EU'] =  result['COC_EU'];
                        self.info['WMD'] =  result['COC_WMD'];
                        self.info['OTHER'] =  (inventory_item.ItemFlags.OTHERSECURITY == 'X');
                        //console.log("HAZARDS: ", self.info, inventory_item.ItemFlags.OTHERSECURITY);
                        self.show_coc_dialog = true;
                    }
                })
            },
            close: function() {
                this.show_coc_dialog = false;
            },

            yesno: function(value) {
                return value ? 'Yes' : 'No';
            }
        },
    }
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['COCDialog'] = mymodule;
    else window.VueComponents = { COCDialog: mymodule };
    
</script>

<style scoped>

.iscoc {
    background: red;
    color: white;
}


</style>