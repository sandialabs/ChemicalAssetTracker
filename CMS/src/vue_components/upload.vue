<!-- ---------------------------------------------------------------------- -->
<!--                                                                        -->
<!-- upload.vue - File Upload Component                                     -->
<!--                                                                        -->
<!-- This component will upload a file and send it to a URL.                -->
<!--                                                                        -->
<!-- Props:                                                                 -->
<!--     title (string):       the header to display in the dialog box      -->
<!--     button (string):      the text of the button to upload             -->
<!--     url (string):         where to send the uploaded data              -->
<!--     extensions (string):  valid file extensions                        -->
<!--     owner (string):       owner of the file (passed to URL)            -->
<!--     tag (string):         a tag for the file (passed to URL)           -->
<!--     description (string): file description (passed to URL              -->
<!--                                                                        -->
<!--  When the user clicks the Upload button, an HTTP PUT is sent to the    -->
<!--  URL supplied in props using FormData with the following fields:       -->
<!--      files: a list containing a single file for processing             -->
<!--      owner: the owner from props                                       -->
<!--      tag: the tag from props                                           -->
<!--      description: the description from props                           -->
<!--                                                                        -->
<!-- Example markup:                                                        -->
<!--                                                                        -->
<!-- <upload style="width: 100%;  font-size: large;"                        -->
<!--         :url="import_url"                                              -->
<!--         title="Import Database"                                        -->
<!--         extensions=".db,.sqlite"                                       -->
<!--         owner="none"                                                   -->
<!--         :tag="target_location_id()"                                    -->
<!--         description="none"                                             -->
<!--         v-on:complete="on_upload_complete"                             -->
<!--         v-on:error="on_upload_error"></upload>                         -->
<!--                                                                        -->
<!-- ---------------------------------------------------------------------- -->
<template>
    <v-card dark flat>
        <v-card-title class="headline justify-center">
            <div>{{ title }}</div>
        </v-card-title>
        <v-card-text style="border: 2px solid white;">
            <form id="upload-form" method="post" enctype="multipart/form-data" accept=".txt" action="#" onsubmit="return file_ok()">
                <div class="form-group">
                    <div class="col-md-10">
                        <div class="white-text mb-4">Select a file to upload</div>
                        <input id="file-input" type="file" name="files" single />
                    </div>
                </div>
                <div class="form-group">
                    <v-layout>
                        <div style="display: flex; flex-direction: row; justify-content: center; width: 100%;">
                            <v-btn v-on:click="api_upload()" class="ml-0">{{ button }}</v-btn>
                        </div>
                    </v-layout>
                </div>
            </form>
            <div v-if="upload_error" class="upload-error">{{ upload_error }}</div>
        </v-card-text>
    </v-card>
</template>

<script>
function file_ok() {
    console.log("In file_ok");
    return false;
}

var upload_config = {
    onUploadProgress: function(progressEvent) {
        var percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
        console.log("Upload percent: " + percentCompleted);
    },
};

const mymodule = {
    props: {
        url: { type: String, required: true },
        title: { type: String, default: "Upload File" },
        button: { type: String, default: "Upload" },
        extensions: { type: String, default: ".txt,.jpg" },
        owner: { type: String },
        tag: { type: String },
        description: { type: String, default: "no description" },
    },
    data: function() {
        return {
            upload_error: undefined,
            valid_extensions: [],
        };
    },
    created: function() {
        console.log("CREATED");
        let self = this;
        let exts = this.extensions.split(",");
        exts.forEach(function(x) {
            x = x.trim();
            if (x.length > 0 && x.startsWith(".")) self.valid_extensions.push(x);
        });
        console.log("Valid extensions: ", this.valid_extensions);
    },
    methods: {
        on_input_change: function(e) {
            let files = e.target.files;
            if (files.length == 1) {
                let filename = files[0].name;
                console.log("on_input_change: " + filename, e);
            }
        },
        on_submit: function() {
            let filename = document.getElementById("file-input").value;
            console.log('Filename: "' + filename + '"');
            if (filename != undefined && filename.length > 0) {
                if (this.is_valid_filename(filename)) {
                    document.forms["upload-form"].submit();
                } else this.upload_error = "File extension must be " + this.valid_extensions.join(" or ");
            } else this.upload_error = "No file selected";
        },
        is_valid_filename: function(name) {
            name = name.toLowerCase();
            //console.log("Checking filename " + name);
            let valid = false;
            this.valid_extensions.forEach(function(x) {
                //console.log("    Checking " + x);
                valid = valid || name.endsWith(x);
            });
            return valid;
        },
        api_upload: function() {
            let self = this;
            var data = new FormData();
            if (this.is_valid_filename(document.getElementById("file-input").value)) {
                let url = this.url;
                data.append("owner", this.owner);
                data.append("tag", this.tag);
                data.append("description", this.description);
                data.append("files", document.getElementById("file-input").files[0]);
                console.log("Uploading to " + url, data);
                axios
                    .put(url, data, upload_config)
                    .then(function(res) {
                        console.log("Upload complete:", res);
                        let ajax_result = res.data;
                        self.$emit("complete", ajax_result);
                    })
                    .catch(function(err) {
                        console.error("Upload error: ", err);
                        self.$emit("error", err);
                    });
            } else this.upload_error = "File extension must be " + this.valid_extensions.join(" or ");
        },
    },
};
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["Upload"] = mymodule;
else window.VueComponents = { Upload: mymodule };
</script>

<style>
.upload-error {
    color: yellow;
    font-weight: bold;
    font-size: larger;
    width: 100%;
    text-align: center;
    margin-top: 1rem;
}
</style>
