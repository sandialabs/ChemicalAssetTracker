<!-- ---------------------------------------------------------------------- -->
<!--                                                                        -->
<!-- Single File Component: grid.vue                                        -->
<!--                                                                        -->
<!-- Props:                                                                 -->
<!--   config:  grid configuration information (see below)                  -->
<!--   data:    array of row data                                           -->
<!--                                                                        -->
<!-- Configuration:                                                         -->
<!--   height:     the height of the grid, with units ("400px")             -->
<!--   filter:     true/false to enable/disable filtering                   -->
<!--   columns:    column definitions                                       -->
<!--     header:   header text for the column                               -->
<!--     align:    text alignment                                           -->
<!--     width:    column width in pixels, without units (e.g. 120)         -->
<!--     sortable: true/false, is column sortable                           -->
<!--     field:    the name of the field in the row data to display         -->
<!--                                                                        -->
<!-- ---------------------------------------------------------------------- -->

<template>
    <div class="outer-div" id="outer-div">
        <!--
        <div class="row" v-if="config.filter" style="margin-top: 0;  margin-bottom: 0;">
            <div class="input-field col s6">
                <input placeholder="Filter" type="text" v-model="search_text" v-on:input="on_filter_change()" />
            </div>
        </div>
        -->
        <div v-if="config.title" class="row" style="margin-top: 0;  margin-bottom: 0;">
            <span v-html="config.title"></span>
        </div>
        <div class="row" style="margin-top: 0;  margin-bottom: 0;">
            <table class="table responsive-table" v-bind:style="{ width: table_width() }">
                <thead>
                    <tr>
                        <th v-for="col in config.columns"
                            v-on:click='on_header_click(col, $event)'
                            v-bind:style="{ textAlign: col.align, width: col.width + 'px'}">
                            {{col.header}}
                            <span v-if="col.sort_order == 'ascending'">&darr;</span>
                            <span v-if="col.sort_order == 'descending'">&uarr;</span>
                        </th>
                    </tr>
                    <tr v-if='config.filter'>
                        <td v-for='col in config.columns'
                            v-bind:style='{ width: col.width + "px", textAlign: col.align  }'
                            class='filter'>
                            <input v-if='col.filtered' 
                                   class="search-input" 
                                   placeholder="filter" 
                                   v-model='col.filter' 
                                   v-bind:style="{ textAlign: col.align }"
                                   style='width: 100%;' 
                                   v-on:input='on_column_filter_change(col, $event)' />
                            <span v-if="!col.filtered">&nbsp;</span>
                        </td>
                    </tr>
                </thead>
                <tbody :style="{ maxHeight: config.height }">
                    <tr v-for="row in active_rowdata" v-if="row.selected" v-on:click="on_row_click(row)">
                        <td v-for="col in config.columns"
                            v-bind:style="{ textAlign: col.align, width: col.width + 'px'}">
                            {{row_value(row,col.field)}}
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>

