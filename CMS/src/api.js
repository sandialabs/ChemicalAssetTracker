//#####################################################################
//
// Browser Session Storage functions
//
//
//#####################################################################

export function store_local_data(name, data) {
    if (typeof Storage !== "undefined") {
        sessionStorage[name] = JSON.stringify(data);
    }
}

export function fetch_local_data(name) {
    if (typeof Storage !== "undefined") {
        let value = sessionStorage[name];
        if (value) return JSON.parse(value);
        else return undefined;
    }
    return undefined;
}

export function record_task_time(task, ms) {
    let key = "tasktime";
    let stats = fetch_local_data(key);
    if (stats) {
        //console.log("Have timing stats", stats);
        let taskvalues = stats[task];
        if (taskvalues) {
            taskvalues.samplecount += 1;
            taskvalues.totaltime += ms;
            taskvalues.mintime = Math.min(taskvalues.mintime, ms);
            taskvalues.maxtime = Math.max(taskvalues.maxtime, ms);
            taskvalues.meantime = taskvalues.totaltime / taskvalues.samplecount;
        } else {
            stats[task] = { samplecount: 1, totaltime: ms, mintime: ms, maxtime: ms, meantime: ms };
        }
        store_local_data(key, stats);
    } else {
        let timings = {};
        timings[task] = { samplecount: 1, totaltime: ms, mintime: ms, maxtime: ms, meantime: ms };
        store_local_data(key, timings);
    }
}

export function get_timing_stats() {
    return fetch_local_data("tasktime");
}

//#####################################################################
//
// CMS Database Interface
//
// Return values will be an object with three properties:
//     Success - true or false
//     Message - a status message
//     Data    - the data returned by the server, or undefined
//
//#####################################################################

//---------------------------------------------------------------------
//
// axios_get - can a server api GET method that returns an AjaxResult
//
// settings should look like this.
// {
//     url: <url to call>,
//     caller:    'my_function_name',
//     verbose:   true/false,
//     onsuccess: function(ajax_result) { ... },
//     onfailure: function(ajax_result) { ... },
//     onerror:   function(response) { ... }
// }
//
// The success and failure functions will be called if the API returns
// an AjaxResult with Success == false.  Either of these can be undefined
// and the corresponding result will just be ignored.
//
// If error is not defined and an exception is thrown, a message will
// be logged to the console.
//
//----------------------------------------------------------------------

const axios = require("axios");
const utilities = require("./utils");

export const CWC = 0;
export const THEFT = 1;
export const OTHERSECURITY = 2;
export const CARCINOGEN = 3;
export const HEALTHHAZARD = 4;
export const IRRITANT = 5;
export const ACUTETOXICITY = 6;
export const CORROSIVE = 7;
export const EXPLOSIVE = 8;
export const FLAMABLE = 9;
export const OXIDIZER = 10;
export const COMPRESSEDGAS = 11;
export const OTHERHAZARD = 12;

function is_x(val) {
    let result = val == "x" || val == "X";
    return result;
}

function to_x(val) {
    let result = " ";
    if (val) result = "X";
    return result;
}

export function set_item_boolean_flags(source_flags, target_flags) {
    target_flags.ACUTETOXICITY = is_x(source_flags.ACUTETOXICITY);
    target_flags.CARCINOGEN = is_x(source_flags.CARCINOGEN);
    target_flags.COMPRESSEDGAS = is_x(source_flags.COMPRESSEDGAS);
    target_flags.CORROSIVE = is_x(source_flags.CORROSIVE);
    target_flags.EXPLOSIVE = is_x(source_flags.EXPLOSIVE);
    target_flags.FLAMABLE = is_x(source_flags.FLAMABLE);
    target_flags.HEALTHHAZARD = is_x(source_flags.HEALTHHAZARD);
    target_flags.IRRITANT = is_x(source_flags.IRRITANT);
    target_flags.OTHERHAZARD = is_x(source_flags.OTHERHAZARD);
    target_flags.OTHERSECURITY = is_x(source_flags.OTHERSECURITY);
    target_flags.OXIDIZER = is_x(source_flags.OXIDIZER);
    target_flags.THEFT = is_x(source_flags.THEFT);
    return target_flags;
}

