<template>
<div v-bind:class='outer_class'>
    <h2>{{title}}</h2>
    <table v-bind:class='table_class' >
        <thead>
            <tr>
                <th v-for='col in columns' 
                    v-bind:style='{ width: col.header_width + "px", "min-width": col.header_width + "px", "max-width": col.header_width + "px"   }'
                    v-on:click='on_header_click(col, $event)'>
                    {{col.header}} <i v-bind:class='col.sort_order'></i>
                </th>
            </tr>
            <tr v-if='filtered'>
                <td v-for='col in columns'
                    v-bind:style='{ width: col.header_width + "px", "min-width": col.header_width + "px", "max-width": col.header_width + "px"   }'
                    class='filter'>
                    <input v-if='col.filtered' class="search-input" v-model='col.filter' style='width: 100%;' v-on:input='on_filter_change(col, $event)' />
                    <span v-if="!col.filtered">&nbsp;</span>
                </td>
            </tr>
        </thead>
        <tbody v-bind:style="{height: height}">
            <tr v-for='row in tabledata'>
                <td v-if='row.selected'
                    v-on:click='handle_row_click(row, $event)'
                    v-bind:style='{ width: col.header_width + "px", "min-width": col.header_width + "px", "max-width": col.header_width + "px"   }'
                    v-for='col in columns'>{{row[col.field]}}</td>
            </tr>
        </tbody>
    </table>
</div>
</template>

