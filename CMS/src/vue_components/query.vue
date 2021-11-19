<template>
    <div>
        <div v-if="rows.length > 1">
            <table class="query-result">
                <thead>
                    <tr>
                        <th v-for="col in def.Columns">{{ col.header }}</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in rows">
                        <td v-for="col in def.Columns">{{ row[col.column] }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div v-if="rows.length == 0">
            <p>No data available</p>
        </div>
    </div>
</template>

<script>
console.log("Loading query.vue");

//---------------------------------------------------------------------
//
// Use:  <query :def="query_def"></query>
//
//       query_def: {
//           QueryName: 'test',
//           // parameters to pass to the query 
//           Parameters: [{
//               Name: 'chemical',
//               DataType: 'string',
//               Value: 'magnesium%'
//           }],
//           // output column definitions
//           Columns: [
//               { header: 'Barcode', column: 'Barcode' },
//               { header: 'Chemical', column: 'ChemicalName' },
//               { header: 'CAS#', column: 'CASNumber' },
//               { header: 'DateIn', column: 'DateIn' },
//               { header: 'Expiry', column: 'ExpirationDate' },
//               { header: 'Size', column: 'ContainerSize' },
//               { header: 'Remaining', column: 'RemainingQuantity' },
//               { header: 'Units', column: 'Units' },
//           ],
//           MaxRows: 100
//       }
//
//---------------------------------------------------------------------


const mymodule = {
    props: ["def"],
    data: function() {
        return {
            have_rows: false,
            rows: []
        };
    },
    created: function() {
        console.log("query.vue created");
        this.run_query();
    },
    methods: {
        run_query: function() {
            let url = "/api/executequery";
            let data = {
                QueryName: this.def.QueryName,
                Parameters: this.def.Parameters,
                MaxRows: this.def.MaxRows || 99999
            };
            console.log("Calling " + url, data);
            let self = this;
            axios({
                url: url,
                method: "POST",
                data: data
            }).then(function(response) {
                console.log("Query.vue: have response from " + url, response);
                console.log("Rows:", response.data.Data.result.Rows);
                // rows will be in response.data.data.query_result.rows
                self.rows = response.data.Data.result.Rows;
                //self.$forceUpdate();
            });
        }
    }
};
module.exports = mymodule;
if (window.VueComponents) window.VueComponents["Query"] = mymodule;
else window.VueComponents = { Query: mymodule };
</script>

<style>
table.query-result {
    border: 1px solid gray;
    border-collapse: collapse;
}

table.query-result th {
    border: 1px solid gray;
    padding-top: 4px;
    padding-left: 4px;
    padding-right: 4px;
    padding-bottom: 8px;
    text-align: center;
}

table.query-result td {
    border: 1px solid gray;
    padding-top: 4px;
    padding-left: 4px;
    padding-right: 4px;
    padding-bottom: 8px;
    text-align: center;
}

thead tr {
    background-color: #666666;
    color: white;
}

table.query-result tr:nth-child(even) {
    padding-top: 2px;
    background-color: #bbbbbb;
}
</style>