export function set_item_char_flags(source_flags, target_flags) {
    target_flags.ACUTETOXICITY = to_x(source_flags.ACUTETOXICITY);
    target_flags.CARCINOGEN = to_x(source_flags.CARCINOGEN);
    target_flags.COMPRESSEDGAS = to_x(source_flags.COMPRESSEDGAS);
    target_flags.CORROSIVE = to_x(source_flags.CORROSIVE);
    target_flags.EXPLOSIVE = to_x(source_flags.EXPLOSIVE);
    target_flags.FLAMABLE = to_x(source_flags.FLAMABLE);
    target_flags.HEALTHHAZARD = to_x(source_flags.HEALTHHAZARD);
    target_flags.IRRITANT = to_x(source_flags.IRRITANT);
    target_flags.OTHERHAZARD = to_x(source_flags.OTHERHAZARD);
    target_flags.OTHERSECURITY = to_x(source_flags.OTHERSECURITY);
    target_flags.OXIDIZER = to_x(source_flags.OXIDIZER);
    target_flags.THEFT = to_x(source_flags.THEFT);
    return target_flags;
}

export function axios_get(settings) {
    if (settings.verbose) console.log('Calling "' + settings.url + '" ...');
    axios({
        method: "GET",
        url: settings.url,
    })
        .then(function(response) {
            let ajax_result = response.data;
            if (settings.verbose) {
                console.log('Have AjaxResult from "' + settings.url + '":', ajax_result);
            }
            if (ajax_result.Success) {
                if (ajax_result.TaskTime) {
                    let tt = parseFloat(ajax_result.TaskTime);
                    let task = "axios_get: " + settings.url;
                    record_task_time(task, tt);
                }
                if (settings.onsuccess) settings.onsuccess(ajax_result);
            } else {
                if (settings.onfailure) settings.onfailure(ajax_result);
                else console.log(settings.caller + ': call to "' + settings.url + '" did not succeed: ', ajax_result.Message);
            }
        })
        .catch(function(response) {
            if (settings.onerror) settings.onerror(response);
            else console.log(settings.caller + ': call to "' + settings.url + '" failed: ', response);
        });
}

export function axios_delete(settings) {
    if (settings.verbose) console.log('Calling DELETE on "' + settings.url + '" ...');
    axios({
        url: settings.url + "?id=" + settings.id,
        data: null,
        method: "delete",
        headers: { "Content-Type": "application/json" },
    })
        .then(function(response) {
            let ajax_result = response.data;
            if (settings.verbose) {
                console.log('Have AjaxResult from "' + settings.url + '":', ajax_result);
            }
            if (ajax_result.Success) {
                if (ajax_result.TaskTime) {
                    let tt = parseFloat(ajax_result.TaskTime);
                    let task = "axios_delete: " + settings.url;
                    record_task_time(task, tt);
                }
                if (settings.onsuccess) settings.onsuccess(ajax_result);
            } else {
                if (settings.onfailure) settings.onfailure(ajax_result);
                else console.log(settings.caller + ': call to "' + settings.url + '" did not succeed: ', ajax_result.Message);
            }
        })
        .catch(function(response) {
            if (settings.onerror) settings.onerror(response);
            else console.log(settings.caller + ': call to "' + settings.url + '" failed: ', response);
        });
}

export function axios_post(settings) {
    if (settings.verbose) console.log('Posting to "' + settings.url + '" ...');
    axios({
        method: "POST",
        url: settings.url,
        data: settings.data,
    })
        .then(function(response) {
            let ajax_result = response.data;
            if (settings.verbose) {
                console.log('Have AjaxResult from "' + settings.url + '":', ajax_result);
            }
            if (ajax_result.Success) {
                if (ajax_result.TaskTime) {
                    let tt = parseFloat(ajax_result.TaskTime);
                    let task = "axios_post: " + settings.url;
                    record_task_time(task, tt);
                }
                if (settings.onsuccess) settings.onsuccess(ajax_result);
            } else {
                if (settings.onfailure) settings.onfailure(ajax_result);
                else console.log(settings.caller + ': call to "' + settings.url + '" did not succeed: ', ajax_result.Message);
            }
        })
        .catch(function(response) {
            if (settings.onerror) settings.onerror(response);
            else console.log(settings.caller + ': call to "' + settings.url + '" failed: ', response);
        });
}

