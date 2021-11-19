//#####################################################################
//
// index.js - dummy JavaScript file used by webpack
//
//#####################################################################

//import "promise-polyfill/src/polyfill";
//import { axios } from "axios";
(function() {
    window.axios = require("axios");
    window.ulog = require("ulog");
    window.api = require("./api");
    window.utils = require("./utils");
    window.site = require("./site");
    window.moment = require("moment");
    window.numeral = require("numeral");
    console.log("Bundle loaded");
})();
