<template>
    <div class="text-xs-center">
        <v-dialog v-model="show_infodialog"
                  :width="width"
                   @keyup.enter="close()">
            <v-card >
                <v-card-title class="headline grey lighten-2"
                              primary-title>
                    {{header}}
                </v-card-title>

                <v-card-text>
                    {{text}}
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

    console.log("Loading infodialog.vue");

    let mymodule = {
        props: ['width'],
        data: function () {
            return {
                show_infodialog: false,
                header: "Info",
                text: "",
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In infodialog.mounted");
            var elems = document.querySelectorAll('#info-modal');
            var instances = M.Modal.init(elems, {});
        },
        methods: {
            open: function (text, header, callback) {
                console.log("In infodialog.vue.open: width prop is", this.width);
                if (text) this.text = text;
                if (header) this.header = header;
                this.callback = callback;
                this.show_infodialog = true;
            },
            close: function() {
                this.show_infodialog = false;
                console.log("CLOSE INFO");
                this.$emit('closed');
            }
        },
    }
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['InfoDialog'] = mymodule;
    else window.VueComponents = { InfoDialog: mymodule };
    
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