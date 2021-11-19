<template>
    <div class="text-xs-center">
        <v-dialog v-model="show_pictogram_dialog"
                  :width="width"
                   @keyup.enter="close()">
            <v-card >
                <v-card-title class="headline grey lighten-2"
                              primary-title>
                    GHS Pictogram
                </v-card-title>

                <v-card-text>
                    <div v-if="info">
                        <v-img :src="'/assets/' + info.image_file" ></v-img>
                        <table class="cat-standard" style="margin-top: 1em;">
                            <tbody>
                                <tr>
                                    <td>Name:</td>
                                    <td>{{info.name}}</td>
                                </tr>
                                <tr>
                                    <td>Description:</td>
                                    <td>{{info.description}}</td>
                                </tr>
                                <tr>
                                    <td>ID:</td>
                                    <td>{{info.id}}</td>
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
    // PictogramDialog.vue
    //
    // Properties:
    //     width: (string) width of dialog, e.g. "300px"
    //
    // Methods:
    //     open(imagefile) - open the dialog
    //         imagefile is the name of a pictogram image file, e.g. "irritant.png"
    //
    // Use:
    //     in Vue instance definition:
    //         components: {
    //             "pictogramdialog": VueComponents.PictogramDialog
    //         }
    //
    //     Markup:
    //         <pictogramdialog ref="pictogramdialog" width="300px"></pictogramdialog>
    //
    //#########################################################################

    console.log("Loading infodialog.vue");


    let mymodule = {
        props: ['width'],
        data: function () {
            return {
                show_pictogram_dialog: false,
                info: undefined,
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In pictogramdialog.mounted");
        },
        methods: {
            open: function (imagefile) {
                console.log("In pictogramdialog.vue.open: width prop is", this.width);
                this.info = GHS_Pictogram_Data.filter(function(x) { return x.image_file == imagefile;})[0];
                this.show_pictogram_dialog = true;
            },
            close: function() {
                this.show_pictogram_dialog = false;
            }
        },
    }
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['PictogramDialog'] = mymodule;
    else window.VueComponents = { PictogramDialog: mymodule };
    
</script>
<style scoped>



</style>