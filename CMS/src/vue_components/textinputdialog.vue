<template>

    <div class="text-xs-center">
        <v-dialog v-model="textinput_dialog_active" :width="width">
            <v-card class="noborder"  @keyup.enter="on_accept()">
                <v-card-title class="headline grey lighten-2" primary-title>
                    {{header}}
                </v-card-title>
                <v-card-text>
                    <v-text-field :label="text" single-line v-model="user_input"></v-text-field>
                </v-card-text>
                <v-card-actions>
                    <v-btn small flat color="green" v-on:click="on_accept()">Ok</v-btn>
                    <v-btn small flat color="red" v-on:click="on_decline()">Cancel</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>

</template>

<script>

    console.log("Loading textinputdialog.vue");

    const mymodule = {
        props: ['width'],
        data: function () {
            return {
                textinput_dialog_active: false,
                header: "Input",
                text: "Enter text below",
                user_input: ""
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In textinputdialog.mounted");
            var elems = document.querySelectorAll('#textinput-modal');
            var instances = M.Modal.init(elems, {});
        },
        methods: {
            open: function (text, header, callback) {
                if (text) this.text = text;
                if (header) this.header = header;
                this.callback = callback;
                this.user_input = "";
                this.textinput_dialog_active = true;
            },

            on_accept: function () {
                console.log("Closing text input dialog");
                this.textinput_dialog_active = false;
                if (this.callback) {
                    this.callback('save', this.user_input);
                }
                this.$emit('save', this.user_input);
            },

            on_decline: function () {
                console.log("Closing text input dialog");
                this.textinput_dialog_active = false;
                if (this.callback) {
                    this.callback('cancel');
                }
                this.$emit('cancel');
            }
        },
    }

    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['TextInputDialog'] = mymodule;
    else window.VueComponents = { TextInputDialog: mymodule };

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

    div.noborder input {
        border-bottom: none !important;
    }

</style>