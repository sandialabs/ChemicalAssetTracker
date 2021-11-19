export function root_url() {
    let appname = "CMS";
    let result = window.location.href;
    let pos = result.indexOf(appname);
    if (pos > 0) result = result.substr(0, pos + appname.length) + "/";
    // http://wherever.sandia.gov:1234/AppName/
    else result = window.location.protocol + "//" + window.location.host + "/";
    console.log("root_url", result);
    return result;
}

export function api_url(method) {
    var result = root_url() + "api/" + method;
    return result;
}

export function is_mobile() {
    return /Mobi|Android/i.test(navigator.userAgent);
}

export function mvc_page_url(controller, action, suffix) {
    var prefix = root_url();
    var url = prefix + controller + "/" + action;
    if (suffix) url = url + "/" + suffix;
    return url;
}

export function jump_to_page(controller, action, suffix) {
    var prefix = root_url();
    var url = mvc_page_url(controller, action, suffix);
    window.location.href = url;
}

export function go_to_page(relative_url) {
    var prefix = root_url();
    var url = prefix + relative_url;
    window.location.href = url;
}

export function xxx_go_home() {
    document.location = root_url() + "Home/Index";
}

export function get_url_parameter(param_name) {
    let url = new URL(window.location.href);
    return url.searchParams.get(param_name);
}

//#####################################################################
//
// filter_to_regex - convert a filter expression to a regular expression
//
// E.g. "help*me" => /help.*me/
//
//#####################################################################

export function filter_to_regex(filter) {
    let regex = filter.replace(".", "\\.").replace("?", ".?");
    regex = regex.replace(/\*/gi, ".*");
    return new RegExp(regex, "i");
}

export function foreach(array, func) {
    var i;
    for (i = 0; i < array.length; i++) {
        var e = array[i];
        func(e);
    }
    return array;
}

export function contains(collection, item) {
    if (collection) {
        if (Array.isArray(collection)) {
            return collection.indexOf(item) >= 0;
        }
        if (typeof collection === "object") {
            if (collection[item]) return true;
        }
    }
    return false;
}

export function deep_copy(obj) {
    return jQuery.extend(true, {}, obj);
}

export function deep_clone(obj) {
    return JSON.parse(JSON.stringify(obj));
}

export function copy_object(from, to) {
    for (var propname in from) {
        if (from.hasOwnProperty(propname)) {
            to[propname] = from[propname];
        }
    }
}

export function format_date(value) {
    if (typeof value === "undefined" || value === null) return "";
    else return moment(value).format("YYYY-MM-DD");
}

export function format_today(value) {
    return format_date(new Date());
}



export function dump(obj, name) {
    if (name) console.log(name);
    console.log(JSON.stringify(obj, null, 4));
}

export function has_role(allroles, required_roles) {
    let result = false;
    required_roles.forEach(function (r) { if (allroles.indexOf(r) >= 0) result = true; });
    return result;
}

export function pprint(obj) {
    console.log(JSON.stringify(obj, 0, 4))
}

export function pretty_print(obj) {
    if (typeof obj == 'string') console.log(obj);
    else {
        let jsonstr = JSON.stringify(obj, null, 4);
        console.log(jsonstr);
    }
}

var casnumber_regex = new RegExp("^\\d+\\-\\d+\\-\\d+$") 
export function is_cas_number(str) {
    return casnumber_regex.exec(str);
}