//#####################################################################
//
// INVENTORY MANAGEMENT
//
//#####################################################################

export function jump_to_inventory_detail(barcode) {
    let encoded_barcode = barcode.replace("#", "_HASH_");
    console.log('Encoded barcode is "' + encoded_barcode + '"');
    jump_to_page("Home", "InventoryDetail", encoded_barcode);
}

export function process_inventory_items(inventory) {
    if (!inventory) return "noclass";
    inventory.forEach(function(x) {
        if (x.Location) {
            x.LocationName = x.Location.Name;
        } else x.LocationName = "";
        x.Class = function(i, color) {
            if (x.DisplayFlags[i] !== " ") return color;
            else return "noclass";
        };
    });
}

export function fetch_inventory(root_location_id, callback) {
    console.log("In fetch_inventory()", root_location_id);
    let url = utilities.api_url("inventory") + "/" + root_location_id;
    console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            console.log("Have result from " + url, result);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_inventory";
                record_task_time(task, tt);
            }
            let inventory = ajax_result.Data.Inventory;
            process_inventory_items(inventory);
            callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
        })
        .catch(function(error) {
            console.log(error);
        });
}

export function fetch_local_inventory(root_location_id, callback) {
    console.log("In fetch_local_inventory()", root_location_id);
    let url = utilities.api_url("localinventory") + "/" + root_location_id;
    console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            console.log("Have result from " + url, result);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_local_inventory";
                record_task_time(task, tt);
            }
            let inventory = ajax_result.Data.Inventory;
            process_inventory_items(inventory);
            callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
        })
        .catch(function(error) {
            console.log(error);
        });
}

export function search_inventory(search_settings, callback) {
    console.log("In search_inventory()", search_settings);
    let url = utilities.api_url("inventorysearch");
    console.log("Calling " + url);
    var self = this;

    axios({
        method: "POST",
        url: url,
        data: search_settings,
    })
        .then(function(result) {
            console.log("Have result from " + url, result);
            console.log("Inventory search took " + result.data.TaskTime);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "search_inventory";
                record_task_time(task, tt);
            }
            let inventory = ajax_result.Data.Inventory;
            try {
                process_inventory_items(inventory);
                callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
            } catch (error) {
                console.error("Error", error);
                callback(create_return_info(false, ajax_result.Message, ajax_result.Data));
            }
        })
        .catch(function(error) {
            console.log(error);
        });
}

//#####################################################################
//
// Function:            fetch_inventory_item
// Author:              Pete Humphrey
// Description:         Get an inventory item using its barcode
//
// Params:
// barcode:             the barcode of the item
// callback:            a function to call when operation completes
//
// If the operation succeeds, the callback will be called with
// an object of the following form.
//     {
//         Success: true/false,
//         Data:    the inventory item data, or nothing
//                  Data.Item - the inventory item data
//                  Data.Owners - owner data
//                  Data.Locations - location data
//                  Data.Groups - group data
//         Message: status message
//     }
//
//#####################################################################
export function fetch_inventory_item(barcode, callback) {
    console.log('In fetch_inventory_item("' + barcode + '")');
    let url = utilities.api_url("fetchitem") + "/" + barcode;
    var self = this;
    axios_get({
        url: url,
        caller: "fetch_inventory_item",
        verbose: true,
        onsuccess: function(ajax_result) {
            self.edit_item = ajax_result.Data.Item;
            callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
        },
        onfailure: function(ajax_result) {
            callback(create_return_info(false, ajax_result.Message, undefined));
        },
    });
}

