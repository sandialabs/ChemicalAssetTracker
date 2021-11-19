<template>
    <div class="text-xs-center">
        <v-dialog v-model="confirm_dialog_active" :width="width">
            <v-card>
                <v-card-title class="headline grey lighten-2" primary-title>
                    {{header}}
                </v-card-title>
                <v-card-text>
                    {{text}}
                </v-card-text>
                <v-card-actions>
                    <v-btn small flat color="green" v-on:click="on_accept()">Yes</v-btn>
                    <v-btn small flat color="red" v-on:click="on_decline()">No</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>
</template>

<script>

    console.log("Loading confirmdialog.vue");

    let mymodule = {
        props: ['width'],
        data: function () {
            return {
                header: "Confirm",
                text: "Please confirm",
                confirm_dialog_active: false,
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In confirmdialog.mounted");
        },
        methods: {
            open: function (text, header, callback) {
                console.log("ConfirmDialog open");
                if (text) this.text = text;
                if (header) this.header = header;
                this.callback = callback;
                this.confirm_dialog_active = true;
            },

            on_accept: function () {
                console.log("Closing confirm dialog");
                this.confirm_dialog_active = false;
                if (this.callback) {
                    this.callback('confirmed');
                }
                this.$emit('confirmed');
            },

            on_decline: function () {
                console.log("Closing confirm dialog");
                this.confirm_dialog_active = false;
                if (this.callback) {
                    this.callback('declined');
                }
                this.$emit('declined');
            }
        },
    }
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['ConfirmDialog'] = mymodule;
    else window.VueComponents = { ConfirmDialog: mymodule };
    
</script>
<style scoped>


    .dialog-header {
        font-weight: bold;
    }

    .btn {
        overflow: hidden;
    }

    .btn:hover:before {
        opacity: 1;
    }

    .btn:before {
        content: '';
        opacity: 0;
        background-color: rgba(0,0,0,.6);
        position: absolute;
        width: 100%;
        height: 100%;
        left: 0;
        top: 0;
        transition: opacity .2s;
        z-index: -1;
    }

</style>