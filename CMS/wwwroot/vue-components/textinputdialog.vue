<template>
    <!-- Modal Structure -->
    <div id="textinput-modal" class="modal" v-bind:style="{ width: width }">
        <div class="modal-content">
            <span class="dialog-header">{{header}}</span>
            <hr />
            <div class="input-field">
                <input type="text" id="user-input" v-model="user_input" />
                <label for="user-input">{{text}}</label>
            </div>
        </div>
        <div class="modal-footer">
            <button class="btn green" v-on:click="on_accept()">Save</button>
            <button class="btn red" v-on:click="on_decline()">Cancel</button>
        </div>
    </div>
</template>

<script>

    console.log("Loading textinputdialog.vue");

    module.exports = {
        props: ['width'],
        data: function () {
            return {
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
                //this.user_input = "";
                let dlg = $('#textinput-modal');
                console.log("Opening stext input dialog", dlg);
                dlg.modal('open');
            },

            on_accept: function () {
                console.log("Closing text input dialog");
                $('#textinput-modal').modal('close');
                if (this.callback) {
                    this.callback('save', this.user_input);
                }
                this.$emit('save', this.user_input);
            },

            on_decline: function () {
                console.log("Closing text input dialog");
                $('#textinput-modal').modal('close');
                if (this.callback) {
                    this.callback('cancel');
                }
                this.$emit('cancel');
            }
        },
    }
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