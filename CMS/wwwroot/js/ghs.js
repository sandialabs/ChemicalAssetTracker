var GHS_Pictogram_Data = [
    { image_file: "explosive.png", name: "Exploding Bomb", description: "Explosives", id: "GHS01" },
    { image_file: "flamable.png", name: "Flame", description: "Flammables", id: "GHS02" },
    { image_file: "oxidizer.png", name: "Flame Over Circle", description: "Oxidizers", id: "GHS03" },
    { image_file: "compressed_gas.png", name: "Gas Cylinder", description: "Compressed Gasses", id: "GHS04" },
    { image_file: "corrosive.png", name: "Corrosion", description: "Corrosives", id: "GHS05" },
    { image_file: "acute_toxicity.png", name: "Skull and Crossbones", description: "Acute Toxicity", id: "GHS06" },
    { image_file: "irritant.png", name: "Exclamation Mark", description: "Irritant", id: "GHS07" },
    { image_file: "health_hazard.png", name: "Health Hazard", description: "Health Hazard", id: "GHS08" },
    { image_file: "environmental.jpg", name: "Environment", description: "Environment", id: "GHS09" },
];

function get_hazard_information(callback) {
    let storage = window.sessionStorage;
    let hazard_tables = storage.getItem("hazard_tables")
    if (!hazard_tables) {
        let url = utils.api_url("gethazardtables");
        console.log("get_hazard_information: calling " + url)
        api.axios_get({
            url: url,
            onsuccess: function (ajax_result) {
                console.log("get_hazard_information - have result:", ajax_result);
                hazard_tables = ajax_result.Data.Hazards;
                storage.setItem("hazard_tables", JSON.stringify(hazard_tables));
                if (callback) callback(hazard_tables);
            }
        });
    }
    else {
        if (callback) callback(JSON.parse(hazard_tables));
    }
}

function get_hazard_information_for_casnumber(casnumber, callback) {
    get_hazard_information(function (hazard_tables) {
        if (callback) callback(hazard_tables[casnumber]);
    })
}

function show_hazard_information_for_casnumber(casnumber) {
    get_hazard_information_for_casnumber(casnumber, (result) => {
        if (result) {
            console.log("Hazard information for " + casnumber);
            Object.keys(result).forEach((key) => console.log("    " + key + " = " + result[key]));
        }
        else console.log("No hazard information for " + casnumber + " found");
    });
}