//#####################################################################
//
// VueGrid
//
// Input
//     { el: '#domelement',
//       outer_class: [<name> ...],  // defaults to 'grid-div'
//       table_class: [<name> ...],
//       filtered: true|false,
//       columns: [ ...],
//       data_source: func
//     }
//
// Column definition:
//     { width: <n>,            // relative width of the column
//       min_width: <n>         // minimum width of the column in pixels
//       max_width: <n>         // maximul width of the column in pixels
//       header: <string>,
//       field: <string>,
//       filtered: true|false
//     }
//
// The data_source function will be called to supply the row data
// for the grid.  It is passed a callback function which expects
// a single argument, that is a structure containing a Data field that
// holds a list of structures, one for each row.  The fields of these
// inner structures should correspond to the field names you reference
// in your column definitions.
//
//#####################################################################

create_vuegrid = 
(function() {
var vue_resize_timer;
var all_vues = [];
var is_verbose = false;
var _vue_grid_template;

Math.trunc = Math.trunc || function (x) {
    if (isNaN(x)) {
        return NaN;
    }
    if (x > 0) {
        return Math.floor(x);
    }
    return Math.ceil(x);
};

function sort_rows(rows, field, direction) 
{
    rows.sort(function(x, y) {
        let result = 0;
        let v1 = x[field];
        let v2 = y[field];
        if (v1 > v2) result = 1;
        if (v1 < v2) result = -1;
        if (direction == 'descending') result = 0 - result;
        return result;
    });
}

function get_element_width(id, is_verbose) {
    var element = document.getElementById(id);
    var styles = window.getComputedStyle(element);
    var padding = parseFloat(styles.paddingLeft) +
                  parseFloat(styles.paddingRight);
  
    if (is_verbose) console.log(id, "client_width", element.clientWidth, "padding", padding)
    return element.clientWidth - padding;
  }

function create_vuegrid(defs) 
{
    if (defs.verbose) console.log(defs);
    var dom_element = '#' + defs.el;

    let el = document.getElementById(defs.el);
    if (!el) {
        console.error("VueGrid - cannot locate a DOM element with id " + defs.el);
        return;
    }

    el.innerHTML = _vue_grid_template;

    var column_definitions = defs.columns;
    var data_url = defs.data_url;
    var response_handler = defs.response_handler;
    var is_verbose = (defs.verbose == true);

    // define the Vue data
    var vuegrid_data = {};
    if (defs.data) vuegrid_data = defs.data;
    vuegrid_data["element_id"] = defs.el;
    vuegrid_data["DomElement"] = dom_element;
    vuegrid_data["TableDefinition"] = {
        Columns: column_definitions,
        Data: null
    };
    vuegrid_data.sort_column = undefined;
    vuegrid_data.sort_direction = 'ascending';
    vuegrid_data.saved_data = [];
    vuegrid_data.header = defs.header;
    vuegrid_data.outer_class = defs.outer_class ? defs.outer_class : ["vuegrid-table"];
    vuegrid_data.table_class = defs.table_class ? defs.table_class : ["vuegrid-table"];
    vuegrid_data.filtered = defs.filtered ? defs.filtered : false;

    if (!defs.handle_row_click) {
        defs.handle_row_click = function (row, ev) {
            if (is_verbose) console.log("DEFAULT ROW CLICK");
        }
    }

    for (let i in vuegrid_data.TableDefinition.Columns) {
        let col = vuegrid_data.TableDefinition.Columns[i];
        //col["sort_order"] = ["invisible"]; // "fa", "fa-sort-up"
        col.pixel_width = 10;  
    }

    if (is_verbose) console.log(vuegrid_data);

    var vuegrid = new Vue({
        el: dom_element,
        data: vuegrid_data,
        created: function() {
            if (is_verbose) console.log("Grid Vue created for " + dom_element);
            this.prepare_columns();
            this.resize();
            let self = this;
            this.data_source(function (result) {
                if (is_verbose) console.log("Have response from data source", result.Message);
                if (result.Success) {
                    if (is_verbose) console.log("Grid data:", result.Data);
                    self.TableDefinition.Data = self.prepare_data(result.Data);
                    self.saved_data = self.TableDefinition.Data.slice(0);
                    self.resize();
                }
            });
        },
        methods: {
            data_source: defs.data_source,
            handle_row_click: defs.handle_row_click,
            prepare_columns: function() {
                if (is_verbose) console.log("In prepare_columns", this.TableDefinition);
                let total_width = 0;
                let columns = this.TableDefinition.Columns;
                for (let i in this.TableDefinition.Columns) {
                    let col = columns[i]
                    total_width += col.width
                }
                //if (is_verbose) console.log("Total logical width", total_width);
                for (let i in this.TableDefinition.Columns) {
                    let col = columns[i]
                    col["filter"] = "";
                    col["width_percent"] = col.width / total_width;
                    col["sort_order"] = ["invisible"]; //["fa", "fa-sort-up"];
                    //if (is_verbose) console.log("Column " + i + " width = " + col.width_percent);
                }
            },
            prepare_data: function(rows) {
                for (let i in rows) {
                    row = rows[i];
                    row["selected"] = true;
                }
                return rows;
            },
            show_columns: function() {
                let columns = this.TableDefinition.Columns;
                let container_width = $(this.DomElement).width();
                let container_height = $(this.DomElement).height();
                console.log("");
                console.log("Container width/height: " + container_width + " x " + container_height);
                let fmt = "%-20s %10s %10.10s %10s %10s %10s";
                console.log(sprintf(fmt, "Field", "Width", "WidthPct", "HdrWidth", "MinWidth", "MaxWidth"));
                console.log(sprintf(fmt, "--------------------", "----------",  "----------", "----------", "----------", "----------"));
                for (let i in columns) {
                    let col = columns[i];
                    console.log(sprintf(fmt, col.field, col.width, col.width_percent, col.header_width, col.min_width, col.max_width));
                }
            },
            sort: function(column, direction) {
                //if (is_verbose) console.log("In sort");
                //if (is_verbose) console.log("    Current column:    ", this.sort_column);
                //if (is_verbose) console.log("    Current direction: ", this.sort_direction);
                //if (is_verbose) console.log("    Selected column:   ", column.field);
                //if (is_verbose) console.log("    Selected direction:", direction);

                this.sort_direction = direction;
                if (direction == 'none') {
                    if (this.sort_column) {
                        this.sort_column.sort_order = ['invisible'];
                    }
                    this.sort_column = undefined;
                }
                else {
                    this.sort_column = column;
                    if (direction == 'ascending') column.sort_order = ['fa', 'fa-sort-up'];
                    else column.sort_order = ['fa', 'fa-sort-down'];
                    sort_rows(this.TableDefinition.Data, column.field, this.sort_direction);
                }
            },
            handle_header_click: function(column, ev) {
                if (is_verbose) console.log("Header click:", column, ev);
                if (this.sort_column != column) {
                    if (this.sort_column) this.sort(this.sort_column, 'none');
                    this.sort(column, 'ascending');
                }
                else {
                    if (this.sort_direction == 'ascending') this.sort(column, 'descending');
                    else {
                        this.sort(column, 'none');
                        this.TableDefinition.Data = this.saved_data.slice(0);
                    }
                }
                this.$forceUpdate();
            },
            apply_filter: function() {
                cols = this.TableDefinition.Columns;
                rows = this.TableDefinition.Data;
                rows.forEach(function(x) { x.selected = true; });
                cols.forEach(function(col) {
                    let filter = col.filter;
                    let field = col.field;
                    if (filter.length > 0) {
                        try {
                            let regex = new RegExp(filter, "i");
                            rows.forEach(function (row) {
                                row.selected = row.selected && row[field].match(regex);
                            });
                        }
                        catch (err) { }
                    }
                });
            },
            on_filter_change: function(column, ev) {
                //if (is_verbose) console.log(column.field + " => " + column.filter);
                this.apply_filter();
            },
            is_included: function(row) {
                return (row.selected);
            },
            resize: function() {
                //if (is_verbose) console.log("            Resizing: ", this, this.DomElement);
                var style = window.getComputedStyle(document.getElementById(this.element_id), null);
                var height = style.getPropertyValue("height");
                var width = get_element_width(this.element_id, is_verbose);
                if (is_verbose) console.log("Height", height, "Width", width);
                let container_width = $(this.DomElement).innerWidth();
                let container_height = $(this.DomElement).height();
                let remaining_width = container_width;
                if (is_verbose) console.log("    Container size is " + container_width + " x " + container_height);
                let columns = this.TableDefinition.Columns;
                let column_count = columns.length;
                let header_fudge = 0;
                let last_column = undefined;
                if (columns.length > 1) header_fudge = Math.trunc(24.0 / (columns.length - 1))
                for (let i in columns) {
                    let col = columns[i];
                    //col.set_pixel_width(col.width * container_width);
                    col.width = Math.trunc(col.width_percent * container_width);
                    if (col.min_width) col.width = Math.max(col.width, col.min_width);
                    if (col.max_width) col.width = Math.min(col.width, col.max_width);
                    col.header_width = col.width;// + header_fudge;
                    last_column = col;
                    if (i < (column_count - 1)) remaining_width -= col.width;
                }
                //if (is_verbose) console.log("Remaining width: ", remaining_width);
                last_column.width = Math.max(last_column.width, remaining_width);
                last_column.header_width = last_column.width;
            },
            vue_resize_handler: function(e) {
                if (is_verbose) console.log("In vue_resize_handler");
                for (let i in all_vues) {
                    let vue = all_vues[i];
                    vue.resize();
                }
            }
            
        }
    });

    all_vues.push(vuegrid);
    return vuegrid;
}

if (all_vues.length == 0) {
    //console.log("Adding window resize event listener");
    window.addEventListener("resize", function () {
        //console.log("window resize");
        if (!vue_resize_timer) {
            //console.log("    Setting resize timeout");
            vue_resize_timer = setTimeout(function () {
                //console.log("        Resizing " + all_vues.length + " vues");
                all_vues.forEach(function (v) {
                    v.resize();
                })
                vue_resize_timer = undefined;
            })
        }
    }, 200);
}


_vue_grid_template = [
"<div v-bind:class='outer_class'>",
"    <table v-bind:class='table_class' style='height: 300px;'>",
"        <thead>",
"            <tr>",
"                <th v-for='col in TableDefinition.Columns' ",
"                    v-on:click='handle_header_click(col, $event)'",
"                    v-bind:style='{ width: col.header_width + \"px\", \"min-width\": col.header_width + \"px\", \"max-width\": col.header_width + \"px\"   }'>",
"                    {{col.header}} <i v-bind:class='col.sort_order'></i>",
"                </th>",
"            </tr>            ",
"            <tr v-if='filtered'>",
"                <th v-for='col in TableDefinition.Columns' v-if='col.filtered'",
"                    class='filter'",
"                    v-bind:style='{ width: col.header_width + \"px\", \"min-width\": col.header_width + \"px\", \"max-width\": col.header_width + \"px\"   }'>",
"                    <input v-model='col.filter' style='width: 100%;' v-on:input='on_filter_change(col, $event)' />",
"                </th>",
"            </tr>",
"        </thead>",
"        <tbody style='height: 300px;'>",
"            <tr v-for='row in TableDefinition.Data'>",
"                <td v-if='row.selected' ",
"                    v-on:click='handle_row_click(row, $event)'",
"                    v-for='col in TableDefinition.Columns' ",
"                    v-bind:style='{ width: col.width + \"px\", \"min-width\": col.header_width + \"px\", \"max-width\": col.header_width + \"px\"   }'>{{row[col.field]}}</td>",
"            </tr>",
"        </tbody>",
"    </table>",
"</div>",
    ].join('\n');

return create_vuegrid;
})();


