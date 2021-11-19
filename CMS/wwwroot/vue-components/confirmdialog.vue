<template>
    <!-- Modal Structure -->
    <div id="confirm-modal" class="modal" v-bind:style="{ width: width }">
        <div class="modal-content">
            <span class="dialog-header">{{header}}</span>
            <hr />
            <p>{{text}}</p>
        </div>
        <div class="modal-footer">
            <button class="btn green" v-on:click="on_accept()">Yes</button>
            <button class="btn red" v-on:click="on_decline()">No</button>
        </div>
    </div>
</template>

<script>

    console.log("Loading confirmdialog.vue");

    module.exports = {
        props: ['width'],
        data: function () {
            return {
                header: "Confirm",
                text: "Please confirm",
            }
        },
        created: function () {
            this.callback = undefined;
        },
        mounted: function () {
            console.log("In confirmdialog.mounted");
            var elems = document.querySelectorAll('#confirm-modal');
            var instances = M.Modal.init(elems, {});
        },
        methods: {
            open: function (text, header, callback) {
                if (text) this.text = text;
                if (header) this.header = header;
                this.callback = callback;
                let dlg = $('#confirm-modal');
                console.log("Opening confirm dialog", dlg);
                dlg.modal('open');
            },

            on_accept: function () {
                console.log("Closing confirm dialog");
                $('#confirm-modal').modal('close');
                if (this.callback) {
                    this.callback('confirmed');
                }
                this.$emit('confirmed');
            },

            on_decline: function () {
                console.log("Closing confirm dialog");
                $('#confirm-modal').modal('close');
                if (this.callback) {
                    this.callback('declined');
                }
                this.$emit('declined');
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