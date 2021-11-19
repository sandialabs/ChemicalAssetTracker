<template>
    <!-- Modal Structure -->
    <div id="item-modal" class="modal itemdialog" v-bind:style="{ minHeight: min_height }" >
        <div class="modal-content">
            <span class="dialog-header">{{header}}</span>
            <hr />
            <table>
                <tr>
                    <td class="required">Name</td>
                    <td colspan="3"><input type="text" style="width: 100%;" v-model="itemdata.ChemicalName" placeholder="Chemical Name" v-bind:readonly="readonly"/></td>
                </tr>
                <tr>
                    <td class="required">Barcode</td>
                    <td><input type="text" class='field11' v-model="itemdata.Barcode" placeholder="Barcode" v-bind:disabled="readonly"/></td>
                    <td class="required">Location</td>
                    <td>
                        <select class='browser-default' v-model="itemdata.LocationID" v-bind:disabled="readonly"><option v-for="l in locations" v-bind:value="l.LocationID">{{l.ShortLocation}}</option></select>
                    </td>
                </tr>
                <tr>
                    <td>CAS #</td>
                    <td><input type="text" v-model="itemdata.CASNumber" placeholder="CAS #" v-bind:readonly="readonly" /></td>
                    <td>Owner</td>
                    <td><select class='browser-default' v-model="itemdata.OwnerID" v-bind:disabled="readonly"><option v-for="o in owners" v-bind:value="o.OwnerID">{{o.Name}}</option></select></td>
                </tr>
                <tr>
                    <td>DateIn</td>
                    <td><input type="date" v-model="itemdata.DateIn" v-bind:readonly="readonly" /></td>
                    <td>Storage Group</td>
                    <td><select class='browser-default' v-model="itemdata.GroupID" v-bind:disabled="readonly"><option v-for="g in groups" v-bind:value="g.GroupID">{{g.Name}}</option></select></td>
                </tr>
                <tr>
                    <td>Expiration</td>
                    <td><input type="date" v-model="itemdata.ExpirationDate" v-bind:readonly="readonly"/></td>
                </tr>
                <tr>
                    <td>Container Size</td>
                    <td><input type="number" class='field4' v-model="itemdata.ContainerSize" placeholder="Size" v-bind:readonly="readonly"/></td>
                    <td>Units</td>
                    <td>
                        <select class='browser-default' v-model="itemdata.Units" v-bind:disabled="readonly">
                            <option v-for="option in units" v-bind:value="option.value">{{option.text}}</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td>Amt Remaining</td>
                    <td><input type="number" class='field4' v-model="itemdata.RemainingQuantity" placeholder="Amt" v-bind:readonly="readonly" /></td>
                    <td>State</td>
                    <td>
                        <select class='browser-default' v-model="itemdata.State" v-bind:disabled="readonly">
                            <option v-for="option in states" v-bind:value="option.value">{{option.text}}</option>
                        </select>
                    </td>
                </tr>
                <tr>
                    <td>Notes</td>
                    <td colspan="3"><textarea style="width: 100%;" v-model="itemdata.Notes" v-bind:readonly="readonly">Notes</textarea></td>
                </tr>
            </table>
            <table style="width:100%;">
                <thead>
                    <tr class="attributes">
                        <td class="attribute red" style="width: 33%;">Security</td>
                        <td class="attribute blue" style="width: 33%;">Health Hazard</td>
                        <td class="attribute yellow" style="width: 33%;">Physical Hazard</td>
                    </tr>
                </thead>
                <tbody>
                    <tr class="attributes">
                        <td class="cwc">
                            <span class="attr">CWC: {{flags.CWC}}</span>
                        </td>
                        <td class="attribute">
                            <div>
                                <label>
                                    <input type="checkbox" class="browser-default" disabled="disabled" v-model="flags.CARCINOGEN" />
                                    <span class="attr">Carcinogen</span>
                                </label>
                            </div>
                        </td>
                        <td class="attribute">
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.CORROSIVE" v-bind:disabled="readonly"/>
                                    <span class="attr">Corrosive</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                    <tr class="attributes">
                        <td class="attribute">
                            <div>
                                <label>
                                    <input type="checkbox" disabled="disabled" v-model="flags.THEFT" />
                                    <span class="attr">Theft</span>
                                </label>
                            </div>
                        </td>
                        <td class="attribute">
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.HEALTHHAZARD"  v-bind:disabled="readonly"/>
                                    <span class="attr">Health Hazard</span>
                                </label>
                            </div>
                        </td>
                        <td class="attribute">
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.EXPLOSIVE"  v-bind:disabled="readonly"/>
                                    <span class="attr">Explosive</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                    <tr class="attributes">
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.OTHERSECURITY" v-bind:disabled="readonly" />
                                    <span class="attr">Other</span>
                                </label>
                            </div>
                        </td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.IRRITANT"  v-bind:disabled="readonly"/>
                                    <span class="attr">Irritant</span>
                                </label>
                            </div>
                        </td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.FLAMABLE"  v-bind:disabled="readonly"/>
                                    <span class="attr">Flamable</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                    <tr class="attributes">
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.ACUTETOXICITY"  v-bind:disabled="readonly"/>
                                    <span class="attr">Skull and Crossbones</span>
                                 </label>
                            </div>
                        </td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.OXIDIZER"  v-bind:disabled="readonly"/>
                                    <span class="attr">Oxidizer</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                    <tr class="attributes">
                        <td class="attribute">&nbsp;</td>
                        <td class="attribute">&nbsp;</td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.COMPRESSEDGAS"  v-bind:disabled="readonly"/>
                                    <span class="attr">Gas Cylinder</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                    <tr class="attributes">
                        <td class="attribute">&nbsp;</td>
                        <td class="attribute">&nbsp;</td>
                        <td>
                            <div>
                                <label>
                                    <input type="checkbox" v-model="flags.OTHERHAZARD"  v-bind:disabled="readonly"/>
                                    <span class="attr">Other</span>
                                </label>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>

        </div>
        <div class="modal-footer">
            <button class="btn green" v-on:click="on_accept()">Save</button>
            <button class="btn red" v-on:click="on_decline()">Cancel</button>
            <span>&nbsp;&nbsp;</span>
        </div>
    </div>
