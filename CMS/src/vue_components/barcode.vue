<template>
    <v-dialog id="barcode-dialog" v-model="show_barcode_dialog" style="top=20px;">
        <v-card>
            <v-card-title class="headline">Barcode Scan</v-card-title>
            <v-card-text class="error-message">{{ message }}</v-card-text>
            <v-card-text>
                <!-- DIV below is for live image dispay - don't change id or class -->
                <div id="interactive" class="viewport"></div>
            </v-card-text>
            <v-card-actions>
                <v-btn @click="on_stop_barcode()">Stop</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script>
console.log("Loading barcode.vue");

let mymodule = {
    props: {
        debug: {
            type: Boolean,
            required: false,
            default: false,
        },
        timeout: {
            type: Number,
            required: false,
            default: 60000,
        },
        // callback: {
        //     type: Function,
        //     required: false
        // },
    },
    data: function() {
        return {
            show_barcode_dialog: false,
            message: "",
        };
    },
    created: function() {
        console.log("In barcode.vue.created - debug is " + this.debug + ", timeout is " + this.timeout);
    },
    mounted: function() {
        console.log("In barcode.vue.mounted - debug is " + this.debug + ", timeout is " + this.timeout);
    },
    methods: {
        scan: function(is_continuous) {
            this.message = "Scanning ...";
            this.show_barcode_dialog = true;
            this.read_camera_barcode(is_continuous);
        },

        stop_quagga: function() {
            console.log("-------------------------------------");
            console.log("- STOPPING QUAGGA                   -");
            console.log("-------------------------------------");
            Quagga.stop();
        },

        on_barcode_results: function(result) {
            if (result.success) {
                this.$emit("barcode", result.result);
            } else {
                this.message = result.message;
            }
        },
        on_stop_barcode: function() {
            console.log("Closing barcode reader");
            this.stop_quagga();
            this.show_barcode_dialog = false;
        },

        read_camera_barcode(isContinuous) {
            let self = this;
            // call library function to do the work and call me back
            activate_barcode_scanner({
                //Determines single barcode or multiple barcodes being scanned
                continuous: isContinuous,
                //Allowed time in milliseconds between succesful scans
                timeout: this.timeout,
                //Determines if a successful scan has occured, also stores and displays barcodes
                onsuccess: function(result) {
                    var isScanned = false;
                    if (result) {
                        isScanned = true;
                    }
                    //Display result to user (NOT included in cms_barcode_scanner.js file)
                    console.log("barcode.vue:read_camera_barcode: have barcode result", result);
                    self.on_barcode_results({ success: true, message: "", result: result });

                    if (!isContinuous) {
                        self.stop_quagga();
                        self.show_barcode_dialog = false;
                    }
                    //display_barcode(result);
                    //Array used to store scanned barcodes (included in cms_barcode_scanner.js file)
                    //bcResults[resultAccumulator] = result;
                    //Increment result array element accumulator (included in cms_barcode_scanner.js file)
                    //resultAccumulator++;

                    // Note: set continuous to false to stop continuous scanning

                    return isScanned;
                },
                //Determine if live scanning has ended due to inactivity
                ontimeout: function(timeFlag) {
                    var isTimeout = false;
                    if (timeFlag) {
                        isTimeout = true;
                    }
                    //Display timeout error to user (NOT included in cms_barcode_scanner.js file)
                    //display_barcode("TIMEOUT");
                    //Halt barcode scanning library
                    console.log("barcode.vue:read_camera_barcode: TIMEOUT");
                    self.stop_quagga(); // PH: double check why this is necessary
                    self.show_barcode_dialog = false;
                    self.on_barcode_results({ success: false, message: "TIMEOUT", result: null });
                    return isTimeout;
                },
                //Determine if an error occored with accessing the camera
                onerror: function(errormessage) {
                    var errorFlag = false;
                    if (errormessage) {
                        var errorFlag = true;
                    }
                    //relay error message to console
                    console.error("barcode.vue:read_camera_barcode: " + errormessage);
                    //Display error to user (NOT included in cms_barcode_scanner.js file)
                    //display_barcode("FAILED");
                    //Halt barcode scanning library
                    self.stop_quagga(); // PH: double check why this is necessary
                    self.show_barcode_dialog = false;
                    self.on_barcode_results({ success: false, message: errormessage, result: null });
                    return errorFlag;
                },
            });
        },
    },
};
module.exports = mymodule;

if (window.VueComponents) window.VueComponents["Barcode"] = mymodule;
else window.VueComponents = { Barcode: mymodule };
</script>

<style>
#interactive.viewport {
    width: 640px;
    height: 480px;
}

#interactive.viewport canvas,
video {
    width: 640px;
    height: 480px;
}

#interactive.viewport canvas.drawingBuffer,
video.drawingBuffer {
    position: absolute;
    left: 20px;
}

@@media only screen and (min-width: 375px) and (max-width: 700px) {
    #interactive.viewport {
        width: 300px;
        height: 225px;
        left: 0px;
    }

    #interactive.viewport canvas,
    video {
        width: 300px;
        height: 225px;
        left: 0px;
    }

    #interactive.viewport canvas.drawingBuffer,
    video.drawingBuffer {
        position: absolute;
        left: 0px;
    }
}

@@media only screen and (max-width: 374px) {
    #interactive.viewport {
        width: 200px;
        height: 150px;
        left: 0px;
    }

    #interactive.viewport canvas,
    video {
        width: 200px;
        height: 150px;
        left: 0px;
    }

    #interactive.viewport canvas.drawingBuffer,
    video.drawingBuffer {
        position: absolute;
        left: 0px;
    }
}
</style>