<script>
    module.exports = {
        props: {
            columns: { type: Array, required: true },
            tabledata: { type: Array, required: true },
            title: { type: String },
            height: { type: String, default: "500px" },
            debug: { type: Boolean, default: false }
        },
        data: function () {
            return {
                filtered: false,
                outer_class: ['grid-div'],
                table_class: ['table', 'event-grid'],
                resize_timer: undefined,
                sort_column: undefined,
                sort_direction: 'none'
            }
        },
        created: function () {
            console.log("In grid.created", this.foo, this.bar);
            this.m_saved_data = [];
            this.prepare_columns();
            let self = this;
            window.addEventListener("resize", function () {
                //console.log("window resize");
                if (!self.resize_timer) {
                    self.resize_timer = setTimeout(function () {
                        self.resize_timer = undefined;
                        self.resize();
                    }, 200);
                }
            });
        },
        methods: {
            prepare_columns: function () {
                if (this.debug) console.log("In prepare_columns", this.columns);
                let total_width = 0;
                let columns = this.columns;
                let filtered_count = 0;
                foreach(columns, function (col) {
                    total_width += col.width;
                    if (col.filtered) filtered_count += 1;
                });
                this.filtered = (filtered_count > 0);
                if (this.debug) console.log("Total logical width", total_width);
                foreach(columns, function (col) {
                    col["filter"] = "";
                    col["width_percent"] = col.width / total_width;
                    col["sort_order"] = ["invisible"]; //["fa", "fa-sort-up"];
                });
            },

            initialize_data: function () {
                if (this.debug) console.log("In initialize_data: # of rows: " + this.tabledata);
                this.m_saved_data = this.tabledata.slice(0);
                this.clear_filters();
                foreach(this.tabledata, function (row) { row["selected"] = true; });
                this.$forceUpdate();
            },

            show_columns: function () {
                let columns = this.columns;
                let dom_element = this.$el;
                let container_width = $(dom_element).width();
                let container_height = $(dom_element).height();
                console.log("");
                console.log("Container width/height: " + container_width + " x " + container_height);
                let fmt = "%-20s %10s %10.10s %10s %10s %10s";
                console.log(sprintf(fmt, "Field", "Width", "WidthPct", "HdrWidth", "MinWidth", "MaxWidth"));
                console.log(sprintf(fmt, "--------------------", "----------", "----------", "----------", "----------", "----------"));
                for (let i in columns) {
                    let col = columns[i];
                    console.log(sprintf(fmt, col.field, col.width, col.width_percent, col.header_width, col.min_width, col.max_width));
                }
            },

            sort_rows: function (rows, field, direction) {
                rows.sort(function (x, y) {
                    let result = 0;
                    let v1 = x[field];
                    let v2 = y[field];
                    if (v1 > v2) result = 1;
                    if (v1 < v2) result = -1;
                    if (direction == 'descending') result = 0 - result;
                    return result;
                });
            },

            sort: function (column, direction) {
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
                    this.sort_rows(this.tabledata, column.field, this.sort_direction);
                }
            },


            on_filter_change: function () {
                let cols = this.columns;
                let rows = this.tabledata;
                let old_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                foreach(rows, function (x) { x.selected = true; });
                foreach(cols, function (col) {
                    let filter = col.filter;
                    let field = col.field;
                    if (filter.length > -1) {
                        try {
                            let regex = new RegExp(filter, "i");
                            rows.forEach(function (row) {
                                let is_included = (filter.length == 0 || row[field].match(regex));
                                row.selected = row.selected && is_included;
                            });
                        }
                        catch (err) { console.error(err); }
                    }
                });
                let new_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                if (old_selected_count !== new_selected_count) {
                    if (this.debug) console.log("on_filter_change: selected rows changed from " + old_selected_count + " to " + new_selected_count);
                    this.$forceUpdate();
                }
            },

            on_header_click: function (column, ev) {
                if (this.sort_column != column) {
                    if (this.sort_column) this.sort(this.sort_column, 'none');
                    this.sort(column, 'ascending');
                }
                else {
                    if (this.sort_direction == 'ascending') this.sort(column, 'descending');
                    else {
                        this.sort(column, 'none');
                        this.tabledata.length = 0;
                        let data = this.tabledata;  // need a reference for inside the lambda
                        foreach(this.m_saved_data, function (x) {
                            data.push(x);
                        });
                    }
                }
                //this.$forceUpdate();
            },


            clear_filters: function () {
                foreach(this.columns, function (col) { col.filter = ''; });
            },

            //---------------------------------------------------------
            //
            // AUTO SIZING
            //
            //---------------------------------------------------------
            get_element_width: function (element, styles) {
                var padding = parseFloat(styles.paddingLeft) +
                    parseFloat(styles.paddingRight);
                if (this.debug) console.log("client_width", element.clientWidth, "padding", padding)
                return element.clientWidth - padding;
            },
            resize: function () {
                var dom_element = this.$el;
                if (this.debug) console.log("Resizing grid: ", this, dom_element);
                var style = window.getComputedStyle(dom_element);
                var height = style.getPropertyValue("height");
                var width = this.get_element_width(dom_element, style);
                if (this.debug) console.log("Height", height, "Width", width);
                let container_width = $(dom_element).innerWidth();
                let container_height = $(dom_element).height();
                if (container_width > 100 && container_height > 10) {
                    let remaining_width = container_width;
                    if (this.debug) console.log("    Container size is " + container_width + " x " + container_height);
                    let columns = this.columns;
                    let column_count = columns.length;
                    let header_fudge = 0;
                    let last_column = undefined;
                    if (columns.length > 1) header_fudge = Math.trunc(24.0 / (columns.length - 1))

                    if (this.debug) console.log("Resizing columns");
                    for (let i = 0; i < columns.length; i++) {
                        let col = columns[i];
                        //col.set_pixel_width(col.width * container_width);
                        col.width = Math.trunc(col.width_percent * container_width);
                        if (col.min_width) col.width = Math.max(col.width, col.min_width);
                        if (col.max_width) col.width = Math.min(col.width, col.max_width);
                        col.header_width = col.width;// + header_fudge;
                        last_column = col;
                        if (i < (column_count - 1)) remaining_width -= col.width;
                        if (this.debug) console.log(sprintf("    %s: width=%d header_width=%d", col.header, col.width, col.header_width));
                    }
                    //if (debug) console.log("Remaining width: ", remaining_width);
                    last_column.width = Math.max(last_column.width, remaining_width);
                    last_column.header_width = last_column.width;
                    this.$forceUpdate();
                }
                return this.columns;
            },
        },
    }
</script>

<style>
    /* the div that contains the grid */
    .grid-div {
        margin-top: 1em;
    }

    /* the grid itself, which is a table */
    .event-grid {
        table-layout: fixed;
    }

    .event-grid tbody {
        display: block;
        overflow: auto;
    }

    .event-grid thead tr {
        display: block;
        position: relative;
        background-color: #318182;
        color: white;
    }

    .event-grid td, th {
        word-wrap: break-word;
        padding: 4px;
    }

    .search-input {
        color: black;
    }

</style>