<template>
    <!-- Modal Structure -->
    <div id="user-modal" class="modal userdialog" v-bind:style="{ minHeight: min_height }" >
        <div class="modal-content">
            <span class="dialog-header">{{header}}</span>
            <hr />

            <div class="row valign-wrapper">
                <div class="col s12 m3">UserName</div><div class="col s12 m9"><input type="text" v-model="local_userdata.UserName" v-on:input="on_user_modified()" :readonly="!local_userdata.IsNew" /></div>
            </div>
            <div class="row valign-wrapper" v-if="local_userdata.IsNew">
                <div class="col s12 m3">Password</div><div class="col s12 m9"><input type="text" v-model="local_userdata.Password" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">First Name</div><div class="col s12 m9"><input type="text" v-model="local_userdata.FirstName" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Middle Name</div><div class="col s12 m9"><input type="text" v-model="local_userdata.MiddleName" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Last Name</div><div class="col s12 m9"><input type="text" v-model="local_userdata.LastName" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Phone</div><div class="col s12 m9"><input type="text" v-model="local_userdata.PhoneNumber" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper">
                <div class="col s12 m3">Email</div><div class="col s12 m9"><input type="text" v-model="local_userdata.Email" v-on:input="on_user_modified()" /></div>
            </div>
            <div class="row valign-wrapper" style="margin-top:2em;">
                <div class="col s12 m3">Permissions</div>
                <div class="col s12 m9 valign-wrapper">
                    <label><input type="checkbox" class="filled-in" v-model="local_userdata.IsAdmin" v-on:change="on_user_modified()" /><span class="permission">Admin</span></label>
                    <label><input type="checkbox" class="filled-in" v-model="local_userdata.IsEditor" v-on:change="on_user_modified()" /><span class="permission">Editor</span></label>
                    <label><input type="checkbox" class="filled-in" v-model="local_userdata.IsViewer" v-on:change="on_user_modified()" /><span class="permission">Viewer</span></label>
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

    console.log("Loading userdialog.vue");

    function copy_user(from, to) {
        to.Email = from.Email;
        to.FirstName = from.FirstName;
        to.LastName = from.LastName;
        to.MiddleName = from.MiddleName;
        to.Email = from.Email;
        to.PhoneNumber = from.PhoneNumber;
        to.Password = from.Password;
        to.Roles = from.Roles;
        to.UserName = from.UserName;
        to.IsAdmin = from.IsAdmin;
        to.IsChanged = from.IsChanged;
        to.IsEditor = from.IsEditor;
        to.IsNew = from.IsNew;
        to.IsViewer = from.IsViewer;
    }



    module.exports = {
        props: ['userdata'],
        data: function () {
            return {
                min_height: '660px',
                header: "Confirm",
                text: "Please confirm",
                modified: false,
                local_userdata: {
                    UserName: "",
                    FirstName: "",
                    MiddleName: "",
                    Password: "",
                    Email: "",
                    PhoneNumber: "",
                    Password: '',
                    IsAdmin: false,
                    IsEditor: false,
                    IsViewer: true,
                    IsNew: true,
                    IsChanged: true,
                },
            }
        },
        mounted: function () {
            console.log("In usereditor.mounted");
            var elems = document.querySelectorAll('#user-modal');
            var instances = M.Modal.init(elems, {});
        },
        methods: {
            open: function (header) {
                if (header) this.header = header;
                this.min_height = (this.userdata.IsNew ? '770px' : '660px');
                let dlg = $('#user-modal');
                copy_user(this.userdata, this.local_userdata);
                dlg.modal('open');
            },

            on_accept: function () {
                console.log("Closing usereditor dialog");
                $('#user-modal').modal('close');
                this.local_userdata.IsChanged = this.modified;
                this.$emit('save', this.local_userdata);
            },

            on_decline: function () {
                console.log("Closing usereditor dialog");
                $('#user-modal').modal('close');
                this.$emit('cancel');
            },
            on_user_modified: function () {
                this.modified = true;
            }
        }
    }
</script>

<style scoped>

    .userdialog {
        width: 600px;
        /* min-height: 660px; */
    }

    .dialog-header {
        font-weight: bold;
    }

    .horizontal-label {
        display: inline;
    }

    .red-button {
        background-color: red;
    }

    .permission {
        margin-right: 2em;
        margin-top: 4px;
    }

</style>