<script>

    console.log("Loading grid.vue");

    function strcontains(obj, substr) {
        if (typeof obj == 'string') {
            return (obj.toLowerCase().indexOf(substr) >= 0);
        }
        if (typeof obj == 'number') {
            return (obj.toString().indexOf(substr) >= 0);
        }
        return false;
    }

    module.exports = {
        data: function () {
            return {
                message: 'ScrollingTable.vue',
                search_text: "",
                active_rowdata: [],
                sort_column: "",
                sort_direction: "",
                autosize: false
            }
        },
        props: ["config", "rowdata"],
        mounted: function () {
            console.log("grid.vue mounted");
            this.config.columns.forEach(function (col) { if (typeof col.align == 'undefined') col.align = 'center'; });
        },
        created: function () {
            console.log("grid.vue created");
        },
        watch: {
            rowdata: function (newval, oldval) {
                console.log("rowdata changed", oldval.length, newval.length);
                this.active_rowdata = newval.slice(0);
                this.active_rowdata.forEach(function (r) { r.selected = true; });
                this.prepare_columns();
            }
        },
        methods: {
            on_column_filter_change: function () {
                let self = this;
                let cols = this.config.columns;
                let rows = this.active_rowdata;
                let old_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                rows.forEach(function(x) { x.selected = true; })
                // compile regex for each filter
                regex_list = [];
                cols.forEach(function (col) {
                    if (col.filter.length > 0) {
                        regex_list.push(filter_to_regex(col.filter));
                    }
                    else {
                        regex_list.push(false);
                    }
                });
                for (let i = 0; i < cols.length; i++) {
                    let col = cols[i];
                    let regex = regex_list[i];
                    let field = col.field;
                    if (regex) {
                        try {
                            rows.forEach(function (row) {
                                let colval = self.row_value(row, field);
                                let is_included = colval.match(regex);
                                row.selected = row.selected && is_included;
                            });
                        }
                        catch (err) { console.error(err); }
                    }
                };
                let new_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                if (old_selected_count !== new_selected_count) {
                    //console.log("on_column_filter_change: selected rows changed from " + old_selected_count + " to " + new_selected_count);
                    this.$forceUpdate();
                }
            },

            on_filter_change: function () {
                //console.log("In on_filter_change")
                let self = this;
                let search_text = this.search_text.toLowerCase();
                let cols = this.config.columns;
                let rows = this.active_rowdata;
                if (search_text.length == 0) {
                    console.log("selecting all rows");
                    rows.forEach(function (r) { r.selected = true; });
                }
                else {
                    let old_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                    rows.forEach(function (row) {
                        row.selected = false;
                        cols.forEach(function (col) {
                            let colval = self.row_value(row, col.field);
                            row.selected = (row.selected || strcontains(colval, search_text));
                        });
                    });
                    let new_selected_count = rows.filter(function (x) { return (x.selected); }).length;
                    if (old_selected_count !== new_selected_count) {
                        if (this.debug) console.log("on_filter_change: selected rows changed from " + old_selected_count + " to " + new_selected_count);
                        this.$forceUpdate();
                    }
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
                        this.active_rowdata = this.rowdata.slice(0);
                        this.active_rowdata.forEach(function (r) { r.selected = true; });
                        this.prepare_columns();
                    }
                }
                //this.$forceUpdate();
            },

            prepare_columns: function () {
                console.log("In prepare_columns", this.config.columns);
                let total_width = 0;
                let columns = this.config.columns;
                let filtered_count = 0;
                this.filtered = (filtered_count > 0);
                columns.forEach(function (col) {
                    col["filter"] = "";
                    col["sort_order"] = ""; // "up", "down"
                });
            },


            row_value: function (row, field) {
                //console.log("Row:", row, "Col", col);
                if (field.indexOf('.') < 0) return row[field];
                else {
                    //console.log("Resolving " + col.field, row);
                    let parts = field.split('.');
                    let result = row;
                    if (result) {
                        for (let i = 0; i < parts.length; i++) {
                            let part = parts[i];
                            //console.log('    ' + part + ' = ' + result[part])
                            if (result[part]) result = result[part];
                            else return "";
                        }
                    }
                    return result;
                }
            },

            table_width: function () {
                let width = 0;
                this.config.columns.forEach(function(col) {
                    width += col.width;
                });
                return width + 22 + "px";
            },

            //---------------------------------------------------------
            //
            // AUTO SIZING - NOT CURRENTLY USED
            //
            //---------------------------------------------------------
            get_element_width: function (element, styles) {
                var padding =
                    parseFloat(styles.paddingLeft) +
                    parseFloat(styles.paddingRight);
                if (this.debug)
                    console.log(
                        "client_width",
                        element.clientWidth,
                        "padding",
                        padding
                    );
                return element.clientWidth - padding;
            },


            get_element_height: function (element, styles) {
                var padding =
                    parseFloat(styles.paddingTop) +
                    parseFloat(styles.paddingBottom);
                if (this.debug)
                    console.log(
                        "get_element_height",
                        element.clientHeight,
                        "padding",
                        padding
                    );
                return element.clientHeight - padding;
            },


            get_container_height: function () {
                return this.get_element_height('outer-div');
            },

            sort_rows: function (rows, field, direction) {
                console.log("sort_rows", rows, field, direction);
                let self = this;
                rows.sort(function (x, y) {
                    let result = 0;
                    let v1 = self.row_value(x, field).toLowerCase();
                    let v2 = self.row_value(y, field).toLowerCase();
                    if (v1 > v2) result = 1;
                    if (v1 < v2) result = -1;
                    if (direction == 'descending') result = 0 - result;
                    return result;
                });
            },

            sort: function (column, direction) {
                console.log("In sort");
                console.log("    Current column:    ", this.sort_column);
                console.log("    Current direction: ", this.sort_direction);
                console.log("    Selected column:   ", column.field);
                console.log("    Selected direction:", direction);

                this.sort_direction = direction;
                if (direction == 'none') {
                    if (this.sort_column) {
                        this.sort_column.sort_order = ''
                    }
                    this.sort_column = undefined;
                }
                else {
                    this.sort_column = column;
                    if (direction == 'ascending') column.sort_order = 'ascending';
                    else column.sort_order = 'descending';
                    this.sort_rows(this.active_rowdata, column.field, this.sort_direction);
                }
            },

            resize: function () {
                var dom_element = "outer-div";
                if (this.debug) console.log("Resizing grid: ", this, dom_element);
                var style = window.getComputedStyle(dom_element);
                var height = style.getPropertyValue("height");
                var width = this.get_element_width(dom_element, style);
                if (this.debug) console.log("Height", height, "Width", width);
                this.$options.tableheight = parseInt(height) - 50;
                let container_width = $(dom_element).innerWidth();
                let container_height = $(dom_element).height();
                console.log("Container height: ", container_height);
                if (container_width > 100 && container_height > 10) {
                    let remaining_width = container_width;
                    if (this.debug)
                        console.log(
                            "    Container size is " +
                            container_width +
                            " x " +
                            container_height
                        );
                    let columns = this.config.columns;
                    let column_count = columns.length;
                    let header_fudge = 0;
                    let last_column = undefined;
                    if (columns.length > 1)
                        header_fudge = Math.trunc(24.0 / (columns.length - 1));

                    if (this.debug) console.log("Resizing columns");
                    for (let i = 0; i < columns.length; i++) {
                        let col = columns[i];
                        //col.set_pixel_width(col.width * container_width);
                        col.width = Math.trunc(col.width_percent * container_width);
                        if (col.min_width)
                            col.width = Math.max(col.width, col.min_width);
                        if (col.max_width)
                            col.width = Math.min(col.width, col.max_width);
                        col.header_width = col.width; // + header_fudge;
                        last_column = col;
                        if (i < column_count - 1) remaining_width -= col.width;
                        if (this.debug)
                            console.log(
                                "    " +
                                col.header +
                                "  width=" +
                                col.width +
                                "    header_width=" +
                                col.header_width
                            );
                    }
                    //if (debug) console.log("Remaining width: ", remaining_width);
                    last_column.width = Math.max(
                        last_column.width,
                        remaining_width
                    );
                    last_column.header_width = last_column.width;
                    this.$forceUpdate();
                }
                return this.config.columns;
            },

            on_row_click: function (row) {
                this.$emit('rowclick', row);
            }
        }
    }

</script>

<style scoped>

    .hello {
        background-color: #ffe;
    }


    .outer-div {
        margin-top: 1em;
    }

    .search-input {
        width: 100%;
        border: 1px solid gray;
        padding: 0;
        height: 1.6rem;
    }

    table {
        border: 1px solid gray;
        border-collapse: collapse;
        table-layout: fixed;
    }

    tr {
        display: block;
        position: relative;
    }

    th {
        font-variant: small-caps;
        xborder-bottom: 1px solid gray;
        xborder-right: 1px solid gray;
        padding: 4px;
        background-color: lightgray;
        width: 150px;
    }

    td {
        text-align: center;
        xborder-right: 1px solid gray;
        padding: 4px;
        width: 150px;
    }

    tr:nth-child(odd) {
        background-color: #e7e7e7;
    }

    thead {
        display: block;
        position: relative;
    }

    tbody {
        display: block;
        overflow: auto;
    }
</style>