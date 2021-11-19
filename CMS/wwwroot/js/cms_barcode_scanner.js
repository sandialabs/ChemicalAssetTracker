/**********************************************************************
* 
* Global variables
* 
**********************************************************************/
var resultAccumulator = 0; //Accumulator to track result index
var bcResults = [];   //Array holding successful barcode results

/**********************************************************************
 * Read barcode from live feed
 * 
 * This function is responsible for scanning a barcode using a live feed
 * from a phones camera. Available barcode readers are: code_128_reader, 
 * ean_reader, ean_8_reader, code_39_reader, code_39_vin_reader, 
 * codabar_reader, upc_reader, upc_e_reader, i2of5_reader, 2of5_reader, 
 * and code_93_reader
 * 
 *********************************************************************/

function activate_barcode_scanner(conditions) {
    //Deactivate scanner if inactive
    var activityTimeout = setTimeout(function () {
        Quagga.stop();
        conditions.ontimeout(true);
    }, conditions.timeout);
    //Cancel scanning and break out of function if stop button is pressed
    if (conditions === false) {
        clearTimeout(activityTimeout);
        Quagga.stop();
        return 0;
    }

    //Initialize barcode scanning library
    Quagga.init({
        inputStream: {
            name: "Live",
            type: "LiveStream", //Indicates live feed will be scanned
            size: 1200, //Width of picture in pixels  
            constraints: {
                facingMode: "environment" //"environment" for  rear camera, "user" for front
            }
        },
        locator: {
            halfSample: true, //Controls whether image resolution is halved
            patchSize: "medium" //Density of barcode search grid
        },
        numOfWorkers: 2, //(navigator.hardwareConcurrency ? navigator.hardwareConcurrency : 4),
        frequency: 10, //Amount of scans per second
        decoder: {
            readers: [{
                format: "code_128_reader", //Type of barcode being scanned
                config: {}
            }]
        },
        locate: true //Automatically locate a barcode within a live feed
    }, function (err) {
        //If the camera cannot be accessed, relay error message to user
        if (err) {
            clearTimeout(activityTimeout);
            conditions.onerror("Cannot access camera");
        }
        //If camera is accessed, call function to set camera zoom and start scanning library
        else {
            set_camera_zoom();
            Quagga.start();
        }
    }
    );
    //Draw boxes around barcode to show user successful scan
    Quagga.onProcessed(function (result) {
        var drawingCtx = Quagga.canvas.ctx.overlay,
            drawingCanvas = Quagga.canvas.dom.overlay;
        if (result) {
            if (result.boxes) {
                drawingCtx.clearRect(0, 0, parseInt(drawingCanvas.getAttribute("width")), parseInt(drawingCanvas.getAttribute("height")));
                result.boxes.filter(function (box) {
                    return box !== result.box;
                }).forEach(function (box) {
                    //Green boxes indicate recognized barcode, but unsuccessful scan
                    Quagga.ImageDebug.drawPath(box, { x: 0, y: 1 }, drawingCtx, { color: "green", lineWidth: 2 });
                });
            }
            if (result.box) {
                //Blue box indicates recognized barcode, and successful scan
                Quagga.ImageDebug.drawPath(result.box, { x: 0, y: 1 }, drawingCtx, { color: "#00F", lineWidth: 2 });
            }
            if (result.codeResult && result.codeResult.code) {
                //Display red line to show where barcode was read
                Quagga.ImageDebug.drawPath(result.line, { x: 'x', y: 'y' }, drawingCtx, { color: 'red', lineWidth: 3 });
            }
        }
    });
    //Handle successful barcode scans
    Quagga.onDetected(function (result) {
        if (result.codeResult.code && result.codeResult.code.length > 6) {
            //Reset inactivity timer
            clearTimeout(activityTimeout);
            activityTimeout = setTimeout(function () {
                Quagga.stop();
                conditions.ontimeout(true);
            }, conditions.timeout);
            if (result.codeResult.code !== bcResults[resultAccumulator - 1] || resultAccumulator === 0) {
                //window.navigator.vibrate(50); //Vibrate phone when barcode is stored
                conditions.onsuccess(result.codeResult.code); //Return stored barcode to callback function
            }
            if (conditions.continuous === false) {
                conditions.continuous = true; //Reset continuous flag
                clearTimeout(activityTimeout); //Reset inactivity timer
                Quagga.stop(); //Stop scanning
            }
        }
    });
}

/**********************************************************************
 * 
 * Set phone camera zoom  
 * 
 *********************************************************************/
function set_camera_zoom() {
    //Get active streaming source for video
    var track = Quagga.CameraAccess.getActiveTrack();
    var capabilities = {};
    //Acquire camera capabilities of the phone
    if (typeof track.getCapabilities === 'function') {
        capabilities = track.getCapabilities();
    }
    //Set zoom on phone based on acquired capabilities
    if (capabilities.zoom) {
        track.applyConstraints({
            //Zoom will be set to 2 times the minimum setting
            advanced: [{ zoom: (2 * capabilities.zoom.min) }]
        })
            .catch(e => console.log(e));
    }
}