</template>

<script>console.log("Loading itemdialog.vue");

    module.exports = {
        // reactive data
        props: ['locations', 'groups', 'owners'],
        data: function () {
            return {
                min_height: '660px',
                header: "Edit Item",
                readonly: true,
                modified: false,
                flags: {},
                flagnames: ['ACUTETOXICITY', 'CARCINOGEN', 'COMPRESSEDGAS', 'CORROSIVE', 'EXPLOSIVE', 'FLAMABLE', 'HEALTHHAZARD',
                    'IRRITANT', 'OTHERHAZARD', 'OTHERSECURITY', 'OXIDIZER', 'THEFT'],
                itemdata: {
                    InventoryID: 0,
                    Barcode: '',
                    CASNumber: undefined,
                    LocationID: undefined,
                    GroupID: undefined,
                    OwnerID: undefined,
                    DateIn: undefined,
                    ExpirationDate: undefined,
                    ContainerSize: undefined,
                    RemainingQuantity: undefined,
                    Units: undefined,
                    State: undefined,
                    Flags: undefined,
                    InventoryStatusID: undefined,
                    LastInventoryDate: undefined,
                    SDS: undefined,
                    StockCheckPreviousLocationID: undefined,
                    StockCheckTime: undefined,
                    StockCheckUser: undefined,
                    ItemFlags: {}
                },
                units: [
                    { value: 'cm3', text: 'Cubic centimeters (cm^3)' },
                    { value: 'm3', text: 'Cubic meters (m^3)' },
                    { value: 'g', text: 'Gram (g)' },
                    { value: 'kg', text: 'Kilogram (kg)' },
                    { value: 'L', text: 'Liter (L)' },
                    { value: 'mg', text: 'Milligram (mg)' },
                    { value: 'mL', text: 'Milliliter (mL)' },
                    { value: 'ft3', text: 'Cubic feet (ft^3)' },
                    { value: ' ', text: '(blank)' },
                ],
                states: [
                    { value: 'solid', text: 'solid' },
                    { value: 'liquid', text: 'liquid' },
                    { value: 'gas', text: 'gas' },
                    { value: 'other', text: 'other' },
                ],
            }
        },

        mounted: function () {
            console.log("In itemeditor.mounted");
            $('select').formSelect();
            var elems = document.querySelectorAll('#item-modal');
            var instances = M.Modal.init(elems, {});
        },

        // created function is called after reactive hooks are added
        created: function () {
            console.log("In itemeditor.created");
            this.callback = 'undefined';
        },

        methods: {
            open: function (config, callback) {
                let self = this;
                let header = config.header;
                let item = config.item;
                console.log("Editing item", item);
                this.readonly = config.readonly;
                this.callback = callback;
                if (header) this.header = header;
                let dlg = $('#item-modal');
                this.itemdata = utils.deep_copy(item);
                // copy hazard flags to local booleans for checkboxes
                this.set_binary_flags();
                this.flags.CWC = this.itemdata.ItemFlags.CWC;
                this.itemdata.DateIn = this.convert_date_string(item.DateIn);
                this.itemdata.ExpirationDate = this.convert_date_string(item.ExpirationDate);
                //console.log('itemeditor.open', this.itemdata);
                //console.log("Flags: ", this.flags);
                dlg.modal('open');
                dlg.animate({ scrollTop: 0 }, 'fast');
            },

            on_accept: function () {
                console.log("Closing itemeditor dialog");
                $('#item-modal').modal('close');
                let json = JSON.stringify(this.itemdata);
                let itemdata = JSON.parse(json);

                itemdata.IsChanged = this.modified;

                // set item flags based on local boolean values
                this.set_item_flags(itemdata);
                //console.log("itemeditor.vue itemdata", itemdata)

                //console.log("accept", itemdata.ItemFlags, this.flags);
                //this.show_flags("Output flags", itemdata.ItemFlags);
                //console.log('Returning', itemdata);

                this.$emit('save', itemdata);
                if (this.callback) this.callback(itemdata);
            },

            on_decline: function () {
                console.log("Closing itemeditor dialog");
                $('#item-modal').modal('close');
                this.$emit('cancel');
            },

            on_modified: function () {
                this.modified = true;
            },

            set_binary_flags: function () {
                this.flags.ACUTETOXICITY = this.is_x(this.itemdata.ItemFlags.ACUTETOXICITY);
                this.flags.CARCINOGEN = this.is_x(this.itemdata.ItemFlags.CARCINOGEN);
                this.flags.COMPRESSEDGAS = this.is_x(this.itemdata.ItemFlags.COMPRESSEDGAS);
                this.flags.CORROSIVE = this.is_x(this.itemdata.ItemFlags.CORROSIVE);
                this.flags.EXPLOSIVE = this.is_x(this.itemdata.ItemFlags.EXPLOSIVE);
                this.flags.FLAMABLE = this.is_x(this.itemdata.ItemFlags.FLAMABLE);
                this.flags.HEALTHHAZARD = this.is_x(this.itemdata.ItemFlags.HEALTHHAZARD);
                this.flags.IRRITANT = this.is_x(this.itemdata.ItemFlags.IRRITANT);
                this.flags.OTHERHAZARD = this.is_x(this.itemdata.ItemFlags.OTHERHAZARD);
                this.flags.OTHERSECURITY = this.is_x(this.itemdata.ItemFlags.OTHERSECURITY);
                this.flags.OXIDIZER = this.is_x(this.itemdata.ItemFlags.OXIDIZER);
                this.flags.THEFT = this.is_x(this.itemdata.ItemFlags.THEFT);
            },

            set_item_flags: function (itemdata) {
                itemdata.ItemFlags.ACUTETOXICITY = this.to_x(this.flags.ACUTETOXICITY );
                itemdata.ItemFlags.CARCINOGEN = this.to_x(this.flags.CARCINOGEN );
                itemdata.ItemFlags.COMPRESSEDGAS = this.to_x(this.flags.COMPRESSEDGAS );
                itemdata.ItemFlags.CORROSIVE = this.to_x(this.flags.CORROSIVE );
                itemdata.ItemFlags.EXPLOSIVE = this.to_x(this.flags.EXPLOSIVE );
                itemdata.ItemFlags.FLAMABLE = this.to_x(this.flags.FLAMABLE );
                itemdata.ItemFlags.HEALTHHAZARD = this.to_x(this.flags.HEALTHHAZARD );
                itemdata.ItemFlags.IRRITANT = this.to_x(this.flags.IRRITANT );
                itemdata.ItemFlags.OTHERHAZARD = this.to_x(this.flags.OTHERHAZARD );
                itemdata.ItemFlags.OTHERSECURITY = this.to_x(this.flags.OTHERSECURITY );
                itemdata.ItemFlags.OXIDIZER = this.to_x(this.flags.OXIDIZER );
                itemdata.ItemFlags.THEFT = this.to_x(this.flags.THEFT );
                itemdata.ItemFlags._TEST = '*';
                //console.log("set_item_flags", itemdata.ItemFlags);
            },

            convert_date_string: function (datestr) {
                let result = "";
                if (datestr) {
                    let m = datestr.match(/^(\d\d\d\d-\d\d-\d\d)/);
                    if (m) result = m[1];
                }
                return result;
            },

            is_x: function (val) {
                let result = (val == 'x' || val == 'X');
                //console.log("is_x: " + val + ' => ' + result);
                return result;
            },

            to_x: function (val) {
                let result = ' ';
                if (val) result = 'X';
                //console.log("to_x: " + val + ' => "' + result + '"');
                return val;
            },

            show_flags: function (description, itemflags) {
                let self = this;
                console.log(description, itemflags);
                this.flagnames.forEach(function (f) {
                    console.log('    ' + f + ' = ' + itemflags[f]);
                });
            }

        }
    }</script>

<style scoped>

    .itemdialog {
        width: 600px;
        /* min-height: 660px; */
    }

    .dialog-header {
        font-weight: bold;
    }

    .horizontal-label {
        display: inline;
    }

    .red-button {
        background-color: red;
    }

    .permission {
        margin-right: 2em;
        margin-top: 4px;
    }

    .attr {
        color: black;
    }

    tr.attributes {
        width: 100%;
        padding: 4px;
        min-height: 2rem;
        max-height: 2rem;
        height: 2rem;
    }

    td.attribute {
        width: 33%;
        padding: 4px;
        min-height: 2rem;
        max-height: 2rem;
        height: 2rem;
    }

    td.cwc {
        width: 33%;
        padding-left: 4px;
        padding-top: 4px;
        padding-bottom: 0px;
    }





</style>