//#####################################################################
//
// Function:            save_inventory_item
// Author:              Pete Humphrey
// Description:         Store an inventory into the database
//
// item:                the inventory item to save
// callback:            a function to call when operation completes
//
// If the operation succeeds, the callback will be called with
// an object of the following form.
//     {
//         Success: true/false,
//         Data:    the updated inventory item,
//         Message: status message
//     }
//
//#####################################################################

export function save_inventory_item(item, callback) {
    let url = utilities.api_url("update_item");
    console.log("Calling " + url);
    //console.log("item:", item);
    let self = this;
    axios({
        method: "POST",
        url: url,
        data: item,
    })
        .then(function(response) {
            console.log("Response:", response);
            let AjaxResult = response.data;
            if (AjaxResult.Success) {
                if (AjaxResult.TaskTime) {
                    let tt = parseFloat(AjaxResult.TaskTime);
                    let task = "save_inventory_item";
                    record_task_time(task, tt);
                }
                let updated_item = AjaxResult.Data.UpdatedItem;
                console.log("SAVED");
                callback(create_return_info(true, AjaxResult.Message, updated_item));
            } else {
                callback(create_return_info(false, AjaxResult.Message, null));
            }
        })
        .catch(function(response) {
            console.log("Response:", response);
            callback(create_return_info(false, "HTTP Error", null));
        });
}

export function new_inventory_item_template() {
    let result = {
        InventoryID: 0,
        Barcode: "",
        CASNumber: "",
        ChemicalName: "",
        ContainerSize: null,
        DateIn: new Date(),
        ExpirationDate: null,
        ItemFlags: {},
        DisplayFlags: "                ",
        Group: "",
        GroupID: null,
        InventoryID: 0,
        InventoryStatusID: null,
        InventoryStatus: null,
        Location: null,
        LocationID: null,
        LocationName: null,
        Notes: null,
        Owner: null,
        OwnerID: null,
        RemainingQuantity: null,
        SDS: "",
        State: "",
        StockCheckLocation: "",
        Units: "",
        date_in: moment().format("YYYY-MM-DD"),
        expiration_date: null,
    };
    return result;
}

export function fetch_settings(callback) {
    console.log("In fetch_settings()");
    let url = utilities.api_url("settings");
    console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            console.log("Have result from " + url, result);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_settings";
                record_task_time(task, tt);
            }
            callback(ajax_result);
        })
        .catch(function(error) {
            console.log(error);
        });
}

export function fetch_user_settings(callback) {
    //console.log("In fetch_user_settings()");
    let url = utilities.api_url("usersettings");
    //console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            //console.log("Have result from " + url, result);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_user_settings";
                record_task_time(task, tt);
            }
            if (callback) callback(ajax_result);
        })
        .catch(function(error) {
            console.log(error);
        });
}

//#####################################################################
//
// LOCATION MANAGEMENT
//
//#####################################################################

export function fetch_locations(callback) {
    console.log("In fetch_locations()");
    let url = utilities.api_url("getlocations");
    console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            console.log("Have result from " + url, result);
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_locations";
                record_task_time(task, tt);
            }
            callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
        })
        .catch(function(error) {
            console.log(error);
        });
}

export function fetch_subtree(root_location_id, depth, callback) {
    //console.log("In fetch_subtree");
    let url = utilities.api_url("getuserlocationsubtree");
    url += "/*/" + root_location_id + "/" + depth;
    //console.log("Calling " + url);
    var self = this;
    axios({
        method: "GET",
        url: url,
    })
        .then(function(result) {
            let ajax_result = result.data;
            if (ajax_result.TaskTime) {
                let tt = parseFloat(ajax_result.TaskTime);
                let task = "fetch_subtree";
                record_task_time(task, tt);
            }
            //console.log("Have result from " + url, result);
            //console.log("API call took " + ajax_result.TaskTime);
            callback(create_return_info(true, ajax_result.Message, ajax_result.Data));
        })
        .catch(function(error) {
            console.log(error);
        });
}

export function create_return_info(is_successful, message, data) {
    if (!message) {
        if (is_successful) message = "Successful";
        else message = "FAILED";
    }
    return {
        Success: is_successful,
        Data: data,
        Message: message,
    };
}
